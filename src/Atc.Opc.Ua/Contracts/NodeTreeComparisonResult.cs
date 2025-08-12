namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Result of comparing two OPC UA node trees produced by scans.
/// </summary>
/// <param name="DeletedNodes">Nodes present in the previous scan but not in the current scan.</param>
/// <param name="AddedNodes">Nodes present in the current scan but not in the previous scan.</param>
/// <param name="UnchangedNodes">Nodes present in both scans (matched by NodeId).</param>
public sealed record NodeTreeComparisonResult(
    IReadOnlyList<NodeBase> DeletedNodes,
    IReadOnlyList<NodeBase> AddedNodes,
    IReadOnlyList<NodeBase> UnchangedNodes);
