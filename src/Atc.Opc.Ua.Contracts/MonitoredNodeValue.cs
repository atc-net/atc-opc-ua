namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Represents a value change notification for a monitored OPC UA node.
/// </summary>
/// <remarks>
/// Instances of this class are raised through the <c>NodeValueChanged</c> event
/// on <c>IOpcUaClient</c> whenever a subscription delivers a new data-change
/// notification from the server.
/// </remarks>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "MonitoredNodeValue is a better API name than MonitoredNodeValueEventArgs.")]
[SuppressMessage("Major Code Smell", "S3376:Make this class name end with 'EventArgs'.", Justification = "MonitoredNodeValue is a better API name than MonitoredNodeValueEventArgs.")]
public class MonitoredNodeValue : EventArgs
{
    /// <summary>
    /// Gets or sets the handle that uniquely identifies the monitored item
    /// within the subscription.
    /// </summary>
    public uint MonitoredItemHandle { get; set; }

    /// <summary>
    /// Gets or sets the fully-qualified node-id (e.g. <c>"ns=2;s=Demo.Dynamic.Scalar.Float"</c>).
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the localised display name of the monitored node.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current value encoded as a string,
    /// or <see langword="null"/> if the value could not be read.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the server timestamp of the value change,
    /// or <see langword="null"/> if unknown.
    /// </summary>
    public DateTime? ServerTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the source timestamp of the value change,
    /// or <see langword="null"/> if unknown.
    /// </summary>
    public DateTime? SourceTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the OPC UA status code for this value.
    /// A value of <c>0</c> indicates <c>Good</c>.
    /// </summary>
    public uint StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the name of the data type (e.g. <c>"Float"</c>, <c>"Int32"</c>),
    /// or <see langword="null"/> if unknown.
    /// </summary>
    public string? DataTypeName { get; set; }

    /// <summary>
    /// Gets or sets the access level bitmask for the node,
    /// or <see langword="null"/> if not yet read.
    /// </summary>
    public byte? AccessLevel { get; set; }

    /// <summary>
    /// Gets a value indicating whether the status code represents a <c>Good</c> quality.
    /// </summary>
    public bool IsGood => StatusCode == 0;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(NodeId)}: {NodeId}, " +
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(Value)}: {Value ?? "(null)"}, " +
        $"{nameof(StatusCode)}: 0x{StatusCode:X8}, " +
        $"{nameof(ServerTimestamp)}: {ServerTimestamp?.ToString("O") ?? "(null)"}";
}
