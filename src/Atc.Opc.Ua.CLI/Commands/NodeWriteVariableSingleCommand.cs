namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeWriteVariableSingleCommand : AsyncCommand<WriteNodeCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<NodeWriteVariableSingleCommand> logger;

    public NodeWriteVariableSingleCommand(
        IOpcUaClient opcUaClient,
        ILogger<NodeWriteVariableSingleCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        WriteNodeCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(WriteNodeCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
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
            return ConsoleExitStatusCodes.Success;
        }

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value, CancellationToken.None)
            : await opcUaClient.ConnectAsync(serverUrl, CancellationToken.None);

        if (succeeded)
        {
            logger.LogInformation("Connection succeeded");

            var (succeededReading, _, _) = await opcUaClient.ReadNodeVariableAsync(nodeId, includeSampleValue: false, CancellationToken.None);
            if (succeededReading)
            {
                var (succeededWriting, _) = await opcUaClient.WriteNodeAsync(nodeId, convertedValue, CancellationToken.None);
                if (succeededWriting)
                {
                    logger.LogInformation("Value is updated.");
                }
            }

            await opcUaClient.DisconnectAsync(CancellationToken.None);
        }
        else
        {
            logger.LogError("Connection failure");
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return succeeded
            ? ConsoleExitStatusCodes.Success
            : ConsoleExitStatusCodes.Failure;
    }
}