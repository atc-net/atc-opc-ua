using System.Collections.Concurrent;
using System.Data;

namespace Atc.Opc.Ua.CLI.Tui.Views;

/// <summary>
/// TableView-based panel showing real-time values of monitored OPC UA variables.
/// Uses batched updates (50 ms) to avoid excessive redraws under high-frequency data.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
[SuppressMessage("Major Code Smell", "S4487:Unread \"private\" fields should be removed", Justification = "Timer handle kept for future cancellation support")]
public sealed class MonitoredVariablesView : FrameView
{
    private const int UpdateBatchIntervalMs = 50;

    private readonly IApplication app;
    private readonly TableView tableView;
    private readonly DataTable dataTable;
    private readonly Dictionary<uint, DataRow> rowsByHandle = [];
    private readonly ConcurrentDictionary<uint, MonitoredNodeValue> pendingUpdates = [];
    private readonly Label emptyLabel;
    private readonly Lock timerLock = new();
    private object? updateTimer;
    private bool updateTimerRunning;

    public MonitoredVariablesView(IApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        this.app = app;

        Title = "Monitored Variables";
        CanFocus = true;

        dataTable = new DataTable();
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("NodeId", typeof(string));
        dataTable.Columns.Add("Value", typeof(string));
        dataTable.Columns.Add("Status", typeof(string));
        dataTable.Columns.Add("Timestamp", typeof(string));

        tableView = new TableView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Table = new DataTableSource(dataTable),
            FullRowSelect = true,
        };

        tableView.Style.ShowHorizontalHeaderOverline = false;
        tableView.Style.ShowHorizontalHeaderUnderline = true;
        tableView.Style.ShowHorizontalBottomline = false;
        tableView.Style.AlwaysShowHeaders = true;
        tableView.Style.ShowVerticalCellLines = false;
        tableView.Style.ShowVerticalHeaderLines = false;
        tableView.Style.ExpandLastColumn = true;

        emptyLabel = new Label
        {
            Text = "No monitored variables. Select a variable node and press Enter to subscribe.",
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        Add(emptyLabel, tableView);
        tableView.Visible = false;
    }

    /// <summary>
    /// Raised when the user requests to unsubscribe a variable (Delete/Backspace).
    /// </summary>
    [SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Internal TUI event, not public API")]
    [SuppressMessage("Design", "MA0046:The delegate must have 2 parameters", Justification = "Internal TUI event")]
    public event Action<uint>? UnsubscribeRequested;

    public void AddVariable(uint handle, MonitoredNodeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (rowsByHandle.ContainsKey(handle))
        {
            return;
        }

        var row = dataTable.NewRow();
        row["Name"] = value.DisplayName;
        row["NodeId"] = value.NodeId;
        row["Value"] = value.Value ?? string.Empty;
        row["Status"] = value.IsGood ? "Good" : $"0x{value.StatusCode:X8}";
        row["Timestamp"] = value.ServerTimestamp?.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) ?? string.Empty;
        dataTable.Rows.Add(row);
        rowsByHandle[handle] = row;

        tableView.Visible = true;
        emptyLabel.Visible = false;
        tableView.Update();
    }

    public void RemoveVariable(uint handle)
    {
        if (!rowsByHandle.TryGetValue(handle, out var row))
        {
            return;
        }

        dataTable.Rows.Remove(row);
        rowsByHandle.Remove(handle);
        tableView.Update();

        if (rowsByHandle.Count == 0)
        {
            tableView.Visible = false;
            emptyLabel.Visible = true;
        }
    }

    /// <summary>
    /// Queues a value update keyed by monitored item handle.
    /// </summary>
    /// <param name="handle">The monitored item handle.</param>
    /// <param name="value">The updated node value.</param>
    public void UpdateVariable(uint handle, MonitoredNodeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        pendingUpdates[handle] = value;
        EnsureUpdateTimerRunning();
    }

    public void Clear()
    {
        dataTable.Rows.Clear();
        rowsByHandle.Clear();
        pendingUpdates.Clear();
        tableView.Update();
        tableView.Visible = false;
        emptyLabel.Visible = true;
    }

    public void HandleDeleteKey()
    {
        var selectedRow = tableView.SelectedRow;
        if (selectedRow < 0 || selectedRow >= dataTable.Rows.Count)
        {
            return;
        }

        // Find the handle for the selected row
        foreach (var kvp in rowsByHandle)
        {
            if (kvp.Value == dataTable.Rows[selectedRow])
            {
                UnsubscribeRequested?.Invoke(kvp.Key);
                break;
            }
        }
    }

    private void EnsureUpdateTimerRunning()
    {
        lock (timerLock)
        {
            if (updateTimerRunning)
            {
                return;
            }

            updateTimerRunning = true;
            updateTimer = app.AddTimeout(
                TimeSpan.FromMilliseconds(UpdateBatchIntervalMs),
                ProcessPendingUpdates);
        }
    }

    private bool ProcessPendingUpdates()
    {
        if (pendingUpdates.IsEmpty)
        {
            lock (timerLock)
            {
                updateTimerRunning = false;
            }

            return false;
        }

        var keys = pendingUpdates.Keys.ToList();
        foreach (var key in keys)
        {
            if (!pendingUpdates.TryRemove(key, out var value))
            {
                continue;
            }

            if (!rowsByHandle.TryGetValue(key, out var row))
            {
                continue;
            }

            row["Value"] = value.Value ?? string.Empty;
            row["Status"] = value.IsGood ? "Good" : $"0x{value.StatusCode:X8}";
            row["Timestamp"] = value.ServerTimestamp?.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) ?? string.Empty;
        }

        tableView.Update();

        var hasMore = !pendingUpdates.IsEmpty;
        if (!hasMore)
        {
            lock (timerLock)
            {
                updateTimerRunning = false;
            }
        }

        return hasMore;
    }
}
