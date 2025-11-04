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
        ReadObjectNodeCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(ReadObjectNodeCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
        var nodeId = settings.NodeId;
        var includeObjects = settings.IncludeObjects;
        var includeVariables = settings.IncludeVariables;
        var includeSampleValues = settings.IncludeSampleValues;
        var nodeObjectReadDepth = settings.NodeObjectReadDepth;
        var nodeVariableReadDepth = settings.NodeVariableReadDepth;

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value, CancellationToken.None)
            : await opcUaClient.ConnectAsync(serverUrl, CancellationToken.None);

        if (succeeded)
        {
            var (succeededReading, nodeObject, _) = await opcUaClient.ReadNodeObjectAsync(
                nodeId,
                includeObjects,
                includeVariables,
                includeSampleValues,
                nodeObjectReadDepth,
                nodeVariableReadDepth,
                cancellationToken: CancellationToken.None);

            if (succeededReading)
            {
                logger.LogInformation($"Received the following data: '{nodeObject!.ToStringSimple()}'");
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