namespace Atc.Opc.Ua.Services;

/// <summary>
/// The monitored nodes of the client's subscription, by the handle the caller got when subscribing. Thread-safe:
/// subscribe calls, value notifications and a reconnect can use it at the same time.
/// </summary>
internal sealed class MonitoredNodeRegistry
{
    private readonly ConcurrentDictionary<uint, MonitoredNode> nodes = new();
    private uint lastHandle;

    /// <summary>
    /// Gets the number of monitored nodes.
    /// </summary>
    public int Count => nodes.Count;

    /// <summary>
    /// Gets the monitored nodes.
    /// </summary>
    public IReadOnlyCollection<MonitoredNode> Nodes => [.. nodes.Values];

    /// <summary>
    /// Returns a new handle. Handles are unique for the client's lifetime, also across subscriptions.
    /// </summary>
    /// <returns>The handle.</returns>
    public uint NextHandle()
        => Interlocked.Increment(ref lastHandle);

    /// <summary>
    /// Adds a monitored node.
    /// </summary>
    /// <param name="node">The monitored node.</param>
    public void Add(MonitoredNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        nodes[node.Handle] = node;
    }

    /// <summary>
    /// Gets a monitored node by its handle.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <param name="node">The monitored node, when found.</param>
    /// <returns><see langword="true"/> when found.</returns>
    public bool TryGet(
        uint handle,
        [NotNullWhen(true)] out MonitoredNode? node)
        => nodes.TryGetValue(handle, out node);

    /// <summary>
    /// Removes a monitored node.
    /// </summary>
    /// <param name="handle">The handle.</param>
    /// <returns><see langword="true"/> when it was removed.</returns>
    public bool Remove(uint handle)
        => nodes.TryRemove(handle, out _);

    /// <summary>
    /// Removes all monitored nodes.
    /// </summary>
    public void Clear()
        => nodes.Clear();

    /// <summary>
    /// Points the monitored nodes at the items of a subscription the OPC UA stack recreated on a new session after a
    /// reconnect. The items are matched by their handle. Nodes without an item in the subscription are removed.
    /// </summary>
    /// <param name="items">The monitored items of the recreated subscription.</param>
    /// <returns>The number of nodes that were matched.</returns>
    public int Rebind(IEnumerable<MonitoredItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var itemsByHandle = items
            .Where(x => x.Handle is uint)
            .ToDictionary(x => (uint)x.Handle);

        var rebound = 0;
        foreach (var node in nodes.Values)
        {
            if (itemsByHandle.TryGetValue(node.Handle, out var item))
            {
                nodes[node.Handle] = node with { Item = item };
                rebound++;
            }
            else
            {
                nodes.TryRemove(node.Handle, out _);
            }
        }

        return rebound;
    }
}