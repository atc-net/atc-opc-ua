// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality for connecting to, and interacting with, OPC UA servers.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK - By Design")]
public partial class OpcUaClient : IOpcUaClient
{
    private const uint SessionTimeout = 30 * 60 * 1000;
    private const string ApplicationName = nameof(OpcUaClient);
    private const int KeepAliveInterval = 5 * 1000;
    private readonly ApplicationConfiguration configuration;
    private readonly OpcUaSecurityOptions securityOptions;

    /// <summary>
    /// Gets the current session with the OPC UA server.
    /// </summary>
    public Session? Session { get; private set; }

    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "OK - By Design")]
    public OpcUaClient(
        ILogger<OpcUaClient> logger,
        IOptions<OpcUaSecurityOptions> opcUaSecurityOptions)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.securityOptions = opcUaSecurityOptions.Value ?? throw new ArgumentNullException(nameof(opcUaSecurityOptions));
        var application = BuildAndValidateOpcUaApplicationAsync().GetAwaiter().GetResult();
        configuration = application.ApplicationConfiguration;
        configuration.CertificateValidator.CertificateValidation += CertificateValidation;
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
    /// <returns>A tuple indicating whether the disconnection was successful, and an error message if not.</returns>
    public (bool Succeeded, string? ErrorMessage) Disconnect()
    {
        try
        {
            if (Session != null)
            {
                var sessionName = Session.SessionName ?? "unknown";
                LogSessionDisconnecting(sessionName);

                Session.KeepAlive -= SessionOnKeepAlive;
                Session.Close();
                Session.Dispose();
                Session = null;

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
    /// <returns>A task representing the asynchronous connect operation.</returns>
    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri)
        => ConnectAsync(serverUri, string.Empty, string.Empty);

    /// <summary>
    /// Asynchronously connects to an OPC UA server with specified credentials.
    /// </summary>
    /// <param name="serverUri">The URI of the OPC UA server.</param>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>A task representing the asynchronous connect operation.</returns>
    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string userName,
        string password)
    {
        ArgumentNullException.ThrowIfNull(serverUri);

        return InvokeConnectAsync(serverUri, userName, password);
    }

    /// <summary>
    /// Asynchronously attempts to connect to the specified OPC UA server.
    /// </summary>
    /// <param name="serverUri">The URI of the server.</param>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>A task representing the asynchronous operation, with a tuple indicating whether the connection was successful, and an error message if not.</returns>
    private async Task<(bool Succeeded, string? ErrorMessage)> InvokeConnectAsync(
        Uri serverUri,
        string userName,
        string password)
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
                var endpoint = GetServerEndpoint(serverUri, useSecurity);
                var userIdentity = BuildUserIdentity(userName, password);
                var session = await CreateSession(endpoint, userIdentity);

                if (session.Connected)
                {
                    Session = session;

                    // Keep alive
                    Session.KeepAliveInterval = KeepAliveInterval;
                    Session.KeepAlive += SessionOnKeepAlive;
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
    private ConfiguredEndpoint GetServerEndpoint(
        Uri serverUri,
        bool useSecurity)
    {
        var endpointDescription = CoreClientUtils.SelectEndpoint(serverUri.AbsoluteUri, useSecurity);
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

        //// ****
        //// Implement a custom logic to decide if the certificate should be accepted or not and set certificateAccepted flag accordingly.
        //// The certificate can be retrieved from the e.Certificate field
        //// ***

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
    private Task<Session> CreateSession(
        ConfiguredEndpoint endpoint,
        IUserIdentity userIdentity)
        => Session.Create(
            configuration,
            endpoint,
            updateBeforeConnect: false,
            checkDomain: false,
            configuration.ApplicationName + Guid.NewGuid(),
            SessionTimeout,
            userIdentity,
            preferredLocales: null);

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
    private async Task<ApplicationInstance> BuildAndValidateOpcUaApplicationAsync()
    {
        var applicationConfiguration = ApplicationConfigurationFactory.Create(ApplicationName, securityOptions);
        applicationConfiguration = ApplicationInstance.FixupAppConfig(applicationConfiguration);
        await applicationConfiguration.Validate(applicationConfiguration.ApplicationType);

        var application = new ApplicationInstance(applicationConfiguration);

        var hasAppCertificate = await application.CheckApplicationInstanceCertificate(
            silent: true,
            CertificateFactory.DefaultKeySize,
            CertificateFactory.DefaultHashSize);

        if (!hasAppCertificate)
        {
            throw new CertificateValidationException("OPC UA application certificate can not be validated");
        }

        return application;
    }

    /// <summary>
    /// Handles keep alive events for the session
    /// </summary>
    /// <param name="session">The session the keep alive event is received for</param>
    /// <param name="e">The event arguments</param>
    private void SessionOnKeepAlive(ISession session, KeepAliveEventArgs e)
    {
        try
        {
            // There is no current session
            if (Session is null)
            {
                return;
            }

            // The keep alive came from a different session
            if (!Session.Equals(session))
            {
                return;
            }

            // The server has not responded to the keep alive
            if (ServiceResult.IsBad(e.Status))
            {
                // Disconnect the session and ensure that there will be no further keep alive requests
                e.CancelKeepAlive = true;
                Disconnect();
            }
        }
        catch (Exception exception)
        {
            LogSessionKeepAliveRequestFailure(exception);
        }
    }
}