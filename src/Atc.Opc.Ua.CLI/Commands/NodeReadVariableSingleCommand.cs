namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadVariableSingleCommand : AsyncCommand<SingleNodeCommandSettings>
{
    private readonly OpcUaClientFactory clientFactory;
    private readonly ILogger<NodeReadVariableSingleCommand> logger;

    public NodeReadVariableSingleCommand(
        OpcUaClientFactory clientFactory,
        ILogger<NodeReadVariableSingleCommand> logger)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
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
            clientFactory,
            settings,
            logger,
            async (client, ct) =>
            {
                var nodeVariable = await client.ReadNodeVariableAsync(nodeId, includeSampleValue, nodeVariableReadDepth, cancellationToken: ct)
                                   ?? throw OpcUaCommandRunner.NodeNotFound(nodeId);

                logger.LogInformation($"Received the following data: '{nodeVariable.ToStringSimple()}'");
            },
            cancellationToken);
    }
}