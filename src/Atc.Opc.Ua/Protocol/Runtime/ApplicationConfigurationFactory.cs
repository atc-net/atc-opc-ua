namespace Atc.Opc.Ua.Protocol.Runtime;

public static class ApplicationConfigurationFactory
{
    public static ApplicationConfiguration Create(
        string applicationName)
    {
        ArgumentNullException.ThrowIfNull(applicationName);

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
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "opc/pki/own",
                    SubjectName = applicationName,
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "opc/pki/trusted",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "opc/pki/issuers",
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = CertificateStoreType.Directory,
                    StorePath = "opc/pki/rejected",
                },
                MinimumCertificateKeySize = 1024,
                RejectSHA1SignedCertificates = false,
                AddAppCertToTrustedStore = true,
                AutoAcceptUntrustedCertificates = false,
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