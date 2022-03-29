namespace Atc.Opc.Ua.CLI.Commands;

public class NodeReadVariableMultiCommand : AsyncCommand<MultiNodeSettings>
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
        MultiNodeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        MultiNodeSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
        var nodeIds = settings.NodeIds;
        var includeSampleValues = settings.IncludeSampleValues;

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value)
            : await opcUaClient.ConnectAsync(serverUrl);

        if (succeeded)
        {
            var (succeededReading, nodeVariables, _) = await opcUaClient.ReadNodeVariablesAsync(nodeIds, includeSampleValues);
            if (succeededReading)
            {
                logger.LogInformation($"Received the following data: {GetSimpleStrings(nodeVariables!)}");
            }

            opcUaClient.Disconnect();
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