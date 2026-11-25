namespace Atc.Opc.Ua.Tests.Services;

public sealed class OpcUaClientApplicationUriTests : IDisposable
{
    private readonly string pkiRootPath = Path.Combine(Path.GetTempPath(), "atc-opc-ua-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void Constructor_ExistingCertificateAndNoApplicationUri_ShouldUseTheCertificate()
    {
        // Arrange
        var certificateFileName = CreateCertificateMadeByAnotherTool(
            "CN=existing-certificate",
            "urn:localhost:existing-certificate");

        var securityOptions = new OpcUaSecurityOptions
        {
            PkiRootPath = pkiRootPath,
            ApplicationCertificate = { SubjectName = "existing-certificate" },
        };

        // Act
        using var client = new OpcUaClient(
            NullLogger<OpcUaClient>.Instance,
            new OptionsWrapper<OpcUaSecurityOptions>(securityOptions));

        // Assert
        Directory.GetFiles(Path.Combine(pkiRootPath, "own", "certs"))
            .Select(Path.GetFileName)
            .Should().Equal(certificateFileName);
    }

    public void Dispose()
    {
        if (Directory.Exists(pkiRootPath))
        {
            Directory.Delete(pkiRootPath, recursive: true);
        }
    }

    /// <summary>
    /// Puts a certificate into the client's own store, made the way a tool other than the OPC UA stack would make it:
    /// self-signed, its Authority Key Identifier pointing to its own key, and allowed to sign certificates, as OPC UA
    /// expects of a self-signed application certificate.
    /// </summary>
    /// <param name="subjectName">The certificate's subject.</param>
    /// <param name="applicationUri">The application URI in the certificate.</param>
    /// <returns>The certificate's file name in the store.</returns>
    private string CreateCertificateMadeByAnotherTool(
        string subjectName,
        string applicationUri)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: false));
        var subjectKeyIdentifier = new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false);
        request.CertificateExtensions.Add(subjectKeyIdentifier);
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromSubjectKeyIdentifier(subjectKeyIdentifier));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyCertSign,
            critical: false));

        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            [new Oid("1.3.6.1.5.5.7.3.2"), new Oid("1.3.6.1.5.5.7.3.1")],
            critical: false));

        var subjectAlternativeName = new SubjectAlternativeNameBuilder();
        subjectAlternativeName.AddUri(new Uri(applicationUri));
        request.CertificateExtensions.Add(subjectAlternativeName.Build());

        var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
        using var certificate = request.CreateSelfSigned(notBefore, notBefore.AddYears(1));

        var fileName = $"existing-certificate [{certificate.Thumbprint}]";
        var certificatesPath = Directory.CreateDirectory(Path.Combine(pkiRootPath, "own", "certs")).FullName;
        var privateKeysPath = Directory.CreateDirectory(Path.Combine(pkiRootPath, "own", "private")).FullName;
        File.WriteAllBytes(Path.Combine(certificatesPath, fileName + ".der"), certificate.Export(X509ContentType.Cert));
        File.WriteAllBytes(Path.Combine(privateKeysPath, fileName + ".pfx"), certificate.Export(X509ContentType.Pfx));

        return fileName + ".der";
    }
}