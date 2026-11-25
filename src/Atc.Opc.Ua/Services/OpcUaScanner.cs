namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality for scanning OPC UA servers.
/// </summary>
public partial class OpcUaScanner : IOpcUaScanner
{
    private const string NotConnectedMessage = "The client isn't connected. Call ConnectAsync first.";

    public OpcUaScanner(ILogger<OpcUaScanner> logger)
        => this.logger = logger;

    /// <inheritdoc/>
    public Task<NodeScanResult> ScanAsync(
        IOpcUaClient client,
        OpcUaScannerOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);
        if (options.ObjectDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.ObjectDepth, "ObjectDepth can't be negative.");
        }

        if (options.VariableDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.VariableDepth, "VariableDepth can't be negative.");
        }

        if (!client.IsConnected())
        {
            throw new InvalidOperationException(NotConnectedMessage);
        }

        return InvokeScanAsync(client, options, cancellationToken);
    }

    /// <summary>
    /// Scans from the starting node: as an object, or as a variable when it isn't an object.
    /// </summary>
    /// <param name="client">The connected client.</param>
    /// <param name="options">What to scan.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The tree, and the nodes the scan skipped.</returns>
    private async Task<NodeScanResult> InvokeScanAsync(
        IOpcUaClient client,
        OpcUaScannerOptions options,
        CancellationToken cancellationToken)
    {
        var startingNodeId = string.IsNullOrWhiteSpace(options.StartingNodeId)
            ? OpcUaConstants.ObjectsFolderNodeId
            : options.StartingNodeId.Trim();

        LogScannerStart(startingNodeId, options.ObjectDepth, options.VariableDepth, options.IncludeSampleValues);

        var skippedNodeIds = new List<string>();
        NodeBase? root;
        try
        {
            root = await client.ReadNodeObjectAsync(
                startingNodeId,
                includeObjects: true,
                includeVariables: true,
                includeSampleValues: options.IncludeSampleValues,
                nodeObjectReadDepth: options.ObjectDepth,
                nodeVariableReadDepth: options.VariableDepth,
                includeObjectNodeIds: (IReadOnlyCollection<string>?)options.IncludeObjectNodeIds,
                excludeObjectNodeIds: (IReadOnlyCollection<string>?)options.ExcludeObjectNodeIds,
                includeVariableNodeIds: (IReadOnlyCollection<string>?)options.IncludeVariableNodeIds,
                excludeVariableNodeIds: (IReadOnlyCollection<string>?)options.ExcludeVariableNodeIds,
                skippedNodeIds,
                cancellationToken);
        }
        catch (ArgumentException ex) when (string.Equals(ex.ParamName, "nodeId", StringComparison.Ordinal))
        {
            // The starting node isn't an object; it may be a variable with child variables.
            skippedNodeIds.Clear();
            root = await client.ReadNodeVariableAsync(
                startingNodeId,
                includeSampleValue: options.IncludeSampleValues,
                nodeVariableReadDepth: options.VariableDepth,
                skippedNodeIds,
                cancellationToken);
        }

        return root is null
            ? throw new ServiceResultException(StatusCodes.BadNodeIdUnknown, $"The starting node '{startingNodeId}' doesn't exist.")
            : new NodeScanResult(root, skippedNodeIds);
    }
}