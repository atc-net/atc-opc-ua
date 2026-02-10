namespace Atc.Opc.Ua.CLI.Tui.Services;

/// <summary>
/// Records monitored OPC UA variable values to a CSV file.
/// Thread-safe for concurrent value updates.
/// </summary>
public sealed class CsvRecorder : IDisposable
{
    private readonly Lock writeLock = new();
    private StreamWriter? writer;
    private bool disposed;

    public bool IsRecording => writer is not null;

    public string? FilePath { get; private set; }

    /// <summary>
    /// Starts recording to the specified file path.
    /// </summary>
    /// <param name="filePath">The output CSV file path.</param>
    /// <returns>True if recording started successfully.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not crash if file cannot be created")]
    public bool Start(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        lock (writeLock)
        {
            if (writer is not null)
            {
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (directory is not null)
                {
                    Directory.CreateDirectory(directory);
                }

                writer = new StreamWriter(filePath, append: false, Encoding.UTF8);
                writer.WriteLine("Timestamp,NodeId,DisplayName,Value,StatusCode,ServerTimestamp");
                writer.Flush();
                FilePath = filePath;
                return true;
            }
            catch (Exception)
            {
                writer?.Dispose();
                writer = null;
                FilePath = null;
                return false;
            }
        }
    }

    /// <summary>
    /// Records a value change to the CSV file.
    /// </summary>
    /// <param name="value">The monitored node value to record.</param>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not crash on write failure")]
    public void Record(MonitoredNodeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        lock (writeLock)
        {
            if (writer is null)
            {
                return;
            }

            try
            {
                var timestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
                var serverTs = value.ServerTimestamp?.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
                var csvValue = EscapeCsvField(value.Value ?? string.Empty);
                var csvName = EscapeCsvField(value.DisplayName);

                writer.WriteLine($"{timestamp},{EscapeCsvField(value.NodeId)},{csvName},{csvValue},0x{value.StatusCode:X8},{serverTs}");
                writer.Flush();
            }
            catch (Exception)
            {
                // Silently ignore write failures to avoid disrupting the TUI.
            }
        }
    }

    /// <summary>
    /// Stops recording and closes the file.
    /// </summary>
    public void Stop()
    {
        lock (writeLock)
        {
            writer?.Dispose();
            writer = null;
            FilePath = null;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Stop();
        disposed = true;
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains('"', StringComparison.Ordinal) ||
            field.Contains(',', StringComparison.Ordinal) ||
            field.Contains('\n', StringComparison.Ordinal))
        {
            return $"\"{field.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return field;
    }
}
