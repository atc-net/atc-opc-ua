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
        ExecuteMethodCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ExecuteMethodCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        return await OpcUaCommandRunner.RunAsync(
            opcUaClient,
            settings,
            logger,
            async ct =>
            {
                var executionResults = await opcUaClient.ExecuteMethodAsync(
                    settings.ParentNodeId,
                    settings.MethodNodeId,
                    MapSettingsToMethodExecutionParameters(settings),
                    ct);

                logger.LogInformation("Received the following data:");
                foreach (var executionResult in executionResults)
                {
                    var spaces = string.Empty.PadRight(16 - executionResult.DataEncoding.ToString().Length);
                    logger.LogInformation($"  {executionResult.DataEncoding}{spaces}{executionResult.Value}");
                }
            },
            cancellationToken);
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