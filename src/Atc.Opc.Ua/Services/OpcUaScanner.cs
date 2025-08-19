namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality for scanning OPC UA servers.
/// </summary>
public partial class OpcUaScanner : IOpcUaScanner
{
    private readonly ILogger<OpcUaScanner> logger;

    public OpcUaScanner(
        ILogger<OpcUaScanner> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<NodeScanResult> ScanAsync(
        IOpcUaClient client,
        OpcUaScannerOptions options)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(options);

        if (!client.IsConnected())
        {
            LogScannerClientNotConnected();
            return new NodeScanResult(false, null, "Client is not connected.");
        }

        if (options.ObjectDepth < 0)
        {
            return new NodeScanResult(false, null, "ObjectDepth cannot be negative.");
        }

        if (options.VariableDepth < 0)
        {
            return new NodeScanResult(false, null, "VariableDepth cannot be negative.");
        }

        var startingNodeId = string.IsNullOrWhiteSpace(options.StartingNodeId)
            ? ObjectIds.ObjectsFolder.ToString()
            : options.StartingNodeId.Trim();

        LogScannerStart(startingNodeId, options.ObjectDepth, options.VariableDepth, options.IncludeSampleValues);

        // First attempt: treat as object root.
        var (objSucceeded, nodeObject, scanErrorMessage) = await client.ReadNodeObjectAsync(
            startingNodeId,
            includeObjects: true,
            includeVariables: true,
            includeSampleValues: options.IncludeSampleValues,
            nodeObjectReadDepth: options.ObjectDepth,
            nodeVariableReadDepth: options.VariableDepth,
            includeObjectNodeIds: (IReadOnlyCollection<string>?)options.IncludeObjectNodeIds,
            excludeObjectNodeIds: (IReadOnlyCollection<string>?)options.ExcludeObjectNodeIds,
            includeVariableNodeIds: (IReadOnlyCollection<string>?)options.IncludeVariableNodeIds,
            excludeVariableNodeIds: (IReadOnlyCollection<string>?)options.ExcludeVariableNodeIds);

        if (objSucceeded && nodeObject is not null)
        {
            return new NodeScanResult(Succeeded: true, nodeObject, ErrorMessage: null);
        }

        // Fallback: maybe the starting node is actually a Variable – attempt variable read & recursion.
        var (varSucceeded, nodeVariable, varError) = await client.ReadNodeVariableAsync(
            startingNodeId,
            includeSampleValue: options.IncludeSampleValues,
            nodeVariableReadDepth: options.VariableDepth);

        if (varSucceeded && nodeVariable is not null)
        {
            return new NodeScanResult(Succeeded: true, nodeVariable, ErrorMessage: null);
        }

        var errorMessage = scanErrorMessage ?? varError ?? $"Failed to read starting node '{startingNodeId}'";
        LogScannerReadRootFailure(startingNodeId, errorMessage);
        return new NodeScanResult(Succeeded: false, Root: null, errorMessage);
    }
}