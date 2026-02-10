using System.Collections.ObjectModel;

namespace Atc.Opc.Ua.CLI.Tui.Views;

/// <summary>
/// Scrollable log panel for displaying connection, operation, and error messages.
/// Auto-scrolls to the latest message.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class LogView : FrameView
{
    private const int MaxEntries = 500;

    private readonly IApplication app;
    private readonly ListView listView;
    private readonly ObservableCollection<string> entries = [];

    public LogView(IApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        this.app = app;

        Title = "Log";

        listView = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
        };

        listView.SetSource(entries);
        Add(listView);
    }

    public void AddLog(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        var entry = $"[{timestamp}] [{level}] {message}";

        app.Invoke(() =>
        {
            entries.Add(entry);

            while (entries.Count > MaxEntries)
            {
                entries.RemoveAt(0);
            }

            if (entries.Count > 0)
            {
                listView.SelectedItem = entries.Count - 1;
            }
        });
    }

    public void AddInfo(string message) => AddLog("INFO", message);

    public void AddWarning(string message) => AddLog("WARN", message);

    public void AddError(string message) => AddLog("ERROR", message);

    public void Clear()
    {
        entries.Clear();
    }
}
