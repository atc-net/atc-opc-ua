namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class ExecuteMethodCommand : AsyncCommand<ExecuteMethodCommandSettings>
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

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value)
            : await opcUaClient.ConnectAsync(serverUrl);

        if (succeeded)
        {
            var (succeededExecutingMethod, executionResults, errorMessage) = opcUaClient.ExecuteMethod(
                settings.ParentNodeId,
                settings.MethodNodeId,
                MapSettingsToMethodExecutionParameters(settings));

            if (succeededExecutingMethod)
            {
                if (executionResults is not null)
                {
                    logger.LogInformation("Received the following data:");
                    foreach (var executionResult in executionResults)
                    {
                        var spaces = string.Empty.PadRight(16 - executionResult.DataEncoding.ToString().Length);
                        logger.LogInformation($"  {executionResult.DataEncoding}{spaces}{executionResult.Value}");
                    }
                }
            }
            else
            {
                logger.LogError(errorMessage);
            }

            opcUaClient.Disconnect();
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return succeeded
            ? ConsoleExitStatusCodes.Success
            : ConsoleExitStatusCodes.Failure;
    }

    private static List<MethodExecutionParameter> MapSettingsToMethodExecutionParameters(
        ExecuteMethodCommandSettings settings)
    {
        var data = new List<MethodExecutionParameter>();
        for (var i = 0; i < settings.DataTypes.Length; i++)
        {
            data.Add(
                new MethodExecutionParameter(
                    Enum<OpcUaDataEncodingType>.Parse(settings.DataTypes[i]),
                    settings.DataValues[i]));
        }

        return data;
    }
}