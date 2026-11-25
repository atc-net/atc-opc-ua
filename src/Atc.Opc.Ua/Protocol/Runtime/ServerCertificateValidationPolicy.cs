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
/// The OPC UA stack checks a server certificate first. Only when it finds a problem that can be accepted does it call
/// the client's <c>CertificateManager.AcceptError</c> handler: once for each problem, in turn, stopping at the first
/// one the handler doesn't accept. A certificate without problems never reaches the handler.
/// </para>
/// <para>
/// The problems that can be accepted: the certificate isn't trusted, has expired or isn't valid yet (or its CA's), its
/// key or signature is too weak, it doesn't contain the host name, its CA's revocation list is missing, or its key usage
/// doesn't allow this use (or its CA's).
/// </para>
/// <para>
/// The OPC UA stack refuses every other problem without calling the handler, in every
/// <see cref="ServerCertificateValidation"/> mode: e.g. a certificate signed by a CA the client doesn't know, a revoked
/// certificate, an application URI that doesn't match, or a malformed certificate. The connect then fails, with the
/// problem among the results of its error, where <see cref="FindErrorRefusedWithoutHandler"/> finds it.
/// </para>
/// <para>
/// While the client has that handler registered, the OPC UA stack ignores its own
/// <c>AutoAcceptUntrustedCertificates</c> setting and leaves every problem to the handler. So <see cref="Decide"/>
/// applies <see cref="OpcUaSecurityOptions.AutoAcceptUntrustedCertificates"/> itself.
/// </para>
/// </remarks>
internal static class ServerCertificateValidationPolicy
{
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

        if (Is(error, StatusCodes.BadCertificateUntrusted))
        {
            return refused
                ? $"To trust it, copy it from '{rejected}/certs' to '{trusted}/certs', or, if it's signed by a CA, put the CA certificate into '{trusted}/certs' and its CRL into '{trusted}/crl', or set AutoAcceptUntrustedCertificates."
                : $"To trust it, put the server's certificate into '{trusted}/certs', or, if it's signed by a CA, put the CA certificate into '{trusted}/certs' and its CRL into '{trusted}/crl', or set AutoAcceptUntrustedCertificates. With Validate, a refused certificate is saved in '{rejected}/certs' to copy from.";
        }

        if (Is(error, StatusCodes.BadCertificateChainIncomplete))
        {
            return $"It's signed by a CA the client doesn't know, and no ServerCertificateValidation mode accepts that: put the CA certificate into '{trusted}/certs' and its CRL into '{trusted}/crl', or have the server send its CA with its certificate.";
        }

        if (Is(error, StatusCodes.BadCertificateRevocationUnknown) || Is(error, StatusCodes.BadCertificateIssuerRevocationUnknown))
        {
            return $"The CA's revocation list is missing: put the CA's CRL next to the CA certificate, into '{trusted}/crl' if the CA is in '{trusted}/certs', or '{issuers}/crl' if it's in '{issuers}/certs'; or turn off RejectUnknownRevocationStatus.";
        }

        if (Is(error, StatusCodes.BadCertificateTimeInvalid) || Is(error, StatusCodes.BadCertificateIssuerTimeInvalid))
        {
            return "It has expired or isn't valid yet: check the clock, or renew the server's certificate.";
        }

        if (Is(error, StatusCodes.BadCertificatePolicyCheckFailed))
        {
            return "Its key is too small or its signature too weak: renew the server's certificate, or lower MinimumCertificateKeySize or turn off RejectSha1SignedCertificates.";
        }

        if (Is(error, StatusCodes.BadCertificateRevoked) || Is(error, StatusCodes.BadCertificateIssuerRevoked))
        {
            return "It's revoked: the server needs a new certificate.";
        }

        if (Is(error, StatusCodes.BadCertificateHostNameInvalid))
        {
            return "It doesn't contain the host name the client connected to: connect by a name on the certificate, or renew it.";
        }

        if (Is(error, StatusCodes.BadCertificateUriInvalid))
        {
            return "Its application URI doesn't match the server's: renew the server's certificate.";
        }

        if (Is(error, StatusCodes.BadCertificateUseNotAllowed) || Is(error, StatusCodes.BadCertificateIssuerUseNotAllowed))
        {
            return "Its key usage doesn't allow this use: renew the server's certificate.";
        }

        return "See the status code in the OPC UA specification (Part 4, Certificate validation).";
    }

    /// <summary>
    /// Finds, in the error of a failed connect, a server certificate problem that the OPC UA stack refuses without
    /// calling the handler, and that <see cref="GetHint"/> has a hint for: a certificate signed by a CA the client
    /// doesn't know, a revoked certificate (or CA), or an application URI that doesn't match.
    /// </summary>
    /// <param name="exception">The error of the failed connect.</param>
    /// <returns>The problem, or <see langword="null"/> when the error isn't one of them.</returns>
    public static StatusCode? FindErrorRefusedWithoutHandler(
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is not ServiceResultException serviceResultException)
            {
                continue;
            }

            for (var result = serviceResultException.Result; result is not null; result = result.InnerResult)
            {
                if (IsRefusedWithoutHandler(result.StatusCode))
                {
                    return result.StatusCode;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the OPC UA stack refuses a server certificate problem without calling the handler, and
    /// <see cref="GetHint"/> has a hint for it.
    /// </summary>
    /// <param name="error">The status code.</param>
    /// <returns>Whether it's one of them.</returns>
    private static bool IsRefusedWithoutHandler(StatusCode error)
        => Is(error, StatusCodes.BadCertificateChainIncomplete) ||
           Is(error, StatusCodes.BadCertificateRevoked) ||
           Is(error, StatusCodes.BadCertificateIssuerRevoked) ||
           Is(error, StatusCodes.BadCertificateUriInvalid);

    /// <summary>
    /// Whether a status code is the given one, ignoring its info bits.
    /// </summary>
    /// <param name="error">The status code.</param>
    /// <param name="statusCode">The one to compare with.</param>
    /// <returns>Whether they're the same.</returns>
    private static bool Is(
        StatusCode error,
        StatusCode statusCode)
        => error.CodeBits == statusCode.CodeBits;
}