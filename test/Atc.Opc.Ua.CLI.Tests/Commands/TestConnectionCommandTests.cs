namespace Atc.Opc.Ua.CLI.Tests.Commands;

public sealed class TestConnectionCommandTests
{
    private const string EndpointUrl = "opc.tcp://plc:4840";
    private const string Basic256Sha256 = "http://opcfoundation.org/UA/SecurityPolicy#Basic256Sha256";

    [Fact]
    public void DescribeEndpoint_NoSecurityAndNoCertificate_ShouldSayNothingIsChecked()
    {
        // Arrange
        var endpoint = new EndpointDescription
        {
            EndpointUrl = EndpointUrl,
            SecurityMode = MessageSecurityMode.None,
            SecurityPolicyUri = SecurityPolicies.None,
        };

        // Act
        var lines = TestConnectionCommand.DescribeEndpoint(endpoint);

        // Assert
        lines.Should().Equal(
            $"Endpoint: {EndpointUrl} (None, None)",
            "No security on this endpoint: the server's certificate isn't checked.",
            "The server presents no certificate.");
    }

    [Fact]
    public void DescribeEndpoint_NoSecurityAndAUserNameSent_ShouldSayTheCertificateIsChecked()
    {
        // Arrange
        var endpoint = new EndpointDescription
        {
            EndpointUrl = EndpointUrl,
            SecurityMode = MessageSecurityMode.None,
            SecurityPolicyUri = SecurityPolicies.None,
        };

        // Act
        var lines = TestConnectionCommand.DescribeEndpoint(endpoint, userNameSent: true);

        // Assert
        lines.Should().Contain("No security on this endpoint, but the server's certificate is checked, because the password is encrypted with it.");
    }

    [Fact]
    public void DescribeEndpoint_SelfSignedCertificate_ShouldDescribeIt()
    {
        // Arrange
        using var key = RSA.Create(2048);
        using var certificate = CreateCertificate("CN=PLC", key, issuer: null);
        var endpoint = new EndpointDescription
        {
            EndpointUrl = EndpointUrl,
            SecurityMode = MessageSecurityMode.SignAndEncrypt,
            SecurityPolicyUri = Basic256Sha256,
            ServerCertificate = ByteString.From(certificate.RawData),
        };

        // Act
        var lines = TestConnectionCommand.DescribeEndpoint(endpoint);

        // Assert
        lines.Should().Contain($"Endpoint: {EndpointUrl} (SignAndEncrypt, Basic256Sha256)");
        lines.Should().Contain("  Subject:     CN=PLC");
        lines.Should().Contain("  Issuer:      self-signed");
        lines.Should().Contain($"  Thumbprint:  {certificate.Thumbprint}");
        lines.Should().Contain("  Valid:       2025-09-25 11:53 UTC to 2037-12-02 01:00 UTC");
        lines.Should().Contain(l => l.StartsWith("  Key:         RSA 2048 bit, signed with ", StringComparison.Ordinal));
        lines.Should().Contain("  URI:         urn:plc:server");
        lines.Should().Contain("  Host names:  plc-host, 192.168.22.20");
        lines.Should().NotContain(l => l.Contains("No security", StringComparison.Ordinal));
    }

    [Fact]
    public void DescribeEndpoint_CertificateSentWithItsCa_ShouldNameTheIssuerAndCountTheCa()
    {
        // Arrange
        using var caKey = RSA.Create(2048);
        using var ca = CreateCertificate("CN=Project CA", caKey, issuer: null, isCa: true);
        using var leafKey = RSA.Create(2048);
        using var leaf = CreateCertificate("CN=PLC", leafKey, issuer: ca);
        var endpoint = new EndpointDescription
        {
            EndpointUrl = EndpointUrl,
            SecurityMode = MessageSecurityMode.Sign,
            SecurityPolicyUri = Basic256Sha256,
            ServerCertificate = ByteString.From([.. leaf.RawData, .. ca.RawData]),
        };

        // Act
        var lines = TestConnectionCommand.DescribeEndpoint(endpoint);

        // Assert
        lines.Should().Contain("  Subject:     CN=PLC");
        lines.Should().Contain("  Issuer:      CN=Project CA");
        lines.Should().Contain("  CA certificates sent with it: 1");
    }

