namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeScanCommand : AsyncCommand<ScanNodeCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly IOpcUaScanner scanner;
    private readonly ILogger<NodeScanCommand> logger;

    public NodeScanCommand(
        IOpcUaClient opcUaClient,
        IOpcUaScanner scanner,
        ILogger<NodeScanCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ScanNodeCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(ScanNodeCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value, CancellationToken.None)
            : await opcUaClient.ConnectAsync(serverUrl, CancellationToken.None);

        if (succeeded)
        {
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

            var scanResult = await scanner.ScanAsync(opcUaClient, options, CancellationToken.None);
            if (scanResult is { Succeeded: true, Root: not null })
            {
                var (totalObjectCount, totalVariableCount) = CountNodes(scanResult.Root!);
                logger.LogInformation($"Scan completed successfully. Found a total of {totalObjectCount} Objects and {totalVariableCount} Variables");
            }
            else
            {
                logger.LogError(scanResult.ErrorMessage ?? "Scan failed");
            }

            await opcUaClient.DisconnectAsync(CancellationToken.None);
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return succeeded ? ConsoleExitStatusCodes.Success : ConsoleExitStatusCodes.Failure;
    }

    /// <summary>
    /// Counts total <see cref="NodeObject"/> and <see cref="NodeVariable"/> nodes in the subtree,
    /// starting at <paramref name="root"/> (root included).
    /// </summary>
    /// <param name="root">Root node (object or variable).</param>
    /// <returns>(objects, variables)</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="root"/> is null.</exception>
    private static (int TotalObjectCount, int TotalVariableCount) CountNodes(NodeBase root)
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
