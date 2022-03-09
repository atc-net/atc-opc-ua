namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadObjectCommand : AsyncCommand<ObjectNodeSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<NodeReadObjectCommand> logger;

    public NodeReadObjectCommand(IOpcUaClient opcUaClient, ILogger<NodeReadObjectCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(CommandContext context, ObjectNodeSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(ObjectNodeSettings settings)
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

        var sw = Stopwatch.StartNew();

        var connectionSucceeded = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value)
            : await opcUaClient.ConnectAsync(serverUrl);

        if (connectionSucceeded)
        {
            var nodeObject = await opcUaClient.ReadNodeObjectAsync(
                nodeId,
                includeObjects,
                includeVariables,
                includeSampleValues,
                nodeObjectReadDepth);

            if (nodeObject is not null)
            {
                logger.LogInformation($"Received the following data: '{nodeObject.ToStringSimple()}'");
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