namespace Atc.Opc.Ua.CLI.Commands;

public sealed class InteractiveCommand : AsyncCommand
{
    public override Task<int> ExecuteAsync(
        CommandContext context,
        CancellationToken cancellationToken)
    {
        ConsoleHelper.WriteHeader();

        AnsiConsole.MarkupLine("[yellow]Interactive TUI mode is not yet available.[/]");
        AnsiConsole.MarkupLine("[dim]Use sub-commands (e.g. testconnection, node read, node scan) or run with --help.[/]");

        return Task.FromResult(ConsoleExitStatusCodes.Success);
    }
}
