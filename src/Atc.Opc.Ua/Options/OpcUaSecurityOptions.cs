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
    public bool AddAppCertToTrustedStore { get; set; }

    /// <summary>
    /// How the certificate a server presents is checked. Defaults to <see cref="Options.ServerCertificateValidation.Validate"/>.
    /// </summary>
    public ServerCertificateValidation ServerCertificateValidation { get; set; } = ServerCertificateValidation.Validate;

    /// <summary>
    /// Whether to accept a server certificate that isn't in the trusted store yet. It accepts only "untrusted": an
    /// expired certificate, a key below <see cref="MinimumCertificateKeySize"/> or a CA that isn't known is still
    /// refused. It applies with every <see cref="ServerCertificateValidation"/> value, so with
    /// <see cref="Options.ServerCertificateValidation.Audit"/> an untrusted certificate is accepted without a warning.
    /// </summary>
    public bool AutoAcceptUntrustedCertificates { get; set; }

    /// <summary>
    /// Minimum key size.
    /// </summary>
    public ushort MinimumCertificateKeySize { get; set; } = 2048;

    /// <summary>
    /// Whether to reject unsecure signatures.
    /// </summary>
    public bool RejectSha1SignedCertificates { get; set; } = true;

    /// <summary>
    /// Reject chain validation with CA certs with unknown revocation status,
    /// e.g.when the CRL is not available or the OCSP provider is offline.
    /// </summary>
    public bool RejectUnknownRevocationStatus { get; set; } = true;

    /// <summary>
    /// Whether a user name and password may be sent when they wouldn't be encrypted: the server's user token policy
    /// doesn't encrypt them, and neither does the connection. Off by default, so they're never sent in plain text.
    /// </summary>
    public bool AllowUnencryptedUserNameTokens { get; set; }
}