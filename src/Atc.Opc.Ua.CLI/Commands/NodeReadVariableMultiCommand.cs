namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadVariableMultiCommand : AsyncCommand<MultiNodeCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<NodeReadVariableMultiCommand> logger;

    public NodeReadVariableMultiCommand(
        IOpcUaClient opcUaClient,
        ILogger<NodeReadVariableMultiCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        MultiNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        MultiNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        var nodeIds = settings.NodeIds;
        var includeSampleValues = settings.IncludeSampleValues;
        var nodeVariableReadDepth = settings.NodeVariableReadDepth;

        return await OpcUaCommandRunner.RunAsync(
            opcUaClient,
            settings,
            logger,
            async ct =>
            {
                var nodeVariables = await opcUaClient.ReadNodeVariablesAsync(nodeIds, includeSampleValues, nodeVariableReadDepth, ct);
                logger.LogInformation($"Received the following data: {GetSimpleStrings(nodeVariables)}");
            },
            cancellationToken);
    }

    private static string GetSimpleStrings(IList<NodeVariable> nodeVariables)
    {
        var sb = new StringBuilder();
        sb.AppendLine();

        foreach (var nodeVariable in nodeVariables)
        {
            sb.AppendLine("   " + nodeVariable.ToStringSimple());
        }

        return sb.ToString();
    }
}