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
    /// Gets or sets the session timeout in milliseconds.
    /// </summary>
    /// <returns>
    /// The session timeout in milliseconds.
    /// </returns>
    public uint SessionTimeoutMilliseconds { get; set; } = OpcUaConstants.DefaultSessionTimeoutMilliseconds;
}