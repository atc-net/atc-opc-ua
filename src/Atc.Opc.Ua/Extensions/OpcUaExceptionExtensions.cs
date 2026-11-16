namespace Atc.Opc.Ua.Extensions;

/// <summary>
/// Tells a failure that concerns one node apart from a failure of the connection.
/// </summary>
internal static class OpcUaExceptionExtensions
{
    /// <summary>
    /// The upper 16 bits of a status code identify it; the lower 16 are info bits.
    /// </summary>
    private const uint StatusCodeIdentityMask = 0xFFFF0000;

    /// <summary>
    /// Whether reading or browsing a node failed because of the node itself, so a call that reads many nodes can skip
    /// it: the node doesn't exist, e.g. a reference to a deleted node, or the client may not read it.
    /// </summary>
    /// <remarks>
    /// Any other status code fails the call. After a dropped connection or a timeout, every following read fails too,
    /// so skipping on those would leave out the rest of the tree, and the call would still look successful.
    /// </remarks>
    /// <param name="exception">The stack's exception.</param>
    /// <returns><see langword="true"/> when only the node is affected.</returns>
    public static bool IsNodeFailure(this ServiceResultException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return (exception.StatusCode & StatusCodeIdentityMask)
            is StatusCodes.BadNodeIdUnknown
            or StatusCodes.BadUserAccessDenied
            or StatusCodes.BadNotReadable
            or StatusCodes.BadSecurityModeInsufficient
            or StatusCodes.BadAttributeIdInvalid;
    }

    /// <summary>
    /// Whether a call failed because of the connection or the session, e.g. a closed channel or a timeout, rather than
    /// because of what it read. A best-effort read lets these through, so the call fails instead of falling back.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns><see langword="true"/> for a failure of the connection or the session.</returns>
    public static bool IsConnectionFailure(this Exception exception)
        => exception is ServiceResultException serviceResultException &&
           (serviceResultException.StatusCode & StatusCodeIdentityMask)
           is StatusCodes.BadCommunicationError
           or StatusCodes.BadConnectionClosed
           or StatusCodes.BadConnectionRejected
           or StatusCodes.BadDisconnect
           or StatusCodes.BadNoCommunication
           or StatusCodes.BadNotConnected
           or StatusCodes.BadRequestInterrupted
           or StatusCodes.BadRequestTimeout
           or StatusCodes.BadSecureChannelClosed
           or StatusCodes.BadSecureChannelIdInvalid
           or StatusCodes.BadServerHalted
           or StatusCodes.BadServerNotConnected
           or StatusCodes.BadSessionClosed
           or StatusCodes.BadSessionIdInvalid
           or StatusCodes.BadSessionNotActivated
           or StatusCodes.BadShutdown
           or StatusCodes.BadTcpInternalError
           or StatusCodes.BadTimeout;
}