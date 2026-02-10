using System.Text.Json;

namespace Atc.Opc.Ua.CLI.Tui.Services;

/// <summary>
/// Manages persistent TUI configuration (recent connections, preferences).
/// Stores configuration as JSON in the user's app data folder.
/// </summary>
public sealed class TuiConfigurationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string configFilePath;
    private readonly TuiConfiguration configuration;

    public TuiConfigurationService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "atc-opc-ua");

        configFilePath = Path.Combine(appDataPath, "tui-config.json");
        configuration = Load();
    }

    public IReadOnlyList<RecentConnection> RecentConnections => configuration.RecentConnections;

    /// <summary>
    /// Adds or updates a connection in the recent connections list.
    /// </summary>
    /// <param name="serverUrl">The OPC UA server URL.</param>
    /// <param name="userName">Optional username.</param>
    [SuppressMessage("Design", "CA1054:URI parameters should not be strings", Justification = "URLs are strings in TUI text fields")]
    public void AddRecentConnection(string serverUrl, string? userName)
    {
        ArgumentException.ThrowIfNullOrEmpty(serverUrl);

        configuration.RecentConnections.RemoveAll(c =>
            c.ServerUrl.Equals(serverUrl, StringComparison.OrdinalIgnoreCase));

        configuration.RecentConnections.Insert(0, new RecentConnection
        {
            ServerUrl = serverUrl,
            UserName = userName,
            LastUsed = DateTime.UtcNow,
        });

        while (configuration.RecentConnections.Count > configuration.MaxRecentConnections)
        {
            configuration.RecentConnections.RemoveAt(configuration.RecentConnections.Count - 1);
        }

        Save();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Config load must not crash the app")]
    private TuiConfiguration Load()
    {
        try
        {
            if (!File.Exists(configFilePath))
            {
                return new TuiConfiguration();
            }

            var json = File.ReadAllText(configFilePath);
            return JsonSerializer.Deserialize<TuiConfiguration>(json, JsonOptions) ?? new TuiConfiguration();
        }
        catch (Exception)
        {
            return new TuiConfiguration();
        }
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Config save must not crash the app")]
    private void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(configFilePath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(configuration, JsonOptions);
            File.WriteAllText(configFilePath, json);
        }
        catch (Exception)
        {
            // Silently ignore save failures.
        }
    }
}
