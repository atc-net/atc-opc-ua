namespace Atc.Opc.Ua.Options;

public sealed class OpcUaSecurityOptions
{
    /// <summary>
    /// PkiRootPath.
    /// </summary>
    public string PkiRootPath { get; set; } = "opc/pki";

    /// <summary>
    /// Application Certificate.
    /// </summary>
    public CertificateInfo ApplicationCertificate { get; set; } = new()
    {
        StoreType = CertificateStoreType.Directory,
        StorePath = "own",
    };

    /// <summary>
    /// Rejected certificates.
    /// </summary>
    public CertificateStore RejectedCertificates { get; set; } = new()
    {
        StoreType = CertificateStoreType.Directory,
        StorePath = "rejected",
    };

    /// <summary>
    /// Trusted issuer certificates.
    /// </summary>
    public CertificateStore TrustedIssuerCertificates { get; set; } = new()
    {
        StoreType = CertificateStoreType.Directory,
        StorePath = "issuers",
    };

    /// <summary>
    /// Trusted peer certificates.
    /// </summary>
    public CertificateStore TrustedPeerCertificates { get; set; } = new()
    {
        StoreType = CertificateStoreType.Directory,
        StorePath = "trusted",
    };

    /// <summary>
    /// Automatically add application certificate to the trusted store.
    /// </summary>
    public bool AddAppCertToTrustedStore { get; set; } = true;

    /// <summary>
    /// Whether to auto accept untrusted certificates.
    /// </summary>
    public bool AutoAcceptUntrustedCertificates { get; set; }

    /// <summary>
    /// Minimum key size.
    /// </summary>
    public ushort MinimumCertificateKeySize { get; set; } = 1024;

    /// <summary>
    /// Whether to reject unsecure signatures.
    /// </summary>
    public bool RejectSha1SignedCertificates { get; set; } = true;

    /// <summary>
    /// Reject chain validation with CA certs with unknown revocation status,
    /// e.g.when the CRL is not available or the OCSP provider is offline.
    /// </summary>
    public bool RejectUnknownRevocationStatus { get; set; } = true;
}