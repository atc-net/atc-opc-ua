namespace Atc.Opc.Ua.CLI.Commands;

public sealed class NodeReadVariableSingleCommand : AsyncCommand<SingleNodeSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<NodeReadVariableSingleCommand> logger;

    public NodeReadVariableSingleCommand(IOpcUaClient opcUaClient, ILogger<NodeReadVariableSingleCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(CommandContext context, SingleNodeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(SingleNodeSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
        var nodeId = settings.NodeId;
        var includeSampleValue = settings.IncludeSampleValue;

        var sw = Stopwatch.StartNew();

        var connectionSucceeded = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value)
            : await opcUaClient.ConnectAsync(serverUrl);

        if (connectionSucceeded)
        {
            var nodeVariable = await opcUaClient.ReadNodeVariableAsync(nodeId, includeSampleValue);
            if (nodeVariable is not null)
            {
                logger.LogInformation($"Received the following data: '{nodeVariable.ToStringSimple()}'");
            }

            opcUaClient.Disconnect();
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return connectionSucceeded
            ? ConsoleExitStatusCodes.Success
            : ConsoleExitStatusCodes.Failure;
    }
}