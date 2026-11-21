namespace Atc.Opc.Ua.CLI.Tests.Helpers;

public sealed class OpcUaClientFactoryTests
{
    private const string ServerUrl = "opc.tcp://localhost:50000";

    [Fact]
    public void CreateSecurityOptions_NoCertificateFlags_ShouldValidateWithoutAutoAccept()
    {
        // Arrange
        var settings = new OpcUaBaseCommandSettings { ServerUrl = ServerUrl };

        // Act
        var options = OpcUaClientFactory.CreateSecurityOptions(settings);

        // Assert
        options.AutoAcceptUntrustedCertificates.Should().BeFalse();
        options.ServerCertificateValidation.Should().Be(ServerCertificateValidation.Validate);
    }

    [Fact]
    public void CreateSecurityOptions_AutoAcceptAndAudit_ShouldSetBoth()
    {
        // Arrange
        var settings = new OpcUaBaseCommandSettings
        {
            ServerUrl = ServerUrl,
            AutoAccept = true,
            CertificateValidation = ServerCertificateValidation.Audit,
        };

        // Act
        var options = OpcUaClientFactory.CreateSecurityOptions(settings);

        // Assert
        options.AutoAcceptUntrustedCertificates.Should().BeTrue();
        options.ServerCertificateValidation.Should().Be(ServerCertificateValidation.Audit);
    }

    [Fact]
    public void Validate_CertificateValidationNotAValue_ShouldFail()
        => new OpcUaBaseCommandSettings { ServerUrl = ServerUrl, CertificateValidation = default }
            .Validate().Successful.Should().BeFalse();

    [Theory]
    [InlineData(ServerCertificateValidation.AcceptAny)]
    [InlineData(ServerCertificateValidation.Audit)]
    [InlineData(ServerCertificateValidation.Validate)]
    public void Validate_CertificateValidationValue_ShouldSucceed(
        ServerCertificateValidation validation)
        => new OpcUaBaseCommandSettings { ServerUrl = ServerUrl, CertificateValidation = validation }
            .Validate().Successful.Should().BeTrue();
}