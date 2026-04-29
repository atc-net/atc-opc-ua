namespace Atc.Opc.Ua.Tests.Differs;

internal sealed record OpcUaScanResult(
    string EndpointUrl,
    string StartingNodeId,
    NodeBase Result,
    DateTimeOffset Start,
    DateTimeOffset End);