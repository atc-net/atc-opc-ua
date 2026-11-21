namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class ExecuteMethodCommand : AsyncCommand<ExecuteMethodCommandSettings>
{
    private readonly OpcUaClientFactory clientFactory;
    private readonly ILogger<ExecuteMethodCommand> logger;

    public ExecuteMethodCommand(
        OpcUaClientFactory clientFactory,
        ILogger<ExecuteMethodCommand> logger)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
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
            clientFactory,
            settings,
            logger,
            async (client, ct) =>
            {
                var executionResults = await client.ExecuteMethodAsync(
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