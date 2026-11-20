// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable InvertIf
// ReSharper disable MemberCanBePrivate.Global
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality for connecting to, and interacting with, OPC UA servers.
/// </summary>
/// <remarks>
/// <para>
/// A failed call throws the OPC UA stack's exception, usually a <see cref="ServiceResultException"/> with the server's
/// status code, and a cancelled call throws <see cref="OperationCanceledException"/>. Calls that need a connection
/// throw <see cref="InvalidOperationException"/> when the client isn't connected.
/// </para>
/// <para>
/// The Dispose pattern uses broad exception handling as a best-effort cleanup strategy,
/// ensuring that disposal completes even if individual cleanup steps fail.
/// </para>
/// </remarks>
public partial class OpcUaClient : IOpcUaClient
{
    private const string NotConnectedMessage = "The client isn't connected. Call ConnectAsync first.";

    /// <summary>
    /// How long to wait for the server when closing a session that is no longer connected, before connecting again.
    /// </summary>
    private static readonly TimeSpan StaleSessionCloseTimeout = TimeSpan.FromSeconds(5);

    private readonly ApplicationConfiguration configuration;
    private readonly OpcUaSecurityOptions securityOptions;
    private readonly OpcUaClientOptions clientOptions;
    private readonly OpcUaClientKeepAliveOptions keepAliveOptions;
    private readonly DataTypeInfoResolver dataTypeInfoResolver = new();

    // The OPC UA stack raises keep-alive events from several threads at once; this guards the reconnect state.
    private readonly Lock reconnectLock = new();
    private SessionReconnectHandler? reconnectHandler;
    private int consecutiveKeepAliveFailures;

    private bool disposed;

    /// <summary>
    /// Gets the current session with the OPC UA server.
    /// </summary>
    public ISession? Session { get; private set; }

    /// <summary>
    /// The session, for a call that checked the connection with <see cref="EnsureConnected"/>. Read it for each request
    /// rather than keeping it: a reconnect can replace the session, and the next request then uses the new one.
    /// </summary>
    /// <exception cref="InvalidOperationException">The client was disconnected while the call ran.</exception>
    private ISession CurrentSession
        => Session ?? throw new InvalidOperationException(NotConnectedMessage);

    public OpcUaClient(
        ILogger<OpcUaClient> logger,
        IOptions<OpcUaSecurityOptions> opcUaSecurityOptions,
        IOptions<OpcUaClientOptions> opcUaClientOptions,
        IOptions<OpcUaClientKeepAliveOptions> opcUaClientKeepAliveOptions)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(opcUaSecurityOptions);
        ArgumentNullException.ThrowIfNull(opcUaClientOptions);
        ArgumentNullException.ThrowIfNull(opcUaClientKeepAliveOptions);

        this.logger = logger;
        securityOptions = opcUaSecurityOptions.Value ?? throw new ArgumentNullException(nameof(opcUaSecurityOptions));
        clientOptions = opcUaClientOptions.Value ?? throw new ArgumentNullException(nameof(opcUaClientOptions));
        keepAliveOptions = opcUaClientKeepAliveOptions.Value ?? throw new ArgumentNullException(nameof(opcUaClientKeepAliveOptions));

        if (!Enum.IsDefined(securityOptions.ServerCertificateValidation))
        {
            throw new ArgumentException(
                $"'{securityOptions.ServerCertificateValidation}' isn't a {nameof(ServerCertificateValidation)} value.",
                nameof(opcUaSecurityOptions));
        }

