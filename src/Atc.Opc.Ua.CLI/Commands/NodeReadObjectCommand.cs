namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadObjectCommand : AsyncCommand<ReadObjectNodeCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<NodeReadObjectCommand> logger;

    public NodeReadObjectCommand(
        IOpcUaClient opcUaClient,
        ILogger<NodeReadObjectCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
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
            opcUaClient,
            settings,
            logger,
            async ct =>
            {
                var nodeObject = await opcUaClient.ReadNodeObjectAsync(
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