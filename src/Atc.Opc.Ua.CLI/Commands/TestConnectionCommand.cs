namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class TestConnectionCommand : AsyncCommand<OpcUaBaseCommandSettings>
{
    /// <summary>
    /// The object identifier of the X.509 subject alternative name extension (RFC 5280, section 4.2.1.6).
    /// </summary>
    private const string SubjectAlternativeNameOid = "2.5.29.17";

    private readonly OpcUaClientFactory clientFactory;
    private readonly ILogger<TestConnectionCommand> logger;

    public TestConnectionCommand(
        OpcUaClientFactory clientFactory,
        ILogger<TestConnectionCommand> logger)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        OpcUaBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        OpcUaBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        return await OpcUaCommandRunner.RunAsync(
            clientFactory,
            settings,
            logger,
            (client, _) =>
            {
                if (client.Session?.Endpoint is { } endpoint)
                {
                    var userNameSent = client.Session.Identity?.TokenType == global::Opc.Ua.UserTokenType.UserName;
                    foreach (var line in DescribeEndpoint(endpoint, userNameSent))
                    {
                        logger.LogInformation(line);
                    }
                }

                return Task.CompletedTask;
            },
            cancellationToken);
    }

    /// <summary>
    /// Describes the endpoint a connection uses, the ways the server accepts a user on it, and the certificate the
    /// server presents on it.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <param name="userNameSent">
    /// Whether the connection sent a user name. Then the server's certificate is checked on an endpoint without security
    /// too, because the password is encrypted with it.
    /// </param>
    /// <returns>The lines to show.</returns>
    internal static List<string> DescribeEndpoint(
        global::Opc.Ua.EndpointDescription endpoint,
        bool userNameSent = false)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var lines = new List<string>
        {
            $"Endpoint: {endpoint.EndpointUrl} ({endpoint.SecurityMode}, {GetPolicyName(endpoint.SecurityPolicyUri)})",
        };

        if (endpoint.SecurityMode == global::Opc.Ua.MessageSecurityMode.None)
        {
            lines.Add(userNameSent
                ? "No security on this endpoint, but the server's certificate is checked, because the password is encrypted with it."
                : "No security on this endpoint: the server's certificate isn't checked.");
        }

        if (endpoint.UserIdentityTokens.Count > 0)
        {
            lines.Add("User tokens:");
            foreach (var userTokenPolicy in endpoint.UserIdentityTokens)
            {
                lines.Add($"  {DescribeUserTokenPolicy(userTokenPolicy, endpoint)}");
            }
        }

        if (endpoint.ServerCertificate is not { Length: > 0 })
        {
            lines.Add("The server presents no certificate.");
            return lines;
        }

        using var chain = global::Opc.Ua.Utils.ParseCertificateChainBlob(endpoint.ServerCertificate, NullTelemetryContext.Instance);
        var certificate = chain[0];
        var selfSigned = string.Equals(certificate.Subject, certificate.Issuer, StringComparison.Ordinal);
        var applicationUris = global::Opc.Ua.X509Utils.GetApplicationUrisFromCertificate(certificate);

        lines.Add("Server certificate:");
        lines.Add($"  Subject:     {certificate.Subject}");
        lines.Add($"  Issuer:      {(selfSigned ? "self-signed" : certificate.Issuer)}");
        lines.Add($"  Thumbprint:  {certificate.Thumbprint}");
        lines.Add($"  Valid:       {FormatUtc(certificate.NotBefore)} to {FormatUtc(certificate.NotAfter)}");
        lines.Add($"  Key:         {DescribeKey(certificate)}");
        lines.Add($"  URI:         {(applicationUris.Count > 0 ? string.Join(", ", applicationUris) : "none")}");
        lines.Add($"  Host names:  {GetHostNames(certificate)}");
        if (chain.Count > 1)
        {
            lines.Add($"  CA certificates sent with it: {chain.Count - 1}");
        }

        return lines;
    }

    /// <summary>
    /// Describes one way the server accepts a user, and for a user name, how the password travels to the server.
    /// </summary>
    /// <param name="userTokenPolicy">The user token policy.</param>
    /// <param name="endpoint">The endpoint the policy belongs to.</param>
    /// <returns>The line to show.</returns>
    internal static string DescribeUserTokenPolicy(
        global::Opc.Ua.UserTokenPolicy userTokenPolicy,
        global::Opc.Ua.EndpointDescription endpoint)
    {
        ArgumentNullException.ThrowIfNull(userTokenPolicy);
        ArgumentNullException.ThrowIfNull(endpoint);

        // A user token policy without a security policy of its own uses the endpoint's.
        var securityPolicyUri = string.IsNullOrEmpty(userTokenPolicy.SecurityPolicyUri)
            ? endpoint.SecurityPolicyUri
            : userTokenPolicy.SecurityPolicyUri;

        if (userTokenPolicy.TokenType != global::Opc.Ua.UserTokenType.UserName)
        {
            return userTokenPolicy.TokenType == global::Opc.Ua.UserTokenType.Anonymous
                ? "Anonymous"
                : $"{userTokenPolicy.TokenType} ({GetPolicyName(securityPolicyUri)})";
        }

        var passwordEncrypted = !string.IsNullOrEmpty(securityPolicyUri) &&
                                !string.Equals(securityPolicyUri, global::Opc.Ua.SecurityPolicies.None, StringComparison.Ordinal);

        if (passwordEncrypted)
        {
            return $"UserName: the password is encrypted ({GetPolicyName(securityPolicyUri)})";
        }

        return endpoint.SecurityMode == global::Opc.Ua.MessageSecurityMode.SignAndEncrypt
            ? "UserName: the password is protected by the encrypted channel"
            : "UserName: the password would be sent as it is, which the client refuses unless AllowUnencryptedUserNameTokens is on";
    }

    /// <summary>
    /// The short name of a security policy, e.g. <c>Basic256Sha256</c>.
    /// </summary>
    private static string GetPolicyName(string? securityPolicyUri)
        => string.IsNullOrEmpty(securityPolicyUri)
            ? "unknown"
            : securityPolicyUri.Split('#')[^1];

    /// <summary>
    /// The DNS names and IP addresses in the certificate's subject alternative name, as written.
    /// </summary>
    private static string GetHostNames(
        global::Opc.Ua.Security.Certificates.Certificate certificate)
    {
        var extension = certificate.Extensions[SubjectAlternativeNameOid];
        if (extension is null)
        {
            return "none";
        }

        var subjectAlternativeName = new X509SubjectAlternativeNameExtension(extension.RawData, extension.Critical);
        var hostNames = subjectAlternativeName
            .EnumerateDnsNames()
            .Concat(subjectAlternativeName.EnumerateIPAddresses().Select(address => address.ToString()))
            .ToList();

        return hostNames.Count > 0 ? string.Join(", ", hostNames) : "none";
    }

    private static string FormatUtc(DateTime dateTime)
        => dateTime.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " UTC";

    private static string DescribeKey(
        global::Opc.Ua.Security.Certificates.Certificate certificate)
    {
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is not null)
        {
            return $"RSA {rsa.KeySize} bit, signed with {certificate.SignatureAlgorithm.FriendlyName}";
        }

        using var ecdsa = certificate.GetECDsaPublicKey();
        return ecdsa is not null
            ? $"ECDSA {ecdsa.KeySize} bit, signed with {certificate.SignatureAlgorithm.FriendlyName}"
            : $"{certificate.PublicKey.Oid.FriendlyName}, signed with {certificate.SignatureAlgorithm.FriendlyName}";
    }

    /// <summary>
    /// A telemetry context that logs nothing, for parsing the server's certificate chain.
    /// </summary>
    private sealed class NullTelemetryContext : global::Opc.Ua.TelemetryContextBase
    {
        private NullTelemetryContext()
            : base(Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
        {
        }

        public static NullTelemetryContext Instance { get; } = new();
    }
}