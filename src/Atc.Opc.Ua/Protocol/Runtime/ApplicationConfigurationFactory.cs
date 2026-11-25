namespace Atc.Opc.Ua.Protocol.Runtime;

public static class ApplicationConfigurationFactory
{
    public static ApplicationConfiguration Create(
        string applicationName,
        OpcUaSecurityOptions opcUaSecurityOptions,
        ITelemetryContext telemetry)
    {
        ArgumentNullException.ThrowIfNull(applicationName);
        ArgumentNullException.ThrowIfNull(opcUaSecurityOptions);
        ArgumentNullException.ThrowIfNull(telemetry);

        var config = new ApplicationConfiguration(telemetry)
        {
            ApplicationName = applicationName,
            ApplicationType = ApplicationType.Client,
            ApplicationUri = string.Empty,
            ProductUri = string.Empty,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = opcUaSecurityOptions.ApplicationCertificate.StoreType,
                    StorePath = $"{opcUaSecurityOptions.PkiRootPath}/{opcUaSecurityOptions.ApplicationCertificate.StorePath}",
                    SubjectName = ToSubjectName(opcUaSecurityOptions.ApplicationCertificate.SubjectName ?? applicationName),
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = opcUaSecurityOptions.RejectedCertificates.StoreType,
                    StorePath = $"{opcUaSecurityOptions.PkiRootPath}/{opcUaSecurityOptions.RejectedCertificates.StorePath}",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = opcUaSecurityOptions.TrustedIssuerCertificates.StoreType,
                    StorePath = $"{opcUaSecurityOptions.PkiRootPath}/{opcUaSecurityOptions.TrustedIssuerCertificates.StorePath}",
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = opcUaSecurityOptions.TrustedPeerCertificates.StoreType,
                    StorePath = $"{opcUaSecurityOptions.PkiRootPath}/{opcUaSecurityOptions.TrustedPeerCertificates.StorePath}",
                },
                AddAppCertToTrustedStore = opcUaSecurityOptions.AddAppCertToTrustedStore,
                AllowUnencryptedUserNameTokens = opcUaSecurityOptions.AllowUnencryptedUserNameTokens,
                AutoAcceptUntrustedCertificates = opcUaSecurityOptions.AutoAcceptUntrustedCertificates,
                MinimumCertificateKeySize = opcUaSecurityOptions.MinimumCertificateKeySize,
                RejectSHA1SignedCertificates = opcUaSecurityOptions.RejectSha1SignedCertificates,
                RejectUnknownRevocationStatus = opcUaSecurityOptions.RejectUnknownRevocationStatus,
            },
            TransportQuotas = TransportQuotaConfigExtensions.DefaultTransportQuotas(),
            ServerConfiguration = new ServerConfiguration(),
            ClientConfiguration = new ClientConfiguration(),
            TraceConfiguration = new TraceConfiguration
            {
                TraceMasks = 1,
            },
        };

        return config;
    }

    /// <summary>
    /// The certificate's subject as an X500 name, e.g. <c>CN=OpcUaClient</c>: a plain name becomes the common name, and
    /// a name that is already an X500 name, e.g. <c>CN=OpcUaClient, O=Company</c>, stays as it is.
    /// </summary>
    /// <remarks>
    /// The OPC UA stack uses the subject as it is when it creates the client's certificate, and a plain name isn't a
    /// valid X500 name, so creating the certificate would fail on the first start, when there is no certificate yet.
    /// Finding an existing certificate works with either form.
    /// </remarks>
    /// <param name="name">The subject name, or a plain name.</param>
    /// <returns>The X500 name.</returns>
    internal static string ToSubjectName(string name)
        => name.Contains('=', StringComparison.Ordinal)
            ? name
            : $"CN={name}";
}