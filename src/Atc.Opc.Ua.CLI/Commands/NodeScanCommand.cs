namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeScanCommand : AsyncCommand<ScanNodeCommandSettings>
{
    private readonly OpcUaClientFactory clientFactory;
    private readonly IOpcUaScanner scanner;
    private readonly ILogger<NodeScanCommand> logger;

    public NodeScanCommand(
        OpcUaClientFactory clientFactory,
        IOpcUaScanner scanner,
        ILogger<NodeScanCommand> logger)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ScanNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ScanNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        var options = new OpcUaScannerOptions
        {
            StartingNodeId = settings.StartingNodeId ?? string.Empty,
            ObjectDepth = settings.ObjectDepth,
            VariableDepth = settings.VariableDepth,
            IncludeSampleValues = settings.IncludeSampleValues,
        };

        foreach (var id in settings.IncludeObjectNodeIds)
        {
            options.IncludeObjectNodeIds.Add(id);
        }

        foreach (var id in settings.ExcludeObjectNodeIds)
        {
            options.ExcludeObjectNodeIds.Add(id);
        }

        foreach (var id in settings.IncludeVariableNodeIds)
        {
            options.IncludeVariableNodeIds.Add(id);
        }

        foreach (var id in settings.ExcludeVariableNodeIds)
        {
            options.ExcludeVariableNodeIds.Add(id);
        }

        return await OpcUaCommandRunner.RunAsync(
            clientFactory,
            settings,
            logger,
            async (client, ct) =>
            {
                var scanResult = await scanner.ScanAsync(client, options, ct);
                var (totalObjectCount, totalVariableCount) = CountNodes(scanResult.Root);
                logger.LogInformation($"Scan completed successfully. Found a total of {totalObjectCount} Objects and {totalVariableCount} Variables");

                if (scanResult.SkippedNodeIds.Count > 0)
                {
                    logger.LogWarning($"Skipped {scanResult.SkippedNodeIds.Count} nodes that can't be read: {string.Join(", ", scanResult.SkippedNodeIds)}");
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// Counts total <see cref="NodeObject"/> and <see cref="NodeVariable"/> nodes in the subtree,
    /// starting at <paramref name="root"/> (root included).
    /// </summary>
    /// <param name="root">Root node (object or variable).</param>
    /// <returns>(objects, variables)</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="root"/> is null.</exception>
    private static (int TotalObjectCount, int TotalVariableCount) CountNodes(
        NodeBase root)
    {
        var objectCount = 0;
        var variableCount = 0;

        var stack = new Stack<NodeBase>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var node = stack.Pop();

            switch (node)
            {
                case NodeObject o:
                {
                    objectCount++;
                    foreach (var childObj in o.NodeObjects)
                    {
                        stack.Push(childObj);
                    }

                    break;
                }

                case NodeVariable:
                    variableCount++;
                    break;
            }

            // Variables can exist under both objects and variables.
            foreach (var childVar in node.NodeVariables)
            {
                stack.Push(childVar);
            }
        }

        return (objectCount, variableCount);
    }
}