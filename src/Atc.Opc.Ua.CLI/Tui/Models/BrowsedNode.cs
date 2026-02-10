namespace Atc.Opc.Ua.CLI.Tui.Models;

/// <summary>
/// TUI-specific model for address space tree nodes.
/// Wraps <see cref="NodeBrowseResult"/> with state for lazy-loading
/// and parent/child relationships needed by the TreeView.
/// </summary>
public sealed class BrowsedNode
{
    public string NodeId { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string BrowseName { get; init; } = string.Empty;

    public NodeClassType NodeClass { get; init; }

    public string? DataTypeName { get; init; }

    public bool HasChildren { get; set; } = true;

    public bool ChildrenLoaded { get; set; }

    public List<BrowsedNode> Children { get; } = [];

    public BrowsedNode? Parent { get; set; }

    public override string ToString()
    {
        var suffix = NodeClass == NodeClassType.Variable && !string.IsNullOrEmpty(DataTypeName)
            ? $" [{DataTypeName}]"
            : string.Empty;

        return $"{DisplayName}{suffix}";
    }

    /// <summary>
    /// Creates a <see cref="BrowsedNode"/> from a <see cref="NodeBrowseResult"/>.
    /// </summary>
    /// <param name="result">The browse result to convert.</param>
    /// <param name="parent">Optional parent node for tree navigation.</param>
    /// <returns>A new <see cref="BrowsedNode"/>.</returns>
    public static BrowsedNode FromBrowseResult(NodeBrowseResult result, BrowsedNode? parent = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new BrowsedNode
        {
            NodeId = result.NodeId,
            DisplayName = result.DisplayName,
            BrowseName = result.BrowseName,
            NodeClass = result.NodeClass,
            DataTypeName = result.DataTypeName,
            HasChildren = result.HasChildren,
            Parent = parent,
        };
    }
}
