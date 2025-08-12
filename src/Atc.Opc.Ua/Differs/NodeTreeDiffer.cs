namespace Atc.Opc.Ua.Differs;

/// <summary>
/// Generates a difference report between two OPC UA node trees.
/// </summary>
/// <remarks>
/// This type does not implement <see cref="IComparer{T}"/> and does not
/// perform sorting. Instead, it produces a <see cref="NodeTreeComparisonResult"/>
/// that classifies nodes as added, removed, or unchanged by comparing
/// <see cref="NodeBase.NodeId"/> values across two trees.
/// </remarks>
public static class NodeTreeDiffer
{
    /// <summary>
    /// Compare two node trees (current vs. previous) and compute differences by NodeId.
    /// </summary>
    /// <param name="currentRoot">Root node of the current scan.</param>
    /// <param name="previousRoot">Root node of the previous scan.</param>
    /// <returns>A <see cref="NodeTreeComparisonResult"/> instance.</returns>
    public static NodeTreeComparisonResult Diff(
        NodeBase? currentRoot,
        NodeBase? previousRoot)
    {
        var currentFlat = Flatten(currentRoot);
        var previousFlat = Flatten(previousRoot);

        var currentById = currentFlat
            .Where(n => !string.IsNullOrEmpty(n.NodeId))
            .ToDictionary(n => n.NodeId, StringComparer.Ordinal);

        var previousById = previousFlat
            .Where(n => !string.IsNullOrEmpty(n.NodeId))
            .ToDictionary(n => n.NodeId, StringComparer.Ordinal);

        var deleted = new List<NodeBase>();
        var added = new List<NodeBase>();
        var unchanged = new List<NodeBase>();

        // Deleted: in previous, not in current
        foreach (var kv in previousById)
        {
            if (!currentById.ContainsKey(kv.Key))
            {
                deleted.Add(kv.Value);
            }
        }

        // Added and Unchanged: in current; if also in previous => unchanged
        foreach (var kv in currentById)
        {
            if (previousById.TryGetValue(kv.Key, out _))
            {
                unchanged.Add(kv.Value);
            }
            else
            {
                added.Add(kv.Value);
            }
        }

        return new NodeTreeComparisonResult(
            deleted,
            added,
            unchanged);
    }

    private static IEnumerable<NodeBase> Flatten(NodeBase? root)
    {
        if (root is null)
        {
            return [];
        }

        var list = new List<NodeBase>();
        Traverse(root, list);
        return list;
    }

    private static void Traverse(NodeBase node, ICollection<NodeBase> output)
    {
        output.Add(node);

        switch (node)
        {
            case NodeObject obj:
            {
                // Include child variables directly attached to this object
                foreach (var nv in obj.NodeVariables)
                {
                    output.Add(nv);
                }

                // Recurse into child objects
                foreach (var child in obj.NodeObjects)
                {
                    Traverse(child, output);
                }

                break;
            }

            case NodeVariable var:
                // Variables have no children; already added
                _ = var;
                break;
        }
    }
}
