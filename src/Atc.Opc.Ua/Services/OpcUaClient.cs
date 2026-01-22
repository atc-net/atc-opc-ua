// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable InvertIf
// ReSharper disable MemberCanBePrivate.Global
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality for connecting to, and interacting with, OPC UA servers.
/// </summary>
/// <remarks>
/// <para>
/// This class intentionally catches general exceptions in several places rather than
/// catching specific exception types. OPC UA server communication can fail due to a wide
/// variety of conditions (network issues, server errors, certificate problems, timeouts,
/// protocol violations, etc.) that manifest as different exception types across the OPC UA
/// stack. Catching <see cref="Exception"/> ensures robust error handling without exposing
/// low-level communication failures to callers, who receive actionable error messages via
/// the result tuple pattern instead.
/// </para>
/// <para>
/// The Dispose pattern uses broad exception handling as a best-effort cleanup strategy,
/// ensuring that disposal completes even if individual cleanup steps fail.
/// </para>
/// </remarks>
public partial class OpcUaClient : IOpcUaClient
{
    private readonly ApplicationConfiguration configuration;
    private readonly OpcUaSecurityOptions securityOptions;
    private readonly OpcUaClientOptions clientOptions;
    private readonly OpcUaClientKeepAliveOptions keepAliveOptions;
    private readonly DataTypeInfoResolver dataTypeInfoResolver = new();

    private SessionReconnectHandler? reconnectHandler;
    private int consecutiveKeepAliveFailures;

    private bool disposed;

    /// <summary>
    /// Gets the current session with the OPC UA server.
    /// </summary>
    public ISession? Session { get; private set; }

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

    public OpcUaClient(
        ILogger<OpcUaClient> logger)
        : this(
            logger,
            new OptionsWrapper<OpcUaSecurityOptions>(new OpcUaSecurityOptions()))
    {
    }

    /// <summary>
    /// Determines whether the client is currently connected to an OPC UA server.
    /// </summary>
    /// <returns>A value indicating whether the client is connected.</returns>
    public bool IsConnected()
        => Session is not null && Session.Connected;

    /// <summary>
    /// Disconnects from the currently connected OPC UA server, if any.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A tuple indicating whether the disconnection was successful, and an error message if not.</returns>
    public async Task<(bool Succeeded, string? ErrorMessage)> DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (Session is not null)
            {
                var sessionName = Session.SessionName ?? "unknown";
                LogSessionDisconnecting(sessionName);

                if (keepAliveOptions.Enable)
                {
                    Session.KeepAlive -= SessionOnKeepAlive;
                }

                reconnectHandler?.Dispose();
                reconnectHandler = null;
                consecutiveKeepAliveFailures = 0;

                await Session.CloseAsync(cancellationToken);
                Session.Dispose();
                Session = null;

                dataTypeInfoResolver.ClearCache();

                LogSessionDisconnected(sessionName);
                return (true, null);
            }