    [Fact]
    public void DescribeEndpoint_UserTokens_ShouldListThemBeforeTheCertificate()
    {
        // Arrange
        var endpoint = new EndpointDescription
        {
            EndpointUrl = EndpointUrl,
            SecurityMode = MessageSecurityMode.None,
            SecurityPolicyUri = SecurityPolicies.None,
            UserIdentityTokens =
            [
                new UserTokenPolicy { TokenType = UserTokenType.Anonymous },
                new UserTokenPolicy { TokenType = UserTokenType.UserName, SecurityPolicyUri = Basic256Sha256 },
            ],
        };

        // Act
        var lines = TestConnectionCommand.DescribeEndpoint(endpoint);

        // Assert
        lines.Should().Equal(
            $"Endpoint: {EndpointUrl} (None, None)",
            "No security on this endpoint: the server's certificate isn't checked.",
            "User tokens:",
            "  Anonymous",
            "  UserName: the password is encrypted (Basic256Sha256)",
            "The server presents no certificate.");
    }

    [Fact]
    public void DescribeUserTokenPolicy_UserNameWithoutItsOwnSecurityPolicy_ShouldUseTheEndpointsPolicy()
    {
        // Arrange
        var endpoint = new EndpointDescription
        {
            SecurityMode = MessageSecurityMode.Sign,
            SecurityPolicyUri = Basic256Sha256,
        };
        var userTokenPolicy = new UserTokenPolicy { TokenType = UserTokenType.UserName };

        // Act
        var line = TestConnectionCommand.DescribeUserTokenPolicy(userTokenPolicy, endpoint);

        // Assert
        line.Should().Be("UserName: the password is encrypted (Basic256Sha256)");
    }

    [Fact]
    public void DescribeUserTokenPolicy_UserNameWithPolicyNoneOnSignAndEncrypt_ShouldSayTheChannelProtectsThePassword()
    {
        // Arrange
        var endpoint = new EndpointDescription
        {
            SecurityMode = MessageSecurityMode.SignAndEncrypt,
            SecurityPolicyUri = Basic256Sha256,
        };
        var userTokenPolicy = new UserTokenPolicy { TokenType = UserTokenType.UserName, SecurityPolicyUri = SecurityPolicies.None };

        // Act
        var line = TestConnectionCommand.DescribeUserTokenPolicy(userTokenPolicy, endpoint);

        // Assert
        line.Should().Be("UserName: the password is protected by the encrypted channel");
    }

    [Theory]
    [InlineData(MessageSecurityMode.None)]
    [InlineData(MessageSecurityMode.Sign)]
    public void DescribeUserTokenPolicy_UserNameWithPolicyNoneWithoutEncryption_ShouldNameTheOption(
        MessageSecurityMode securityMode)
    {
        // Arrange
        var endpoint = new EndpointDescription
        {
            SecurityMode = securityMode,
            SecurityPolicyUri = securityMode == MessageSecurityMode.None ? SecurityPolicies.None : Basic256Sha256,
        };
        var userTokenPolicy = new UserTokenPolicy { TokenType = UserTokenType.UserName, SecurityPolicyUri = SecurityPolicies.None };

        // Act
        var line = TestConnectionCommand.DescribeUserTokenPolicy(userTokenPolicy, endpoint);

        // Assert
        line.Should().Be("UserName: the password would be sent as it is, which the client refuses unless AllowUnencryptedUserNameTokens is on");
    }

    [Fact]
    public void DescribeUserTokenPolicy_Certificate_ShouldNameItsSecurityPolicy()
    {
        // Arrange
        var endpoint = new EndpointDescription
        {
            SecurityMode = MessageSecurityMode.SignAndEncrypt,
            SecurityPolicyUri = Basic256Sha256,
        };
        var userTokenPolicy = new UserTokenPolicy { TokenType = UserTokenType.Certificate };

        // Act
        var line = TestConnectionCommand.DescribeUserTokenPolicy(userTokenPolicy, endpoint);

        // Assert
        line.Should().Be("Certificate (Basic256Sha256)");
    }

    private static X509Certificate2 CreateCertificate(
        string subject,
        RSA key,
        X509Certificate2? issuer,
        bool isCa = false)
    {
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(isCa, hasPathLengthConstraint: false, 0, critical: true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));
        if (!isCa)
        {
            var san = new SubjectAlternativeNameBuilder();
            san.AddUri(new Uri("urn:plc:server"));
            san.AddDnsName("plc-host");
            san.AddIpAddress(IPAddress.Parse("192.168.22.20"));
            request.CertificateExtensions.Add(san.Build());
        }

        var notBefore = new DateTimeOffset(2025, 9, 25, 11, 53, 0, TimeSpan.Zero);
        var notAfter = new DateTimeOffset(2037, 12, 2, 1, 0, 0, TimeSpan.Zero);
        if (issuer is null)
        {
            return request.CreateSelfSigned(notBefore, notAfter);
        }

        using var issuerKey = issuer.GetRSAPrivateKey();
        var generator = X509SignatureGenerator.CreateForRSA(issuerKey, RSASignaturePadding.Pkcs1);
        return request.Create(issuer.SubjectName, generator, notBefore, notAfter, [1, 2, 3, 4]);
    }
}