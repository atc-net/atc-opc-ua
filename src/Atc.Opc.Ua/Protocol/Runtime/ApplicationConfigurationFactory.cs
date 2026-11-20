namespace Atc.Opc.Ua.Protocol.Runtime;

public static class ApplicationConfigurationFactory
{
    public static ApplicationConfiguration Create(
        string applicationName,
        OpcUaSecurityOptions opcUaSecurityOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationName);
        ArgumentNullException.ThrowIfNull(opcUaSecurityOptions);

        var config = new ApplicationConfiguration
        {
            ApplicationName = applicationName,
            CertificateValidator = new CertificateValidator(),
            ApplicationType = ApplicationType.Client,
            ApplicationUri = string.Empty,
            ProductUri = string.Empty,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = opcUaSecurityOptions.ApplicationCertificate.StoreType,
                    StorePath = $"{opcUaSecurityOptions.PkiRootPath}/{opcUaSecurityOptions.ApplicationCertificate.StorePath}",
                    SubjectName = opcUaSecurityOptions.ApplicationCertificate.SubjectName ?? applicationName,
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

                // While OpcUaClient's certificate handler is registered, the OPC UA stack ignores this, and
                // ServerCertificateValidationPolicy.Decide applies it; it's kept so the stack's configuration matches the options.
                AutoAcceptUntrustedCertificates = opcUaSecurityOptions.AutoAcceptUntrustedCertificates,
                MinimumCertificateKeySize = opcUaSecurityOptions.MinimumCertificateKeySize,
                RejectSHA1SignedCertificates = opcUaSecurityOptions.RejectSha1SignedCertificates,
                RejectUnknownRevocationStatus = opcUaSecurityOptions.RejectUnknownRevocationStatus,
            },
            TransportConfigurations = new TransportConfigurationCollection(),
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
}