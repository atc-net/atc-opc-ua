namespace Atc.Opc.Ua.CLI.Helpers;

/// <summary>
/// Creates the client for a command, with the certificate settings from the command line.
/// </summary>
/// <remarks>
/// The client is created when the command runs, not injected: the command line is only parsed by then, and the client
/// reads its security options when it's created.
/// </remarks>
internal sealed class OpcUaClientFactory
{
    private readonly ILoggerFactory loggerFactory;

    public OpcUaClientFactory(ILoggerFactory loggerFactory)
        => this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

    /// <summary>
    /// Creates a client with the command's certificate settings.
    /// </summary>
    /// <param name="settings">The command's settings.</param>
    /// <returns>The client; the caller disposes it.</returns>
    public IOpcUaClient Create(OpcUaBaseCommandSettings settings)
        => new OpcUaClient(
            loggerFactory.CreateLogger<OpcUaClient>(),
            new OptionsWrapper<OpcUaSecurityOptions>(CreateSecurityOptions(settings)));

    /// <summary>
    /// Maps the command's certificate settings to the client's security options.
    /// </summary>
    /// <param name="settings">The command's settings.</param>
    /// <returns>The security options.</returns>
    public static OpcUaSecurityOptions CreateSecurityOptions(
        OpcUaBaseCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new OpcUaSecurityOptions
        {
            AutoAcceptUntrustedCertificates = settings.AutoAccept,
            ServerCertificateValidation = settings.CertificateValidation,
        };
    }
}