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
    /// <returns>The child nodes.</returns>
    /// <exception cref="InvalidOperationException">The client isn't connected.</exception>
    /// <exception cref="ServiceResultException">Browsing failed, e.g. with <c>BadNodeIdUnknown</c> for a node that doesn't exist.</exception>
    Task<IList<NodeBrowseResult>> BrowseChildrenAsync(
        IOpcUaClient client,
        string parentNodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the full set of attributes for the specified node.
    /// </summary>
    /// <param name="client">The connected OPC UA client.</param>
    /// <param name="nodeId">The node whose attributes to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The attributes, or <see langword="null"/> when the node doesn't exist.</returns>
    /// <exception cref="InvalidOperationException">The client isn't connected.</exception>
    Task<NodeAttributeSet?> ReadNodeAttributesAsync(
        IOpcUaClient client,
        string nodeId,
        CancellationToken cancellationToken = default);
}