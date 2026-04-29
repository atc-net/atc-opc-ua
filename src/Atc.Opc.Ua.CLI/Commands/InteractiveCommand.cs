namespace Atc.Opc.Ua.CLI.Commands;

public sealed class InteractiveCommand(
    IOpcUaInteractiveRunner runner) : AsyncCommand
{
    public override async Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);
        return await runner.RunAsync(cancellationToken);
    }
}
