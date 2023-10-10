namespace Atc.Opc.Ua.Protocol.Runtime;

public static class ApplicationConfigurationFactory
{
    public static ApplicationConfiguration Create(
        string applicationName,
        OpcUaSecurityOptions opaUaSecurityOptions)
    {
        ArgumentNullException.ThrowIfNull(applicationName);
        ArgumentNullException.ThrowIfNull(opaUaSecurityOptions);

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
                    StoreType = opaUaSecurityOptions.ApplicationCertificate.StoreType,
                    StorePath = $"{opaUaSecurityOptions.PkiRootPath}/{opaUaSecurityOptions.ApplicationCertificate.StorePath}",
                    SubjectName = opaUaSecurityOptions.ApplicationCertificate.SubjectName ?? applicationName,
                },
                RejectedCertificateStore = new CertificateTrustList
                {
                    StoreType = opaUaSecurityOptions.RejectedCertificates.StoreType,
                    StorePath = $"{opaUaSecurityOptions.PkiRootPath}/{opaUaSecurityOptions.RejectedCertificates.StorePath}",
                },
                TrustedIssuerCertificates = new CertificateTrustList
                {
                    StoreType = opaUaSecurityOptions.TrustedIssuerCertificates.StoreType,
                    StorePath = $"{opaUaSecurityOptions.PkiRootPath}/{opaUaSecurityOptions.TrustedIssuerCertificates.StorePath}",
                },
                TrustedPeerCertificates = new CertificateTrustList
                {
                    StoreType = opaUaSecurityOptions.TrustedPeerCertificates.StoreType,
                    StorePath = $"{opaUaSecurityOptions.PkiRootPath}/{opaUaSecurityOptions.TrustedPeerCertificates.StorePath}",
                },
                AddAppCertToTrustedStore = opaUaSecurityOptions.AddAppCertToTrustedStore,
                AutoAcceptUntrustedCertificates = opaUaSecurityOptions.AutoAcceptUntrustedCertificates,
                MinimumCertificateKeySize = opaUaSecurityOptions.MinimumCertificateKeySize,
                RejectSHA1SignedCertificates = opaUaSecurityOptions.RejectSha1SignedCertificates,
                RejectUnknownRevocationStatus = opaUaSecurityOptions.RejectUnknownRevocationStatus,
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