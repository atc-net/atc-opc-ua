namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Contains the full set of attributes read from an OPC UA node.
/// </summary>
/// <remarks>
/// Returned by <c>IOpcUaNodeBrowser.ReadNodeAttributesAsync</c> and used
/// by the TUI node details panel to display comprehensive node information.
/// </remarks>
public class NodeAttributeSet
{
    /// <summary>
    /// Gets or sets the fully-qualified node-id.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the localised display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the browse name.
    /// </summary>
    public string BrowseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node class.
    /// </summary>
    public NodeClassType NodeClass { get; set; }

    /// <summary>
    /// Gets or sets the description of the node,
    /// or <see langword="null"/> if not available.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the name of the data type for variable nodes,
    /// or <see langword="null"/> for non-variable nodes.
    /// </summary>
    public string? DataTypeName { get; set; }

    /// <summary>
    /// Gets or sets the current value encoded as a string,
    /// or <see langword="null"/> if the node is not a variable or the value could not be read.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets or sets the access level bitmask for variable nodes,
    /// or <see langword="null"/> for non-variable nodes.
    /// </summary>
    public byte? AccessLevel { get; set; }

    /// <summary>
    /// Gets or sets the user access level bitmask for variable nodes,
    /// or <see langword="null"/> for non-variable nodes.
    /// </summary>
    public byte? UserAccessLevel { get; set; }

    /// <summary>
    /// Gets or sets the OPC UA status code.
    /// A value of <c>0</c> indicates <c>Good</c>.
    /// </summary>
    public uint StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the server timestamp,
    /// or <see langword="null"/> if not available.
    /// </summary>
    public DateTime? ServerTimestamp { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the node is writable
    /// based on the <see cref="AccessLevel"/> bitmask.
    /// </summary>
    public bool IsWritable { get; set; }

    /// <summary>
    /// Gets a value indicating whether the status code represents a <c>Good</c> quality.
    /// </summary>
    public bool IsGood => StatusCode == 0;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(NodeId)}: {NodeId}, " +
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(NodeClass)}: {NodeClass}, " +
        $"{nameof(Value)}: {Value ?? "(null)"}, " +
        $"{nameof(IsWritable)}: {IsWritable}";
}
