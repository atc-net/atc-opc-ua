namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// The outcome of subscribing one node with <c>SubscribeToNodesAsync</c>.
/// </summary>
/// <param name="NodeId">The node that was subscribed.</param>
/// <param name="Succeeded"><see langword="true"/> when the node is monitored.</param>
/// <param name="MonitoredItemHandle">The node's handle when it's monitored, otherwise <c>0</c>.</param>
/// <param name="StatusCode">Why the node isn't monitored, e.g. <c>BadNodeIdUnknown</c> (<c>0x80340000</c>), otherwise <c>0</c> (Good).</param>
/// <param name="ErrorMessage">The name of <paramref name="StatusCode"/>, e.g. <c>BadNodeIdUnknown</c>, when the node isn't monitored, otherwise <see langword="null"/>.</param>
public sealed record NodeSubscriptionResult(
    string NodeId,
    bool Succeeded,
    uint MonitoredItemHandle,
    uint StatusCode,
    string? ErrorMessage);