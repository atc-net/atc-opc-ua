// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OK - By Design")]
public partial class OpcUaClient : IOpcUaClient
{
    private const uint SessionTimeout = 30 * 60 * 1000;
    private const string ApplicationName = nameof(OpcUaClient);
    private readonly ApplicationConfiguration configuration;

    public Session? Session { get; private set; }

    public OpcUaClient(
        ILogger<OpcUaClient> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        var application = BuildAndValidateOpcUaApplicationAsync().GetAwaiter().GetResult();
        this.configuration = application.ApplicationConfiguration;
        this.configuration.CertificateValidator.CertificateValidation += CertificateValidation;
    }

    public bool IsConnected()
        => Session is not null && Session.Connected;

    public (bool Succeeded, string? ErrorMessage) Disconnect()
    {
        try
        {
            if (Session != null)
            {
                var sessionName = Session.SessionName ?? "unknown";
                LogSessionDisconnecting(sessionName);

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

    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri)
        => ConnectAsync(serverUri, string.Empty, string.Empty);

    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string userName,
        string password)
    {
        ArgumentNullException.ThrowIfNull(serverUri);

        return InvokeConnectAsync(serverUri, userName, password);
    }

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
                LogSessionConnecting();

                var endpoint = GetServerEndpoint(serverUri);
                var userIdentity = BuildUserIdentity(userName, password);
                var session = await CreateSession(endpoint, userIdentity);

                if (session.Connected)
                {
                    Session = session;
                }

                LogSessionConnected(Session?.SessionName ?? "unknown");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            LogSessionConnectionFailure(ex.Message);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Get the endpoint by connecting to server's discovery endpoint.
    /// </summary>
    /// <param name="serverUri">The server to get endpoint from.</param>
    /// <returns>The ConfiguredEndpoint on the server.</returns>
    private ConfiguredEndpoint GetServerEndpoint(
        Uri serverUri)
    {
        var endpointDescription = CoreClientUtils.SelectEndpoint(serverUri.AbsoluteUri, useSecurity: true);
        var endpointConfiguration = EndpointConfiguration.Create(configuration);
        var endpoint = new ConfiguredEndpoint(collection: null, endpointDescription, endpointConfiguration);
        return endpoint;
    }

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

    private static async Task<ApplicationInstance> BuildAndValidateOpcUaApplicationAsync()
    {
        var applicationConfiguration = ApplicationConfigurationFactory.Create(ApplicationName);
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
}