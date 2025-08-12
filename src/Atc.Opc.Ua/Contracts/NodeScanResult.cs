namespace Atc.Opc.Ua.Contracts;

public record NodeScanResult(
    bool Succeeded,
    NodeBase? Root,
    string? ErrorMessage);
