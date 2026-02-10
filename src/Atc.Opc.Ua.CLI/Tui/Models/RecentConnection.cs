namespace Atc.Opc.Ua.CLI.Tui.Models;

/// <summary>
/// Represents a previously used OPC UA server connection.
/// </summary>
public sealed class RecentConnection
{
    public string ServerUrl { get; set; } = string.Empty;

    public string? UserName { get; set; }

    public DateTime LastUsed { get; set; }
}
