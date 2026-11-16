namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class TestConnectionCommand : AsyncCommand<OpcUaBaseCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<TestConnectionCommand> logger;

    public TestConnectionCommand(
        IOpcUaClient opcUaClient,
        ILogger<TestConnectionCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        OpcUaBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        OpcUaBaseCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        return await OpcUaCommandRunner.RunAsync(
            opcUaClient,
            settings,
            logger,
            _ => Task.CompletedTask,
            cancellationToken);
    }
}