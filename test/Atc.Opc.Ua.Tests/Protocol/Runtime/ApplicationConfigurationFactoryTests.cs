namespace Atc.Opc.Ua.Tests.Protocol.Runtime;

public sealed class ApplicationConfigurationFactoryTests
{
    [Theory]
    [InlineData("OpcUaClient", "CN=OpcUaClient")]
    [InlineData("my-iot-certificate", "CN=my-iot-certificate")]
    [InlineData("CN=OpcUaClient", "CN=OpcUaClient")]
    [InlineData("CN=OpcUaClient, O=Company, C=DK", "CN=OpcUaClient, O=Company, C=DK")]
    public void ToSubjectName_Name_ShouldBeAnX500Name(
        string name,
        string expected)
        => ApplicationConfigurationFactory.ToSubjectName(name).Should().Be(expected);

    [Fact]
    public void Create_WithoutSubjectName_ShouldUseTheApplicationNameAsTheCommonName()
    {
        // Arrange
        var telemetry = new StackTelemetryContext(NullLoggerFactory.Instance);

        // Act
        var configuration = ApplicationConfigurationFactory.Create("OpcUaClient", new OpcUaSecurityOptions(), telemetry);

        // Assert
        configuration.SecurityConfiguration.ApplicationCertificate.SubjectName.Should().Be("CN=OpcUaClient");
    }
}