namespace Atc.Opc.Ua.Options;

public sealed class OpcUaClientKeepAliveOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether keep-alive is enabled.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if keep-alive is enabled; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// Gets or sets the keep-alive interval in milliseconds.
    /// </summary>
    /// <returns>
    /// The interval in milliseconds at which the keep-alive checks are performed.
    /// </returns>
    public int IntervalMilliseconds { get; set; } = OpcUaConstants.DefaultKeepAliveIntervalMilliseconds;

    /// <summary>
    /// Gets or sets the maximum number of keep-alive failures before the handler attempts to re-establish the session.
    /// </summary>
    /// <returns>
    /// The maximum number of keep-alive failures before the handler attempts to re-establish the session.
    /// </returns>
    public int MaxFailuresBeforeReconnect { get; set; } = OpcUaConstants.DefaultMaxRetryCountBeforeReconnect;

    /// <summary>
    /// Gets or sets the reconnect period in milliseconds.
    /// </summary>
    /// <returns>
    /// The reconnect period in milliseconds.
    /// </returns>
    public int ReconnectPeriodMilliseconds { get; set; } = OpcUaConstants.DefaultReconnectPeriodMilliseconds;
}