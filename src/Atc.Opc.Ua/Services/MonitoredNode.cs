namespace Atc.Opc.Ua.Services;

/// <summary>
/// A node the client's subscription monitors.
/// </summary>
/// <param name="Handle">The handle the caller got when subscribing.</param>
/// <param name="Item">The OPC UA stack's monitored item.</param>
/// <param name="DataTypeName">The name of the node's data type, e.g. <c>Float</c>, or <see langword="null"/> if it couldn't be read.</param>
/// <param name="AccessLevel">The node's access level, or <see langword="null"/> if it couldn't be read.</param>
internal sealed record MonitoredNode(
    uint Handle,
    MonitoredItem Item,
    string? DataTypeName,
    byte? AccessLevel);