namespace Atc.Opc.Ua.CLI.Tui.Dialogs;

/// <summary>
/// Dialog for writing a new value to an OPC UA variable node.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class WriteValueDialog : Dialog
{
    private readonly TextField newValueField;

    private WriteValueDialog(string displayName, string nodeId, string? currentValue, string? dataType)
    {
        Title = "Write Value";
        Width = 60;
        Height = 14;

        var nameLabel = new Label
        {
            Text = $"Node: {displayName}",
            X = 1,
            Y = 1,
        };

        var nodeIdLabel = new Label
        {
            Text = $"NodeId: {nodeId}",
            X = 1,
            Y = 2,
        };

        var typeLabel = new Label
        {
            Text = $"DataType: {dataType ?? "Unknown"}",
            X = 1,
            Y = 3,
        };

        var currentLabel = new Label
        {
            Text = $"Current: {currentValue ?? "(null)"}",
            X = 1,
            Y = 4,
        };

        var newLabel = new Label
        {
            Text = "New value:",
            X = 1,
            Y = 6,
        };

        newValueField = new TextField
        {
            Text = currentValue ?? string.Empty,
            X = 12,
            Y = 6,
            Width = Dim.Fill(2),
        };

        Add(nameLabel, nodeIdLabel, typeLabel, currentLabel, newLabel, newValueField);
    }

    public bool WasAccepted { get; private set; }

    public string NewValue => newValueField.Text.Trim() ?? string.Empty;

    /// <summary>
    /// Shows the write value dialog and returns it with the result.
    /// </summary>
    /// <param name="app">The Terminal.Gui application instance.</param>
    /// <param name="displayName">The display name of the node.</param>
    /// <param name="nodeId">The OPC UA node identifier.</param>
    /// <param name="currentValue">The current value of the node.</param>
    /// <param name="dataType">The data type name of the node.</param>
    /// <returns>The dialog with the new value and acceptance state.</returns>
    public static WriteValueDialog Show(
        IApplication app,
        string displayName,
        string nodeId,
        string? currentValue,
        string? dataType)
    {
        ArgumentNullException.ThrowIfNull(app);

        var dialog = new WriteValueDialog(displayName, nodeId, currentValue, dataType);

        var writeButton = new Button
        {
            Text = "Write",
            X = Pos.Center() - 8,
            Y = 8,
            IsDefault = true,
        };

        writeButton.Accepting += (_, e) =>
        {
            dialog.WasAccepted = true;
            app.RequestStop();
            e.Handled = true;
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = 8,
        };

        cancelButton.Accepting += (_, e) =>
        {
            app.RequestStop();
            e.Handled = true;
        };

        dialog.Add(writeButton, cancelButton);
        app.Run(dialog);

        return dialog;
    }
}
