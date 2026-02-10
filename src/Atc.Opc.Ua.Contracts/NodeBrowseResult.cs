namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Represents a single node returned by a lazy browse operation.
/// </summary>
/// <remarks>
/// Used by <c>IOpcUaNodeBrowser.BrowseChildrenAsync</c> to return one level
/// of the address space hierarchy at a time, suitable for lazy-loading
/// tree views.
/// </remarks>
public class NodeBrowseResult
{
    /// <summary>
    /// Gets or sets the fully-qualified node-id (e.g. <c>"ns=2;i=1234"</c>).
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the localised display name of the node.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the browse name of the node.
    /// </summary>
    public string BrowseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the node class (Object, Variable, Method, etc.).
    /// </summary>
    public NodeClassType NodeClass { get; set; }

    /// <summary>
    /// Gets or sets the name of the data type for variable nodes,
    /// or <see langword="null"/> for non-variable nodes.
    /// </summary>
    public string? DataTypeName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node has child nodes.
    /// This may be an optimistic estimate to support lazy-loading
    /// (the node may report <see langword="true"/> even if it has no children).
    /// </summary>
    public bool HasChildren { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(NodeId)}: {NodeId}, " +
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(NodeClass)}: {NodeClass}, " +
        $"{nameof(HasChildren)}: {HasChildren}";
}
