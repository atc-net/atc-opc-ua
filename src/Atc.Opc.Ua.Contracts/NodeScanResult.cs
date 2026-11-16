namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// The result of scanning the address space with <c>IOpcUaScanner.ScanAsync</c>.
/// </summary>
/// <param name="Root">The starting node, with the objects and variables below it.</param>
/// <param name="SkippedNodeIds">
/// The nodes the scan skipped because they don't exist or may not be read, e.g. without access, or whose children
/// couldn't be browsed for that reason. Such a node is missing from <paramref name="Root"/>, or is in it without its children, so don't treat it
/// as removed when comparing two scans.
/// </param>
public sealed record NodeScanResult(
    NodeBase Root,
    IReadOnlyList<string> SkippedNodeIds);