namespace Atc.Opc.Ua.Tests.Protocol.Runtime;

public sealed class ServerCertificateValidationPolicyTests
{
    [Theory]
    [InlineData(ServerCertificateValidation.AcceptAny)]
    [InlineData(ServerCertificateValidation.Audit)]
    [InlineData(ServerCertificateValidation.Validate)]
    public void Decide_UntrustedWithAutoAccept_ShouldAcceptUntrusted(
        ServerCertificateValidation validation)
        => ServerCertificateValidationPolicy
            .Decide(validation, autoAcceptUntrusted: true, StatusCodes.BadCertificateUntrusted)
            .Should().Be(ServerCertificateDecision.AcceptUntrusted);

    [Fact]
    public void Decide_UntrustedWithoutAutoAcceptOnValidate_ShouldReject()
        => ServerCertificateValidationPolicy
            .Decide(ServerCertificateValidation.Validate, autoAcceptUntrusted: false, StatusCodes.BadCertificateUntrusted)
            .Should().Be(ServerCertificateDecision.Reject);

    [Theory]
    [InlineData(StatusCodes.BadCertificateTimeInvalid)]
    [InlineData(StatusCodes.BadCertificatePolicyCheckFailed)]
    [InlineData(StatusCodes.BadCertificateChainIncomplete)]
    [InlineData(StatusCodes.BadCertificateRevocationUnknown)]
    public void Decide_OtherErrorOnValidate_ShouldRejectEvenWithAutoAccept(
        uint statusCode)
        => ServerCertificateValidationPolicy
            .Decide(ServerCertificateValidation.Validate, autoAcceptUntrusted: true, statusCode)
            .Should().Be(ServerCertificateDecision.Reject);

    [Theory]
    [InlineData(StatusCodes.BadCertificateUntrusted)]
    [InlineData(StatusCodes.BadCertificateTimeInvalid)]
    public void Decide_ErrorOnAudit_ShouldAcceptAudited(uint statusCode)
        => ServerCertificateValidationPolicy
            .Decide(ServerCertificateValidation.Audit, autoAcceptUntrusted: false, statusCode)
            .Should().Be(ServerCertificateDecision.AcceptAudited);

    [Theory]
    [InlineData(StatusCodes.BadCertificateUntrusted)]
    [InlineData(StatusCodes.BadCertificateTimeInvalid)]
    public void Decide_ErrorOnAcceptAny_ShouldAcceptWithoutValidation(
        uint statusCode)
        => ServerCertificateValidationPolicy
            .Decide(ServerCertificateValidation.AcceptAny, autoAcceptUntrusted: false, statusCode)
            .Should().Be(ServerCertificateDecision.AcceptWithoutValidation);

    [Fact]
    public void Decide_UntrustedWithInfoBits_ShouldIgnoreTheInfoBits()
    {
        // Arrange
        const uint infoBits = 0x00000480;

        // Act
        var decision = ServerCertificateValidationPolicy.Decide(
            ServerCertificateValidation.Validate,
            autoAcceptUntrusted: true,
            StatusCodes.BadCertificateUntrusted | infoBits);

        // Assert
        decision.Should().Be(ServerCertificateDecision.AcceptUntrusted);
    }

    [Fact]
    public void GetHint_Untrusted_ShouldNameTheRejectedAndTrustedStores()
        => ServerCertificateValidationPolicy
            .GetHint(StatusCodes.BadCertificateUntrusted, new OpcUaSecurityOptions())
            .Should().Contain("'opc/pki/rejected/certs'").And.Contain("'opc/pki/trusted/certs'");

    [Fact]
    public void GetHint_ChainIncomplete_ShouldNameTheIssuerStore()
    {
        // Arrange
        var options = new OpcUaSecurityOptions
        {
            TrustedIssuerCertificates = { StorePath = "issuer" },
        };

        // Act
        var hint = ServerCertificateValidationPolicy.GetHint(StatusCodes.BadCertificateChainIncomplete, options);

        // Assert
        hint.Should().Contain("'opc/pki/issuer/certs'");
    }

    [Fact]
    public void GetHint_RevocationUnknown_ShouldNameTheCrlFolder()
        => ServerCertificateValidationPolicy
            .GetHint(StatusCodes.BadCertificateRevocationUnknown, new OpcUaSecurityOptions())
            .Should().Contain("'opc/pki/issuers/crl'");

    [Theory]
    [InlineData(StatusCodes.BadCertificateTimeInvalid, "expired")]
    [InlineData(StatusCodes.BadCertificatePolicyCheckFailed, "MinimumCertificateKeySize")]
    [InlineData(StatusCodes.BadCertificateRevoked, "revoked")]
    [InlineData(StatusCodes.BadCertificateUriInvalid, "application URI")]
    public void GetHint_KnownError_ShouldSayWhatToDo(
        uint statusCode,
        string expected)
        => ServerCertificateValidationPolicy
            .GetHint(statusCode, new OpcUaSecurityOptions())
            .Should().Contain(expected);

    [Fact]
    public void GetHint_UnknownError_ShouldPointToTheSpecification()
        => ServerCertificateValidationPolicy
            .GetHint(StatusCodes.BadUnexpectedError, new OpcUaSecurityOptions())
            .Should().Contain("OPC UA specification");

    [Fact]
    public void DefaultOptions_ShouldValidate()
        => new OpcUaSecurityOptions().ServerCertificateValidation.Should().Be(ServerCertificateValidation.Validate);
}