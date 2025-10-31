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
        MultiNodeCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(MultiNodeCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
        var nodeIds = settings.NodeIds;
        var includeSampleValues = settings.IncludeSampleValues;
        var nodeVariableReadDepth = settings.NodeVariableReadDepth;

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value, CancellationToken.None)
            : await opcUaClient.ConnectAsync(serverUrl, CancellationToken.None);

        if (succeeded)
        {
            var (succeededReading, nodeVariables, _) = await opcUaClient.ReadNodeVariablesAsync(
                nodeIds,
                includeSampleValues,
                CancellationToken.None,
                nodeVariableReadDepth);

            if (succeededReading)
            {
                logger.LogInformation($"Received the following data: {GetSimpleStrings(nodeVariables!)}");
            }

            await opcUaClient.DisconnectAsync(CancellationToken.None);
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return succeeded
            ? ConsoleExitStatusCodes.Success
            : ConsoleExitStatusCodes.Failure;
    }

    private static string GetSimpleStrings(
        IList<NodeVariable> nodeVariables)
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