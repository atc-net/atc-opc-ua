namespace Atc.Opc.Ua.Options;

/// <summary>
/// How the client checks the certificate a server presents on a secure connection.
/// </summary>
/// <remarks>
/// <para>
/// A server certificate is only checked on an endpoint with security. The client picks one when it connects with a
/// user name and password, and also without them when the server offers no endpoint without security.
/// </para>
/// <para>
/// The values start at 1, so a value that was never set (e.g. 0 from configuration) is rejected instead of meaning
/// one of the choices.
/// </para>
/// </remarks>
[SuppressMessage("Design", "CA1008:Enums should have zero value", Justification = "OK - a value that was never set must be rejected, not mean one of the choices.")]
public enum ServerCertificateValidation
{
    /// <summary>
    /// Accept any certificate the OPC UA stack lets a client accept, without checking it. The connection is still
    /// encrypted, but the client can't tell the real server from anything in between.
    /// </summary>
    AcceptAny = 1,

    /// <summary>
    /// Accept any certificate, as <see cref="AcceptAny"/>, but log a warning when <see cref="Validate"/> would have
    /// refused it, why, and what to do. Use it to try out validation in production without risk.
    /// </summary>
    Audit = 2,

    /// <summary>
    /// The OPC UA stack's validation with the security options: the certificate must be trusted (or untrusted, with
    /// <see cref="OpcUaSecurityOptions.AutoAcceptUntrustedCertificates"/>), valid now, with a big enough key, and, when
    /// a CA signed it, the CA must be known. A refused certificate fails the connection, and is logged with what to do.
    /// </summary>
    Validate = 3,
}