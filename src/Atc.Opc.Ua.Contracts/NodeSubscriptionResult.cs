namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// The outcome of subscribing one node with <c>SubscribeToNodesAsync</c>.
/// </summary>
/// <param name="NodeId">The node that was subscribed.</param>
/// <param name="Succeeded"><see langword="true"/> when the node is monitored.</param>
/// <param name="MonitoredItemHandle">The node's handle when it's monitored, otherwise <c>0</c>.</param>
/// <param name="ErrorMessage">Why the node isn't monitored, e.g. <c>BadNodeIdUnknown</c>, otherwise <see langword="null"/>.</param>
public sealed record NodeSubscriptionResult(
    string NodeId,
    bool Succeeded,
    uint MonitoredItemHandle,
    string? ErrorMessage);