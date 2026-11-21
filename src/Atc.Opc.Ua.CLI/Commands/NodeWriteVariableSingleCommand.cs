namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeWriteVariableSingleCommand : AsyncCommand<WriteNodeCommandSettings>
{
    private readonly OpcUaClientFactory clientFactory;
    private readonly ILogger<NodeWriteVariableSingleCommand> logger;

    public NodeWriteVariableSingleCommand(
        OpcUaClientFactory clientFactory,
        ILogger<NodeWriteVariableSingleCommand> logger)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        WriteNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        WriteNodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        var nodeId = settings.NodeId;
        var dataTypeAsString = settings.DataType;
        var value = settings.Value;

        var dataTypeResolver = new DataTypeResolver();
        var dataType = dataTypeResolver.GetTypeByName(dataTypeAsString);
        var typeConverter = TypeDescriptor.GetConverter(dataType);
        var convertedValue = typeConverter.ConvertFromString(value);

        if (convertedValue is null)
        {
            logger.LogError($"Could not convert input type '{dataTypeAsString}'");
            return ConsoleExitStatusCodes.Failure;
        }

        return await OpcUaCommandRunner.RunAsync(
            clientFactory,
            settings,
            logger,
            async (client, ct) =>
            {
                logger.LogInformation("Connection succeeded");

                _ = await client.ReadNodeVariableAsync(nodeId, includeSampleValue: false, cancellationToken: ct)
                    ?? throw OpcUaCommandRunner.NodeNotFound(nodeId);

                await client.WriteNodeAsync(nodeId, convertedValue, ct);
                logger.LogInformation("Value is updated.");
            },
            cancellationToken);
    }
}