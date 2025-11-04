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
        SingleNodeCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(SingleNodeCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
        var nodeId = settings.NodeId;
        var includeSampleValue = settings.IncludeSampleValue;
        var nodeVariableReadDepth = settings.NodeVariableReadDepth;

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value, CancellationToken.None)
            : await opcUaClient.ConnectAsync(serverUrl, CancellationToken.None);

        if (succeeded)
        {
            var (succeededReading, nodeVariable, _) = await opcUaClient.ReadNodeVariableAsync(
                nodeId,
                includeSampleValue,
                nodeVariableReadDepth,
                CancellationToken.None);

            if (succeededReading)
            {
                logger.LogInformation($"Received the following data: '{nodeVariable!.ToStringSimple()}'");
            }

            await opcUaClient.DisconnectAsync(CancellationToken.None);
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return succeeded
            ? ConsoleExitStatusCodes.Success
            : ConsoleExitStatusCodes.Failure;
    }
}