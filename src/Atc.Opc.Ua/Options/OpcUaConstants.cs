namespace Atc.Opc.Ua.Options;

public static class OpcUaConstants
{
    /// <summary>
    /// The default application name for the OPC UA client.
    /// </summary>
    public const string DefaultApplicationName = nameof(OpcUaClient);

    /// <summary>
    /// The default session timeout for the OPC UA client.
    /// </summary>
    public const uint DefaultSessionTimeoutMilliseconds = 30 * 60 * 1000; // 30 minutes

    /// <summary>
    /// The default keep-alive interval for the OPC UA client.
    /// </summary>
    public const int DefaultKeepAliveIntervalMilliseconds = 15_000;

    /// <summary>
    /// The maximum number of keep-alive failures before the handler attempts to re-establish the session.
    /// </summary>
    public const int DefaultMaxRetryCountBeforeReconnect = 3;

    /// <summary>
    /// The period between reconnect attempts while the handler is trying to re-establish the session.
    /// </summary>
    public const int DefaultReconnectPeriodMilliseconds = 10_000;
}