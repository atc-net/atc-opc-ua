namespace Atc.Opc.Ua.Protocol.Runtime;

/// <summary>
/// What the client does with an error the OPC UA stack found in a server certificate.
/// </summary>
internal enum ServerCertificateDecision
{
    /// <summary>
    /// The certificate isn't trusted yet, and <see cref="OpcUaSecurityOptions.AutoAcceptUntrustedCertificates"/> is on.
    /// </summary>
    AcceptUntrusted,

    /// <summary>
    /// <see cref="ServerCertificateValidation.AcceptAny"/>: accepted without checking.
    /// </summary>
    AcceptWithoutValidation,

    /// <summary>
    /// <see cref="ServerCertificateValidation.Audit"/>: accepted, but <see cref="ServerCertificateValidation.Validate"/>
    /// would have refused it.
    /// </summary>
    AcceptAudited,

    /// <summary>
    /// <see cref="ServerCertificateValidation.Validate"/>: refused.
    /// </summary>
    Reject,
}

/// <summary>
/// Decides on the errors the OPC UA stack finds in a server certificate, and says what to do about them.
/// </summary>
/// <remarks>
/// <para>
/// The OPC UA stack checks a server certificate first. Only when it finds a problem does it call the client's
/// <c>CertificateValidation</c> handler: once for each problem, in turn, stopping at the first one the handler doesn't
/// accept. A certificate without problems never reaches the handler.
/// </para>
/// <para>
/// While the client has that handler registered, the OPC UA stack ignores its own
/// <c>AutoAcceptUntrustedCertificates</c> setting and leaves every problem to the handler. So <see cref="Decide"/>
/// applies <see cref="OpcUaSecurityOptions.AutoAcceptUntrustedCertificates"/> itself.
/// </para>
/// <para>
/// The OPC UA stack refuses some problems without calling the handler, e.g. a malformed certificate. No setting
/// accepts those.
/// </para>
/// </remarks>
internal static class ServerCertificateValidationPolicy
{
    /// <summary>
    /// The upper 16 bits of a status code identify it; the lower 16 are info bits.
    /// </summary>
    private const uint StatusCodeIdentityMask = 0xFFFF0000;

    /// <summary>
    /// Decides on one error in a server certificate.
    /// </summary>
    /// <param name="validation">How server certificates are checked.</param>
    /// <param name="autoAcceptUntrusted">Whether a certificate that isn't trusted yet is accepted.</param>
    /// <param name="error">The error the stack found.</param>
    /// <returns>The decision.</returns>
    public static ServerCertificateDecision Decide(
        ServerCertificateValidation validation,
        bool autoAcceptUntrusted,
        StatusCode error)
    {
        if (autoAcceptUntrusted && Is(error, StatusCodes.BadCertificateUntrusted))
        {
            return ServerCertificateDecision.AcceptUntrusted;
        }

        return validation switch
        {
            ServerCertificateValidation.AcceptAny => ServerCertificateDecision.AcceptWithoutValidation,
            ServerCertificateValidation.Audit => ServerCertificateDecision.AcceptAudited,
            _ => ServerCertificateDecision.Reject,
        };
    }

    /// <summary>
    /// Says what to do about an error in a server certificate.
    /// </summary>
    /// <param name="error">The error the stack found.</param>
    /// <param name="refused">
    /// Whether the certificate was refused. Only then does the OPC UA stack save it in the rejected store, to copy from.
    /// </param>
    /// <param name="options">The security options, for the store paths.</param>
    /// <returns>What to do.</returns>
    public static string GetHint(
        StatusCode error,
        bool refused,
        OpcUaSecurityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var trusted = $"{options.PkiRootPath}/{options.TrustedPeerCertificates.StorePath}";
        var rejected = $"{options.PkiRootPath}/{options.RejectedCertificates.StorePath}";
        var issuers = $"{options.PkiRootPath}/{options.TrustedIssuerCertificates.StorePath}";

        return (error.Code & StatusCodeIdentityMask) switch
        {
            StatusCodes.BadCertificateUntrusted when refused =>
                $"To trust it, copy it from '{rejected}/certs' to '{trusted}/certs', or set AutoAcceptUntrustedCertificates.",
            StatusCodes.BadCertificateUntrusted =>
                $"To trust it, put the server's certificate into '{trusted}/certs', or set AutoAcceptUntrustedCertificates. With Validate, a refused certificate is saved in '{rejected}/certs' to copy from.",
            StatusCodes.BadCertificateChainIncomplete =>
                $"It's signed by a CA the client doesn't know: put the CA certificate into '{issuers}/certs'.",
            StatusCodes.BadCertificateRevocationUnknown or StatusCodes.BadCertificateIssuerRevocationUnknown =>
                $"The CA's revocation list is missing: put the CA's CRL into '{issuers}/crl'.",
            StatusCodes.BadCertificateTimeInvalid or StatusCodes.BadCertificateIssuerTimeInvalid =>
                "It has expired or isn't valid yet: check the clock, or renew the server's certificate.",
            StatusCodes.BadCertificatePolicyCheckFailed =>
                "Its key is too small or its signature too weak: renew the server's certificate, or lower MinimumCertificateKeySize or turn off RejectSha1SignedCertificates.",
            StatusCodes.BadCertificateRevoked or StatusCodes.BadCertificateIssuerRevoked =>
                "It's revoked: the server needs a new certificate.",
            StatusCodes.BadCertificateHostNameInvalid =>
                "It doesn't contain the host name the client connected to: connect by a name on the certificate, or renew it.",
            StatusCodes.BadCertificateUriInvalid =>
                "Its application URI doesn't match the server's: renew the server's certificate.",
            StatusCodes.BadCertificateUseNotAllowed or StatusCodes.BadCertificateIssuerUseNotAllowed =>
                "Its key usage doesn't allow this use: renew the server's certificate.",
            _ => "See the status code in the OPC UA specification (Part 4, Certificate validation).",
        };
    }

    private static bool Is(
        StatusCode error,
        uint statusCode)
        => (error.Code & StatusCodeIdentityMask) == statusCode;
}