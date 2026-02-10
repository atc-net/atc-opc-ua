namespace Atc.Opc.Ua.CLI.Tui;

/// <summary>
/// Abstraction for the interactive TUI runner, decoupling Terminal.Gui
/// from the rest of the CLI codebase.
/// </summary>
public interface IOpcUaInteractiveRunner
{
    /// <summary>
    /// Starts the interactive TUI and blocks until the user exits.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>The exit code.</returns>
    Task<int> RunAsync(CancellationToken cancellationToken = default);
}
