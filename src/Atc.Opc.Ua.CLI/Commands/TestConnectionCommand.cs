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
                    foreach (var line in DescribeEndpoint(endpoint))
                    {
                        logger.LogInformation(line);
                    }
                }

                return Task.CompletedTask;
            },
            cancellationToken);
    }

    /// <summary>
    /// Describes the endpoint a connection uses, and the certificate the server presents on it.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <returns>The lines to show.</returns>
    internal static List<string> DescribeEndpoint(
        global::Opc.Ua.EndpointDescription endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var policy = endpoint.SecurityPolicyUri?.Split('#')[^1] ?? "unknown";
        var lines = new List<string>
        {
            $"Endpoint: {endpoint.EndpointUrl} ({endpoint.SecurityMode}, {policy})",
        };

        if (endpoint.SecurityMode == global::Opc.Ua.MessageSecurityMode.None)
        {
            lines.Add("No security on this endpoint: the server's certificate isn't checked.");
        }

        if (endpoint.ServerCertificate is not { Length: > 0 })
        {
            lines.Add("The server presents no certificate.");
            return lines;
        }

        var chain = global::Opc.Ua.Utils.ParseCertificateChainBlob(endpoint.ServerCertificate, useAsnParser: false);
        var certificate = chain[0];
        var selfSigned = string.Equals(certificate.Subject, certificate.Issuer, StringComparison.Ordinal);

        lines.Add("Server certificate:");
        lines.Add($"  Subject:     {certificate.Subject}");
        lines.Add($"  Issuer:      {(selfSigned ? "self-signed" : certificate.Issuer)}");
        lines.Add($"  Thumbprint:  {certificate.Thumbprint}");
        lines.Add($"  Valid:       {FormatUtc(certificate.NotBefore)} to {FormatUtc(certificate.NotAfter)}");
        lines.Add($"  Key:         {DescribeKey(certificate)}");
        var applicationUri = global::Opc.Ua.X509Utils.GetApplicationUriFromCertificate(certificate) ?? "none";
        lines.Add($"  URI:         {applicationUri}");
        lines.Add($"  Host names:  {GetHostNames(certificate)}");
        if (chain.Count > 1)
        {
            lines.Add($"  CA certificates sent with it: {chain.Count - 1}");
        }

        return lines;
    }

    /// <summary>
    /// The DNS names and IP addresses in the certificate's subject alternative name, as written.
    /// </summary>
    private static string GetHostNames(X509Certificate2 certificate)
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

    private static string DescribeKey(X509Certificate2 certificate)
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
}