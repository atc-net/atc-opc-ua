namespace Atc.Opc.Ua.Services;

/// <summary>
/// Scans the address space into a tree of objects and variables.
/// </summary>
public interface IOpcUaScanner
{
    /// <summary>
    /// Scans from the starting node, as an object, or as a variable when it isn't an object. A node that doesn't exist
    /// or may not be read, e.g. without access, is skipped with a warning, and listed in
    /// <see cref="NodeScanResult.SkippedNodeIds"/>. Any other failure, e.g. a dropped connection, fails the scan.
    /// </summary>
    /// <param name="client">The connected client.</param>
    /// <param name="options">What to scan.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tree, and the nodes that were skipped.</returns>
    /// <exception cref="InvalidOperationException">The client isn't connected.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A depth in <paramref name="options"/> is negative.</exception>
    /// <exception cref="ServiceResultException">The starting node doesn't exist (<c>BadNodeIdUnknown</c>), or the scan failed.</exception>
    Task<NodeScanResult> ScanAsync(
        IOpcUaClient client,
        OpcUaScannerOptions options,
        CancellationToken cancellationToken = default);
}