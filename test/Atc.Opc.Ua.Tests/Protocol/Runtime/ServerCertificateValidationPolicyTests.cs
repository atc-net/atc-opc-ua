namespace Atc.Opc.Ua.Tests.Protocol.Runtime;

public sealed class ServerCertificateValidationPolicyTests
{
    public static TheoryData<StatusCode> ErrorsRejectedOnValidate =>
    [
        StatusCodes.BadCertificateTimeInvalid,
        StatusCodes.BadCertificatePolicyCheckFailed,
        StatusCodes.BadCertificateChainIncomplete,
        StatusCodes.BadCertificateRevocationUnknown,
    ];

    public static TheoryData<StatusCode> ErrorsAcceptedWithoutValidate =>
    [
        StatusCodes.BadCertificateUntrusted,
        StatusCodes.BadCertificateTimeInvalid,
    ];

    public static TheoryData<StatusCode, string> ErrorsWithAHint => new()
    {
        { StatusCodes.BadCertificateTimeInvalid, "expired" },
        { StatusCodes.BadCertificatePolicyCheckFailed, "MinimumCertificateKeySize" },
        { StatusCodes.BadCertificateRevoked, "revoked" },
        { StatusCodes.BadCertificateUriInvalid, "application URI" },
        { StatusCodes.BadCertificateChainIncomplete, "no ServerCertificateValidation mode accepts that" },
    };

    public static TheoryData<StatusCode> ErrorsRefusedWithoutHandler =>
    [
        StatusCodes.BadCertificateChainIncomplete,
        StatusCodes.BadCertificateRevoked,
        StatusCodes.BadCertificateIssuerRevoked,
        StatusCodes.BadCertificateUriInvalid,
    ];

    public static TheoryData<StatusCode> ErrorsNotRefusedWithoutHandler =>
    [
        StatusCodes.BadCertificateUntrusted,
        StatusCodes.BadTimeout,
    ];

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
    [MemberData(nameof(ErrorsRejectedOnValidate))]
    public void Decide_OtherErrorOnValidate_ShouldRejectEvenWithAutoAccept(
        StatusCode statusCode)
        => ServerCertificateValidationPolicy
            .Decide(ServerCertificateValidation.Validate, autoAcceptUntrusted: true, statusCode)
            .Should().Be(ServerCertificateDecision.Reject);

    [Theory]
    [MemberData(nameof(ErrorsAcceptedWithoutValidate))]
    public void Decide_ErrorOnAudit_ShouldAcceptAudited(StatusCode statusCode)
        => ServerCertificateValidationPolicy
            .Decide(ServerCertificateValidation.Audit, autoAcceptUntrusted: false, statusCode)
            .Should().Be(ServerCertificateDecision.AcceptAudited);

    [Theory]
    [MemberData(nameof(ErrorsAcceptedWithoutValidate))]
    public void Decide_ErrorOnAcceptAny_ShouldAcceptWithoutValidation(
        StatusCode statusCode)
        => ServerCertificateValidationPolicy
            .Decide(ServerCertificateValidation.AcceptAny, autoAcceptUntrusted: false, statusCode)
            .Should().Be(ServerCertificateDecision.AcceptWithoutValidation);

    [Fact]
    public void Decide_UntrustedWithInfoBits_ShouldIgnoreTheInfoBits()
    {
        // Arrange
        var statusCode = StatusCodes.BadCertificateUntrusted.SetHasDataValueInfo(true).SetOverflow(true);

        // Act
        var decision = ServerCertificateValidationPolicy.Decide(
            ServerCertificateValidation.Validate,
            autoAcceptUntrusted: true,
            statusCode);

        // Assert
        decision.Should().Be(ServerCertificateDecision.AcceptUntrusted);
    }

    [Fact]
    public void GetHint_UntrustedRefused_ShouldNameTheRejectedAndTrustedStores()
        => ServerCertificateValidationPolicy
            .GetHint(StatusCodes.BadCertificateUntrusted, refused: true, new OpcUaSecurityOptions())
            .Should().Contain("'opc/pki/rejected/certs'").And.Contain("'opc/pki/trusted/certs'");

