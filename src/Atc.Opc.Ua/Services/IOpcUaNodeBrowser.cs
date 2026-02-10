namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides lazy, single-level address space browsing and attribute reading
/// for OPC UA nodes.
/// </summary>
public interface IOpcUaNodeBrowser
{
    /// <summary>
    /// Browses the immediate children of the specified parent node.
    /// </summary>
    /// <param name="client">The connected OPC UA client.</param>
    /// <param name="parentNodeId">The node whose children to browse.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple indicating success, the list of child nodes, and an optional error message.</returns>
    Task<(bool Succeeded, IList<NodeBrowseResult>? Children, string? ErrorMessage)> BrowseChildrenAsync(
        IOpcUaClient client,
        string parentNodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the full set of attributes for the specified node.
    /// </summary>
    /// <param name="client">The connected OPC UA client.</param>
    /// <param name="nodeId">The node whose attributes to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple indicating success, the attribute set, and an optional error message.</returns>
    Task<(bool Succeeded, NodeAttributeSet? Attributes, string? ErrorMessage)> ReadNodeAttributesAsync(
        IOpcUaClient client,
        string nodeId,
        CancellationToken cancellationToken = default);
}
