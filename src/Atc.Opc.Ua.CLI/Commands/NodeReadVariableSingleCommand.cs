namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadVariableSingleCommand : AsyncCommand<SingleNodeCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<NodeReadVariableSingleCommand> logger;

    public NodeReadVariableSingleCommand(
        IOpcUaClient opcUaClient,
        ILogger<NodeReadVariableSingleCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        SingleNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        SingleNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        var nodeId = settings.NodeId;
        var includeSampleValue = settings.IncludeSampleValue;
        var nodeVariableReadDepth = settings.NodeVariableReadDepth;

        return await OpcUaCommandRunner.RunAsync(
            opcUaClient,
            settings,
            logger,
            async ct =>
            {
                var nodeVariable = await opcUaClient.ReadNodeVariableAsync(nodeId, includeSampleValue, nodeVariableReadDepth, cancellationToken: ct)
                                   ?? throw OpcUaCommandRunner.NodeNotFound(nodeId);

                logger.LogInformation($"Received the following data: '{nodeVariable.ToStringSimple()}'");
            },
            cancellationToken);
    }
}