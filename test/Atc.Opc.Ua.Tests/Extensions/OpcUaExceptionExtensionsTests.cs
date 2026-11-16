namespace Atc.Opc.Ua.Tests.Extensions;

public sealed class OpcUaExceptionExtensionsTests
{
    [Theory]
    [InlineData(StatusCodes.BadNodeIdUnknown)]
    [InlineData(StatusCodes.BadUserAccessDenied)]
    [InlineData(StatusCodes.BadNotReadable)]
    [InlineData(StatusCodes.BadSecurityModeInsufficient)]
    [InlineData(StatusCodes.BadAttributeIdInvalid)]
    public void IsNodeFailure_StatusCodeAboutTheNode_ShouldReturnTrue(
        uint statusCode)
        => new ServiceResultException(statusCode).IsNodeFailure().Should().BeTrue();

    [Theory]
    [InlineData(StatusCodes.BadConnectionClosed)]
    [InlineData(StatusCodes.BadRequestTimeout)]
    [InlineData(StatusCodes.BadSecureChannelClosed)]
    [InlineData(StatusCodes.BadRequestInterrupted)]
    [InlineData(StatusCodes.BadUnexpectedError)]
    public void IsNodeFailure_StatusCodeNotAboutTheNode_ShouldReturnFalse(
        uint statusCode)
        => new ServiceResultException(statusCode).IsNodeFailure().Should().BeFalse();

    [Fact]
    public void IsNodeFailure_StatusCodeWithInfoBits_ShouldIgnoreTheInfoBits()
    {
        // Arrange
        const uint overflowInfoBits = 0x00000480;
        var exception = new ServiceResultException(StatusCodes.BadUserAccessDenied | overflowInfoBits);

        // Act
        var result = exception.IsNodeFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(StatusCodes.BadConnectionClosed)]
    [InlineData(StatusCodes.BadSecureChannelClosed)]
    [InlineData(StatusCodes.BadNotConnected)]
    [InlineData(StatusCodes.BadRequestTimeout)]
    [InlineData(StatusCodes.BadTimeout)]
    [InlineData(StatusCodes.BadSessionIdInvalid)]
    public void IsConnectionFailure_StatusCodeOfTheConnection_ShouldReturnTrue(
        uint statusCode)
        => new ServiceResultException(statusCode).IsConnectionFailure().Should().BeTrue();

    [Theory]
    [InlineData(StatusCodes.BadNodeIdUnknown)]
    [InlineData(StatusCodes.BadUserAccessDenied)]
    [InlineData(StatusCodes.BadAttributeIdInvalid)]
    public void IsConnectionFailure_StatusCodeAboutTheNode_ShouldReturnFalse(
        uint statusCode)
        => new ServiceResultException(statusCode).IsConnectionFailure().Should().BeFalse();

    [Fact]
    public void IsConnectionFailure_NotAServiceResultException_ShouldReturnFalse()
        => new InvalidOperationException("Not an OPC UA failure.").IsConnectionFailure().Should().BeFalse();
}