            LogSessionNotConnected();
            return (false, "Session is not connected.");
        }
        catch (Exception ex)
        {
            LogSessionDisconnectionFailure(ex.Message);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Asynchronously connects to an OPC UA server.
    /// </summary>
    /// <param name="serverUri">The URI of the OPC UA server.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        CancellationToken cancellationToken = default)
        => ConnectAsync(serverUri, string.Empty, string.Empty, cancellationToken);

    /// <summary>
    /// Asynchronously connects to an OPC UA server with specified credentials.
    /// </summary>
    /// <param name="serverUri">The URI of the OPC UA server.</param>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serverUri);

        return InvokeConnectAsync(serverUri, userName, password, cancellationToken);
    }

    /// <summary>
    /// Asynchronously attempts to connect to the specified OPC UA server.
    /// </summary>
    /// <param name="serverUri">The URI of the server.</param>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task representing the asynchronous operation, with a tuple indicating whether the connection was successful, and an error message if not.</returns>
    private async Task<(bool Succeeded, string? ErrorMessage)> InvokeConnectAsync(
        Uri serverUri,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (IsConnected())
            {
                LogSessionAlreadyConnected();
            }
            else
            {
                LogSessionConnecting(serverUri.AbsoluteUri);

                var useSecurity = !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password);
                var endpoint = await GetServerEndpointAsync(serverUri, useSecurity, cancellationToken);
                var userIdentity = BuildUserIdentity(userName, password);
                var session = await CreateSessionAsync(endpoint, userIdentity, cancellationToken);

                if (session.Connected)
                {
                    Session = session;

                    // Keep alive
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
                }

                LogSessionConnected(Session?.SessionName ?? "unknown");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            LogSessionConnectionFailure(serverUri.AbsoluteUri, ex.Message);
            return (false, ex.Message);
        }
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
    /// Handles the certificate validation event.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void CertificateValidation(
        CertificateValidator sender,
        CertificateValidationEventArgs e)
    {
        const bool certificateAccepted = true;

        if (certificateAccepted)
        {
            LogSessionUntrustedCertificateAccepted(e.Certificate.SubjectName.Name);
        }

        e.Accept = certificateAccepted;
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
    private async Task<ApplicationInstance> BuildAndValidateOpcUaApplicationAsync(CancellationToken cancellationToken = default)
    {
        var applicationConfiguration = ApplicationConfigurationFactory.Create(clientOptions.ApplicationName, securityOptions);
        applicationConfiguration = ApplicationInstance.FixupAppConfig(applicationConfiguration);

        await applicationConfiguration.ValidateAsync(applicationConfiguration.ApplicationType, cancellationToken);

        var application = new ApplicationInstance(applicationConfiguration);

        var hasAppCertificate = await application.CheckApplicationInstanceCertificatesAsync(
            silent: true,
            lifeTimeInMonths: CertificateFactory.DefaultLifeTime,
            cancellationToken);

        if (!hasAppCertificate)
        {
            throw new CertificateValidationException("OPC UA application certificate cannot be validated.");
        }

        return application;
    }

    /// <summary>
    /// Handles keep-alive notifications. Tolerates temporary hiccups and triggers
    /// a background reconnect after several consecutive failures.
    /// </summary>
    /// <param name="session">The session the keep alive event is received for</param>
    /// <param name="e">The event arguments</param>
    private void SessionOnKeepAlive(ISession session, KeepAliveEventArgs e)
    {
        try
        {
            if (Session is null || !ReferenceEquals(Session, session))
            {
                return;
            }

            if (ServiceResult.IsGood(e.Status))
            {
                if (consecutiveKeepAliveFailures != 0)
                {
                    consecutiveKeepAliveFailures = 0;
                    LogSessionKeepAliveFailureCountReset();
                }

                return;
            }

            consecutiveKeepAliveFailures++;
            LogSessionKeepAliveFailure(e.Status?.ToString() ?? "Unknown", consecutiveKeepAliveFailures);

            // Avoid multiple concurrent reconnect attempts.
            if (reconnectHandler is not null)
            {
                return;
            }

            if (consecutiveKeepAliveFailures >= keepAliveOptions.MaxFailuresBeforeReconnect)
            {
                // Start background reconnect; session remains usable if server recovers quickly.
                reconnectHandler = new SessionReconnectHandler();
                reconnectHandler.BeginReconnect(Session, keepAliveOptions.ReconnectPeriodMilliseconds, OnReconnectComplete);
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
    private void OnReconnectComplete(object? sender, EventArgs e)
    {
        try
        {
            if (sender is not SessionReconnectHandler handler)
            {
                return;
            }

            // Swap in the new, reconnected session.
            if (handler.Session is Session reconnected)
            {
                Session = reconnected;

                // Re-apply keep-alive interval based on options. The event subscription remains
                // on the Session instance when enabled; if disabled we ensure interval is 0.
                Session.KeepAliveInterval = keepAliveOptions.Enable
                    ? keepAliveOptions.IntervalMilliseconds
                    : 0;

                LogSessionReconnected(Session.SessionName ?? "unknown");
            }
        }
        catch (Exception ex)
        {
            LogSessionReconnectFailure(ex);
        }
        finally
        {
            reconnectHandler?.Dispose();
            reconnectHandler = null;
            consecutiveKeepAliveFailures = 0;
        }
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
                reconnectHandler?.Dispose();
                reconnectHandler = null;
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