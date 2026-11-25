namespace Atc.Opc.Ua.Tests.Extensions;

public sealed class OpcUaExceptionExtensionsTests
{
    public static TheoryData<StatusCode> StatusCodesAboutTheNode =>
    [
        StatusCodes.BadNodeIdUnknown,
        StatusCodes.BadUserAccessDenied,
        StatusCodes.BadNotReadable,
        StatusCodes.BadSecurityModeInsufficient,
        StatusCodes.BadAttributeIdInvalid,
    ];

    public static TheoryData<StatusCode> StatusCodesNotAboutTheNode =>
    [
        StatusCodes.BadConnectionClosed,
        StatusCodes.BadRequestTimeout,
        StatusCodes.BadSecureChannelClosed,
        StatusCodes.BadRequestInterrupted,
        StatusCodes.BadUnexpectedError,
    ];

    public static TheoryData<StatusCode> StatusCodesOfTheConnection =>
    [
        StatusCodes.BadConnectionClosed,
        StatusCodes.BadSecureChannelClosed,
        StatusCodes.BadNotConnected,
        StatusCodes.BadRequestTimeout,
        StatusCodes.BadTimeout,
        StatusCodes.BadSessionIdInvalid,
    ];

    public static TheoryData<StatusCode> StatusCodesNotOfTheConnection =>
    [
        StatusCodes.BadNodeIdUnknown,
        StatusCodes.BadUserAccessDenied,
        StatusCodes.BadAttributeIdInvalid,
    ];

    [Theory]
    [MemberData(nameof(StatusCodesAboutTheNode))]
    public void IsNodeFailure_StatusCodeAboutTheNode_ShouldReturnTrue(
        StatusCode statusCode)
        => new ServiceResultException(statusCode).IsNodeFailure().Should().BeTrue();

    [Theory]
    [MemberData(nameof(StatusCodesNotAboutTheNode))]
    public void IsNodeFailure_StatusCodeNotAboutTheNode_ShouldReturnFalse(
        StatusCode statusCode)
        => new ServiceResultException(statusCode).IsNodeFailure().Should().BeFalse();

    [Fact]
    public void IsNodeFailure_StatusCodeWithInfoBits_ShouldIgnoreTheInfoBits()
    {
        // Arrange
        var statusCode = StatusCodes.BadUserAccessDenied.SetHasDataValueInfo(true).SetOverflow(true);
        var exception = new ServiceResultException(statusCode);

        // Act
        var result = exception.IsNodeFailure();

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(StatusCodesOfTheConnection))]
    public void IsConnectionFailure_StatusCodeOfTheConnection_ShouldReturnTrue(
        StatusCode statusCode)
        => new ServiceResultException(statusCode).IsConnectionFailure().Should().BeTrue();

    [Theory]
    [MemberData(nameof(StatusCodesNotOfTheConnection))]
    public void IsConnectionFailure_StatusCodeAboutTheNode_ShouldReturnFalse(
        StatusCode statusCode)
        => new ServiceResultException(statusCode).IsConnectionFailure().Should().BeFalse();

    [Fact]
    public void IsConnectionFailure_NotAServiceResultException_ShouldReturnFalse()
        => new InvalidOperationException("Not an OPC UA failure.").IsConnectionFailure().Should().BeFalse();
}