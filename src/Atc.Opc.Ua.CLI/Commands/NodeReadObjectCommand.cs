namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadObjectCommand : AsyncCommand<ReadObjectNodeCommandSettings>
{
    private readonly OpcUaClientFactory clientFactory;
    private readonly ILogger<NodeReadObjectCommand> logger;

    public NodeReadObjectCommand(
        OpcUaClientFactory clientFactory,
        ILogger<NodeReadObjectCommand> logger)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ReadObjectNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ReadObjectNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        var nodeId = settings.NodeId;
        var includeObjects = settings.IncludeObjects;
        var includeVariables = settings.IncludeVariables;
        var includeSampleValues = settings.IncludeSampleValues;
        var nodeObjectReadDepth = settings.NodeObjectReadDepth;
        var nodeVariableReadDepth = settings.NodeVariableReadDepth;

        return await OpcUaCommandRunner.RunAsync(
            clientFactory,
            settings,
            logger,
            async (client, ct) =>
            {
                var nodeObject = await client.ReadNodeObjectAsync(
                                     nodeId,
                                     includeObjects,
                                     includeVariables,
                                     includeSampleValues,
                                     nodeObjectReadDepth,
                                     nodeVariableReadDepth,
                                     cancellationToken: ct)
                                 ?? throw OpcUaCommandRunner.NodeNotFound(nodeId);

                logger.LogInformation($"Received the following data: '{nodeObject.ToStringSimple()}'");
            },
            cancellationToken);
    }
}