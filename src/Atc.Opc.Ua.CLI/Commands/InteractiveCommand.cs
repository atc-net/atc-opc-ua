namespace Atc.Opc.Ua.CLI.Commands;

public sealed class InteractiveCommand(
    IOpcUaInteractiveRunner runner) : AsyncCommand
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();
        return runner.RunAsync(cancellationToken);
    }
}
