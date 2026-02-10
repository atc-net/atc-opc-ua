namespace Atc.Opc.Ua.CLI.Tui.Models;

/// <summary>
/// Persisted configuration for the TUI, including recent connections
/// and last-used subscriptions.
/// </summary>
public sealed class TuiConfiguration
{
    public List<RecentConnection> RecentConnections { get; set; } = [];

    public int MaxRecentConnections { get; set; } = 10;
}