    [Fact]
    public void GetHint_UntrustedNotRefused_ShouldNotSayToCopyFromTheRejectedStore()
    {
        // Act
        var hint = ServerCertificateValidationPolicy.GetHint(StatusCodes.BadCertificateUntrusted, refused: false, new OpcUaSecurityOptions());

        // Assert
        hint.Should().StartWith("To trust it, put the server's certificate into 'opc/pki/trusted/certs'");
        hint.Should().NotContain("copy it from");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetHint_Untrusted_ShouldSayToPutTheCaAndItsCrlIntoTheTrustedStore(
        bool refused)
        => ServerCertificateValidationPolicy
            .GetHint(StatusCodes.BadCertificateUntrusted, refused, new OpcUaSecurityOptions())
            .Should().Contain("if it's signed by a CA, put the CA certificate into 'opc/pki/trusted/certs' and its CRL into 'opc/pki/trusted/crl'");

    [Fact]
    public void GetHint_ChainIncomplete_ShouldNameTheTrustedStore()
    {
        // Arrange
        var options = new OpcUaSecurityOptions
        {
            TrustedPeerCertificates = { StorePath = "trusted-peers" },
        };

        // Act
        var hint = ServerCertificateValidationPolicy.GetHint(StatusCodes.BadCertificateChainIncomplete, refused: true, options);

        // Assert
        hint.Should().Contain("'opc/pki/trusted-peers/certs'").And.Contain("'opc/pki/trusted-peers/crl'");
    }

    [Fact]
    public void GetHint_RevocationUnknown_ShouldNameTheCrlFolderNextToEachCaFolder()
        => ServerCertificateValidationPolicy
            .GetHint(StatusCodes.BadCertificateRevocationUnknown, refused: true, new OpcUaSecurityOptions())
            .Should().Contain("into 'opc/pki/trusted/crl' if the CA is in 'opc/pki/trusted/certs', or 'opc/pki/issuers/crl' if it's in 'opc/pki/issuers/certs'");

    [Theory]
    [MemberData(nameof(ErrorsWithAHint))]
    public void GetHint_KnownError_ShouldSayWhatToDo(
        StatusCode statusCode,
        string expected)
        => ServerCertificateValidationPolicy
            .GetHint(statusCode, refused: true, new OpcUaSecurityOptions())
            .Should().Contain(expected);

    [Fact]
    public void GetHint_UnknownError_ShouldPointToTheSpecification()
        => ServerCertificateValidationPolicy
            .GetHint(StatusCodes.BadUnexpectedError, refused: true, new OpcUaSecurityOptions())
            .Should().Contain("OPC UA specification");

    [Theory]
    [MemberData(nameof(ErrorsRefusedWithoutHandler))]
    public void FindErrorRefusedWithoutHandler_ErrorRefusedWithoutHandler_ShouldFindIt(
        StatusCode statusCode)
        => ServerCertificateValidationPolicy
            .FindErrorRefusedWithoutHandler(new ServiceResultException(statusCode))
            .Should().Be(statusCode);

    [Theory]
    [MemberData(nameof(ErrorsNotRefusedWithoutHandler))]
    public void FindErrorRefusedWithoutHandler_OtherError_ShouldFindNothing(
        StatusCode statusCode)
        => ServerCertificateValidationPolicy
            .FindErrorRefusedWithoutHandler(new ServiceResultException(statusCode))
            .Should().BeNull();

    [Fact]
    public void FindErrorRefusedWithoutHandler_ChainIncompleteWrappedAsTheStackDoes_ShouldFindIt()
    {
        // Arrange
        var problems = new ServiceResult(
            new ServiceResult(StatusCodes.BadCertificateUntrusted),
            new ServiceResult(StatusCodes.BadCertificateChainIncomplete));

        var exception = new ServiceResultException(
            new ServiceResultException(problems),
            StatusCodes.BadCertificateInvalid);

        // Act
        var error = ServerCertificateValidationPolicy.FindErrorRefusedWithoutHandler(exception);

        // Assert
        error.Should().Be(StatusCodes.BadCertificateChainIncomplete);
    }

    [Fact]
    public void FindErrorRefusedWithoutHandler_ChainIncompleteInAnInnerException_ShouldFindIt()
    {
        // Arrange
        var exception = new ServiceResultException(
            StatusCodes.BadSecureChannelClosed,
            new ServiceResultException(StatusCodes.BadCertificateChainIncomplete));

        // Act
        var error = ServerCertificateValidationPolicy.FindErrorRefusedWithoutHandler(exception);

        // Assert
        error.Should().Be(StatusCodes.BadCertificateChainIncomplete);
    }

    [Fact]
    public void DefaultOptions_ShouldValidate()
        => new OpcUaSecurityOptions().ServerCertificateValidation.Should().Be(ServerCertificateValidation.Validate);
}