        var application = BuildAndValidateOpcUaApplicationAsync().GetAwaiter().GetResult();
        configuration = application.ApplicationConfiguration;
        configuration.CertificateValidator.CertificateValidation += CertificateValidation;
    }

    public OpcUaClient(
        ILogger<OpcUaClient> logger,
        IOptions<OpcUaSecurityOptions> opcUaSecurityOptions)
        : this(
            logger,
            opcUaSecurityOptions,
            new OptionsWrapper<OpcUaClientOptions>(new OpcUaClientOptions()),
            new OptionsWrapper<OpcUaClientKeepAliveOptions>(new OpcUaClientKeepAliveOptions()))
    {
    }

    public OpcUaClient(ILogger<OpcUaClient> logger)
        : this(
            logger,
            new OptionsWrapper<OpcUaSecurityOptions>(new OpcUaSecurityOptions()))
    {
    }

    /// <inheritdoc/>
    public bool IsConnected()
        => Session is not null && Session.Connected;

    /// <inheritdoc/>
    public Task DisconnectAsync(CancellationToken cancellationToken = default)
        => InvokeDisconnectAsync(cancellationToken).NormalizeCancellation(cancellationToken);

    /// <inheritdoc/>
    public Task ConnectAsync(
        Uri serverUri,
        CancellationToken cancellationToken = default)
        => ConnectAsync(serverUri, string.Empty, string.Empty, cancellationToken);

    /// <inheritdoc/>
    public Task ConnectAsync(
        Uri serverUri,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serverUri);

        return InvokeConnectAsync(serverUri, userName, password, cancellationToken).NormalizeCancellation(cancellationToken);
    }

    /// <summary>
    /// Throws when the client isn't connected, for the calls that need a connection.
    /// </summary>
    /// <exception cref="InvalidOperationException">The client isn't connected.</exception>
    private void EnsureConnected()
    {
        if (!IsConnected())
        {
            throw new InvalidOperationException(NotConnectedMessage);
        }
    }

    /// <summary>
    /// Closes the session, if there is one. The session is disposed even when closing it fails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task InvokeDisconnectAsync(
        CancellationToken cancellationToken)
    {
        ISession? session;
        lock (reconnectLock)
        {
            // Stop a running reconnect first, so it can't swap a session in while this one closes.
            reconnectHandler?.Dispose();
            reconnectHandler = null;
            consecutiveKeepAliveFailures = 0;
            session = Session;
        }

        if (session is null)
        {
            return;
        }

        var sessionName = session.SessionName ?? "unknown";
        LogSessionDisconnecting(sessionName);

        if (keepAliveOptions.Enable)
        {
            session.KeepAlive -= SessionOnKeepAlive;
        }

        CleanupSubscription();

        try
        {
            await session.CloseAsync(cancellationToken);
        }
        finally
        {
            session.Dispose();
            Session = null;
            dataTypeInfoResolver.ClearCache();
        }

        LogSessionDisconnected(sessionName);
    }

    /// <summary>
    /// Closes a session that was closed without disconnecting the client, e.g. through <see cref="Session"/>, before
    /// connecting again: stops its reconnect, and drops its subscription. Waits at most <see cref="StaleSessionCloseTimeout"/> for the server. A failure is logged and
    /// ignored; the session is disposed either way.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task CloseStaleSessionAsync(
        CancellationToken cancellationToken)
    {
        LogSessionClosingStale(Session?.SessionName ?? "unknown");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(StaleSessionCloseTimeout);

        try
        {
            await InvokeDisconnectAsync(timeout.Token);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            LogSessionCloseStaleFailure(ex.Message);
        }
    }

    /// <summary>
    /// Asynchronously attempts to connect to the specified OPC UA server.
    /// </summary>
    /// <param name="serverUri">The URI of the server.</param>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the client is connected.</returns>
    private async Task InvokeConnectAsync(
        Uri serverUri,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (IsConnected())
        {
            LogSessionAlreadyConnected();
            return;
        }

        // A session that was closed, e.g. through Session, without disconnecting the client: dispose it, so it isn't
        // left behind, and its reconnect can't swap it back in later. A dropped connection doesn't get here: the
        // session counts as connected until it's closed, and the client reconnects it in the background.
        if (Session is not null)
        {
            await CloseStaleSessionAsync(cancellationToken);
        }

        LogSessionConnecting(serverUri.AbsoluteUri);

        var useSecurity = !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password);
        var endpoint = await GetServerEndpointAsync(serverUri, useSecurity, cancellationToken);
        var userIdentity = BuildUserIdentity(userName, password);
        var session = await CreateSessionAsync(endpoint, userIdentity, cancellationToken);

        if (!session.Connected)
        {
            session.Dispose();
            throw new ServiceResultException(StatusCodes.BadNotConnected, $"The session to '{serverUri.AbsoluteUri}' isn't connected.");
        }

        Session = session;

        if (keepAliveOptions.Enable)
        {
            Session.KeepAliveInterval = keepAliveOptions.IntervalMilliseconds;
            Session.KeepAlive += SessionOnKeepAlive;
        }
        else
        {
            // Disable keep-alive by setting interval to 0 and not subscribing to the event.
            Session.KeepAliveInterval = 0;
        }

        LogSessionConnected(Session.SessionName ?? "unknown");
    }

    /// <summary>
    /// Retrieves the endpoint configuration from the specified server.
    /// </summary>
    /// <param name="serverUri">The URI of the server.</param>
    /// <param name="useSecurity">Indicates whether to use security while connecting.</param>
    /// <returns>The configured endpoint on the server.</returns>
    private async Task<ConfiguredEndpoint> GetServerEndpointAsync(
        Uri serverUri,
        bool useSecurity,
        CancellationToken cancellationToken = default)
    {
        var endpointDescription = await CoreClientUtils.SelectEndpointAsync(configuration, serverUri.AbsoluteUri, useSecurity, cancellationToken);
        var endpointConfiguration = EndpointConfiguration.Create(configuration);
        var endpoint = new ConfiguredEndpoint(collection: null, endpointDescription, endpointConfiguration);
        return endpoint;
    }

    /// <summary>
    /// Decides on an error the OPC UA stack found in a server certificate, as
    /// <see cref="OpcUaSecurityOptions.ServerCertificateValidation"/> says. The stack raises it once for each error.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void CertificateValidation(
        CertificateValidator sender,
        CertificateValidationEventArgs e)
    {
        var error = e.Error.StatusCode;
        var decision = ServerCertificateValidationPolicy.Decide(
            securityOptions.ServerCertificateValidation,
            securityOptions.AutoAcceptUntrustedCertificates,
            error);

        var subject = e.Certificate?.Subject ?? "unknown";
        var thumbprint = e.Certificate?.Thumbprint ?? "unknown";
        var statusCode = StatusCodes.GetBrowseName(error.Code);

        switch (decision)
        {
            case ServerCertificateDecision.AcceptUntrusted:
                LogServerCertificateUntrustedAccepted(subject, thumbprint);
                break;
            case ServerCertificateDecision.AcceptWithoutValidation:
                LogServerCertificateAcceptedWithoutValidation(subject, thumbprint, statusCode);
                break;
            case ServerCertificateDecision.AcceptAudited:
                LogServerCertificateValidationWouldFail(
                    subject,
                    thumbprint,
                    statusCode,
                    ServerCertificateValidationPolicy.GetHint(error, refused: false, securityOptions));
                break;
            default:
                LogServerCertificateRejected(
                    subject,
                    thumbprint,
                    statusCode,
                    ServerCertificateValidationPolicy.GetHint(error, refused: true, securityOptions));
                break;
        }

        e.Accept = decision != ServerCertificateDecision.Reject;
    }

    /// <summary>
    /// Asynchronously creates a new session with the specified endpoint and user identity.
    /// </summary>
    /// <param name="endpoint">The endpoint configuration.</param>
    /// <param name="userIdentity">The user identity.</param>
    /// <returns>A task representing the asynchronous operation, with the created session as result.</returns>
    private async Task<Session> CreateSessionAsync(
        ConfiguredEndpoint endpoint,
        IUserIdentity userIdentity,
        CancellationToken cancellationToken = default)
    {
        var session = await DefaultSessionFactory.Instance.CreateAsync(
            configuration,
            endpoint,
            updateBeforeConnect: false,
            checkDomain: false,
            configuration.ApplicationName + Guid.NewGuid(),
            clientOptions.SessionTimeoutMilliseconds,
            userIdentity,
            preferredLocales: null,
            cancellationToken);

        return (Session)session;
    }

    /// <summary>
    /// Builds a user identity object based on the specified credentials.
    /// </summary>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The created user identity.</returns>
    private static UserIdentity BuildUserIdentity(
        string userName,
        string password)
    {
        UserIdentity userIdentity;

        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            userIdentity = new UserIdentity(new AnonymousIdentityToken());
        }
        else
        {
            var userNameIdentityToken = new UserNameIdentityToken
            {
                UserName = userName,
                DecryptedPassword = password,
            };

            userIdentity = new UserIdentity(userNameIdentityToken);
        }

        return userIdentity;
    }

    /// <summary>
    /// Asynchronously builds and validates an OPC UA application instance.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, with the created application instance as result.</returns>
    private async Task<ApplicationInstance> BuildAndValidateOpcUaApplicationAsync(
        CancellationToken cancellationToken = default)
    {
        var applicationConfiguration = ApplicationConfigurationFactory.Create(clientOptions.ApplicationName, securityOptions);
        applicationConfiguration = ApplicationInstance.FixupAppConfig(applicationConfiguration);

        await applicationConfiguration.ValidateAsync(applicationConfiguration.ApplicationType, cancellationToken);

        var application = new ApplicationInstance(applicationConfiguration);

        var hasAppCertificate = await application.CheckApplicationInstanceCertificatesAsync(
            silent: true,
            lifeTimeInMonths: CertificateFactory.DefaultLifeTime,
            cancellationToken);

        return hasAppCertificate
            ? application
            : throw new CertificateValidationException("OPC UA application certificate cannot be validated.");
    }

    /// <summary>
    /// Handles keep-alive notifications. Tolerates temporary hiccups and triggers
    /// a background reconnect after several consecutive failures.
    /// </summary>
    /// <param name="session">The session the keep alive event is received for</param>
    /// <param name="e">The event arguments</param>
    private void SessionOnKeepAlive(
        ISession session,
        KeepAliveEventArgs e)
    {
        try
        {
            if (Session is null || !ReferenceEquals(Session, session))
            {
                return;
            }

            lock (reconnectLock)
            {
                if (ServiceResult.IsGood(e.Status))
                {
                    if (consecutiveKeepAliveFailures != 0)
                    {
                        LogSessionKeepAliveFailureCountReset(consecutiveKeepAliveFailures);
                        consecutiveKeepAliveFailures = 0;
                    }

                    return;
                }

                consecutiveKeepAliveFailures++;

                // A reconnect is already running: its start was logged, and its end will be.
                if (reconnectHandler is not null)
                {
                    return;
                }

                LogSessionKeepAliveFailure(e.Status?.ToString() ?? "Unknown", consecutiveKeepAliveFailures);

                if (consecutiveKeepAliveFailures >= keepAliveOptions.MaxFailuresBeforeReconnect)
                {
                    // Start background reconnect; session remains usable if server recovers quickly.
                    LogSessionReconnecting(session.SessionName ?? "unknown", consecutiveKeepAliveFailures);
                    reconnectHandler = new SessionReconnectHandler();
                    reconnectHandler.BeginReconnect(session, keepAliveOptions.ReconnectPeriodMilliseconds, OnReconnectComplete);
                }
            }
        }
        catch (Exception exception)
        {
            LogSessionKeepAliveRequestFailure(exception);
        }
    }

    /// <summary>
    /// Called when the reconnect operation completes.
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event args.</param>
    private void OnReconnectComplete(
        object? sender,
        EventArgs e)
    {
        if (sender is not SessionReconnectHandler handler)
        {
            return;
        }

        ISession? replaced = null;
        try
        {
            // Under the lock, so a disconnect can't run between the check and the swap.
            lock (reconnectLock)
            {
                // A reconnect that a disconnect, or a new connect, stopped: its session isn't the client's anymore.
                if (!ReferenceEquals(handler, reconnectHandler))
                {
                    return;
                }

                // Swap in the new, reconnected session.
                if (handler.Session is Session reconnected)
                {
                    if (Session is not null && !ReferenceEquals(Session, reconnected))
                    {
                        replaced = Session;
                    }

                    Session = reconnected;

                    // Re-apply keep-alive interval based on options. The event subscription remains
                    // on the Session instance when enabled; if disabled we ensure interval is 0.
                    reconnected.KeepAliveInterval = keepAliveOptions.Enable
                        ? keepAliveOptions.IntervalMilliseconds
                        : 0;

                    LogSessionReconnected(reconnected.SessionName ?? "unknown");

                    RestoreSubscriptionAfterReconnect(reconnected);
                }
            }

            if (replaced is not null)
            {
                DisposeReplacedSession(replaced);
            }
        }
        catch (Exception ex)
        {
            LogSessionReconnectFailure(ex);
        }
        finally
        {
            lock (reconnectLock)
            {
                // Only while it's still the client's reconnect; a disconnect may have stopped it already.
                if (ReferenceEquals(handler, reconnectHandler))
                {
                    handler.Dispose();
                    reconnectHandler = null;
                    consecutiveKeepAliveFailures = 0;
                }
            }
        }
    }

    /// <summary>
    /// Disposes the session that a reconnect replaced with a new one, because the old session couldn't be reactivated,
    /// e.g. after the server restarted. The OPC UA stack leaves that to the session's owner. The subscription was
    /// recreated on the new session before this.
    /// </summary>
    /// <param name="session">The replaced session.</param>
    private void DisposeReplacedSession(ISession session)
    {
        if (keepAliveOptions.Enable)
        {
            session.KeepAlive -= SessionOnKeepAlive;
        }

        var sessionName = session.SessionName ?? "unknown";
        Utils.SilentDispose(session);

        LogSessionReplacedAfterReconnect(sessionName);
    }

    /// <summary>
    /// Releases all resources used by the client.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the client and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            try
            {
                configuration.CertificateValidator.CertificateValidation -= CertificateValidation;
            }
            catch
            {
                // Ignore: best-effort detach
            }

            CleanupSubscription();
            subscriptionGate.Dispose();

            try
            {
                if (Session is not null)
                {
                    if (keepAliveOptions.Enable)
                    {
                        Session.KeepAlive -= SessionOnKeepAlive;
                    }

                    try
                    {
                        TaskHelper.RunSync(async () => await Session.CloseAsync(CancellationToken.None));
                    }
                    catch
                    {
                        // Ignore: best-effort close
                    }

                    Session.Dispose();
                    Session = null;
                }
            }
            catch
            {
                // Ignore: best-effort dispose
            }

            try
            {
                lock (reconnectLock)
                {
                    reconnectHandler?.Dispose();
                    reconnectHandler = null;
                }
            }
            catch
            {
                // Ignore: best-effort dispose
            }

            consecutiveKeepAliveFailures = 0;
        }

        disposed = true;
    }
}