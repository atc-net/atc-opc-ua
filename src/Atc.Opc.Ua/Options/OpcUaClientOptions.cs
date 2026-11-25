namespace Atc.Opc.Ua.Options;

public sealed class OpcUaClientOptions
{
    /// <summary>
    /// Gets or sets the application name for the OPC UA client.
    /// </summary>
    /// <returns>
    /// The application name for the OPC UA client.
    /// </returns>
    public string ApplicationName { get; set; } = OpcUaConstants.DefaultApplicationName;

    /// <summary>
    /// Gets or sets the application URI: the client's identity, which must match the URI in its certificate.
    /// </summary>
    /// <remarks>
    /// When it's empty, the URI is taken from the client's existing certificate, and a new certificate gets
    /// <c>urn:&lt;host name&gt;:&lt;ApplicationName&gt;</c>.
    /// </remarks>
    /// <returns>
    /// The application URI, or empty to take it from the certificate.
    /// </returns>
    public string ApplicationUri { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the session timeout in milliseconds.
    /// </summary>
    /// <returns>
    /// The session timeout in milliseconds.
    /// </returns>
    public uint SessionTimeoutMilliseconds { get; set; } = OpcUaConstants.DefaultSessionTimeoutMilliseconds;
}