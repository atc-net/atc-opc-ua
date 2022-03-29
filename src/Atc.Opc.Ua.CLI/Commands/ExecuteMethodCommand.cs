namespace Atc.Opc.Ua.CLI.Commands;

public class ExecuteMethodCommand : AsyncCommand<ExecuteMethodCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<ExecuteMethodCommand> logger;

    public ExecuteMethodCommand(
        IOpcUaClient opcUaClient,
        ILogger<ExecuteMethodCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ExecuteMethodCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(
        ExecuteMethodCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
        ////var nodeId = settings.NodeId;

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value)
            : await opcUaClient.ConnectAsync(serverUrl);

        if (succeeded)
        {
            var arguments = new List<MethodExecutionParameter>();

            opcUaClient.ExecuteMethod(
                "ns=2;s=Methods/sqrt(x)",
                "ns=2;s=Methods",
                arguments);

            // TODO: FIx.
            ////if (succeededReading)
            ////{
            ////    logger.LogInformation($"Received the following data: '{nodeObject!.ToStringSimple()}'");
            ////}

            opcUaClient.Disconnect();
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return succeeded
            ? ConsoleExitStatusCodes.Success
            : ConsoleExitStatusCodes.Failure;
    }
}