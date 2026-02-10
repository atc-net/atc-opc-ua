namespace Atc.Opc.Ua.CLI.Tui.Dialogs;

/// <summary>
/// Dialog for entering OPC UA server connection details.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class ConnectDialog : Dialog
{
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Default example endpoint")]
    private static string lastServerUrl = "opc.tcp://localhost:48010";
    private static string lastUserName = string.Empty;
    private static string lastPassword = string.Empty;

    private readonly TextField serverUrlField;
    private readonly TextField userNameField;
    private readonly TextField passwordField;

    private ConnectDialog()
    {
        Title = "Connect to OPC UA Server";
        Width = 60;
        Height = 14;

        var urlLabel = new Label
        {
            Text = "Server URL:",
            X = 1,
            Y = 1,
        };

        serverUrlField = new TextField
        {
            Text = lastServerUrl,
            X = 14,
            Y = 1,
            Width = Dim.Fill(2),
        };

        var userLabel = new Label
        {
            Text = "Username:",
            X = 1,
            Y = 3,
        };

        userNameField = new TextField
        {
            Text = lastUserName,
            X = 14,
            Y = 3,
            Width = Dim.Fill(2),
        };

        var passLabel = new Label
        {
            Text = "Password:",
            X = 1,
            Y = 5,
        };

        passwordField = new TextField
        {
            Text = lastPassword,
            Secret = true,
            X = 14,
            Y = 5,
            Width = Dim.Fill(2),
        };

        var hint = new Label
        {
            Text = "(Leave username/password empty for anonymous)",
            X = 1,
            Y = 7,
        };

        Add(urlLabel, serverUrlField, userLabel, userNameField, passLabel, passwordField, hint);
    }

    public bool WasAccepted { get; private set; }

    public string ServerUrl => serverUrlField.Text.Trim() ?? string.Empty;

    public string? UserName
    {
        get
        {
            var text = userNameField.Text.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }

    public string? Password
    {
        get
        {
            var text = passwordField.Text.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }

    /// <summary>
    /// Shows the connect dialog and returns it with the result.
    /// </summary>
    /// <param name="app">The Terminal.Gui application instance.</param>
    /// <returns>The dialog with connection details and acceptance state.</returns>
    public static ConnectDialog Show(IApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var dialog = new ConnectDialog();

        var connectButton = new Button
        {
            Text = "Connect",
            X = Pos.Center() - 10,
            Y = 9,
            IsDefault = true,
        };

        connectButton.Accepting += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(dialog.serverUrlField.Text))
            {
                MessageBox.Query(app, "Validation", "Server URL is required.", "OK");
                e.Handled = true;
                return;
            }

            dialog.WasAccepted = true;
            lastServerUrl = dialog.ServerUrl;
            lastUserName = dialog.userNameField.Text.Trim() ?? string.Empty;
            lastPassword = dialog.passwordField.Text.Trim() ?? string.Empty;
            app.RequestStop();
            e.Handled = true;
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = 9,
        };

        cancelButton.Accepting += (_, e) =>
        {
            app.RequestStop();
            e.Handled = true;
        };

        dialog.Add(connectButton, cancelButton);
        app.Run(dialog);

        return dialog;
    }
}
