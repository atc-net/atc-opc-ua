namespace Atc.Opc.Ua.CLI.Tui.Dialogs;

/// <summary>
/// Dialog for entering OPC UA server connection details.
/// Supports recent connections list for quick selection.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class ConnectDialog : Dialog
{
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "Default example endpoint")]
    private static string lastServerUrl = "opc.tcp://localhost:48010";
    private static string lastUserName = string.Empty;
    private static string lastPassword = string.Empty;
    private static bool lastUseAuth;

    private readonly TextField serverUrlField;
    private readonly OptionSelector authTypeRadio;
    private readonly Label userNameLabel;
    private readonly TextField userNameField;
    private readonly Label passwordLabel;
    private readonly TextField passwordField;

    private ConnectDialog(IReadOnlyList<RecentConnection>? recentConnections)
    {
        Title = "Connect to OPC UA Server";
        Width = 60;

        var yOffset = 0;

        if (recentConnections is { Count: > 0 })
        {
            Height = 18;
            yOffset = 4;
            AddRecentConnectionsList(recentConnections);
        }
        else
        {
            Height = 14;
        }

        serverUrlField = CreateTextField(lastServerUrl, 1 + yOffset);

        authTypeRadio = new OptionSelector
        {
            X = 18,
            Y = 3 + yOffset,
            Labels = ["Anonymous", "Username/Password"],
            Orientation = Orientation.Horizontal,
            Value = lastUseAuth ? 1 : 0,
        };

        userNameLabel = new Label { Text = "Username:", X = 1, Y = 5 + yOffset, Visible = lastUseAuth };
        userNameField = CreateTextField(lastUserName, 5 + yOffset);
        userNameField.Visible = lastUseAuth;

        passwordLabel = new Label { Text = "Password:", X = 1, Y = 7 + yOffset, Visible = lastUseAuth };
        passwordField = CreateTextField(lastPassword, 7 + yOffset, secret: true);
        passwordField.Visible = lastUseAuth;

        authTypeRadio.ValueChanged += (_, _) =>
        {
            var showCredentials = authTypeRadio.Value == 1;
            userNameLabel.Visible = showCredentials;
            userNameField.Visible = showCredentials;
            passwordLabel.Visible = showCredentials;
            passwordField.Visible = showCredentials;
        };

        AddFieldLabels(yOffset);
    }

    public bool WasAccepted { get; private set; }

    public string ServerUrl => serverUrlField.Text.Trim() ?? string.Empty;

    public string? UserName
    {
        get
        {
            if (authTypeRadio.Value == 0)
            {
                return null;
            }

            var text = userNameField.Text.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }

    public string? Password
    {
        get
        {
            if (authTypeRadio.Value == 0)
            {
                return null;
            }

            var text = passwordField.Text.Trim();
            return string.IsNullOrEmpty(text) ? null : text;
        }
    }

    /// <summary>
    /// Shows the connect dialog and returns it with the result.
    /// </summary>
    /// <param name="app">The Terminal.Gui application instance.</param>
    /// <param name="recentConnections">Optional list of recent connections to display.</param>
    /// <returns>The dialog with connection details and acceptance state.</returns>
    public static ConnectDialog Show(
        IApplication app,
        IReadOnlyList<RecentConnection>? recentConnections = null)
    {
        ArgumentNullException.ThrowIfNull(app);

        var dialog = new ConnectDialog(recentConnections);

        var buttonY = recentConnections is { Count: > 0 } ? 13 : 9;

        var connectButton = new Button
        {
            Text = "Connect",
            X = Pos.Center() - 10,
            Y = buttonY,
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

            if (dialog.authTypeRadio.Value == 1)
            {
                if (string.IsNullOrWhiteSpace(dialog.userNameField.Text))
                {
                    MessageBox.Query(app, "Validation", "Username is required.", "OK");
                    dialog.userNameField.SetFocus();
                    e.Handled = true;
                    return;
                }

                if (string.IsNullOrWhiteSpace(dialog.passwordField.Text))
                {
                    MessageBox.Query(app, "Validation", "Password is required.", "OK");
                    dialog.passwordField.SetFocus();
                    e.Handled = true;
                    return;
                }
            }

            dialog.WasAccepted = true;
            lastServerUrl = dialog.ServerUrl;
            lastUseAuth = dialog.authTypeRadio.Value == 1;
            lastUserName = dialog.userNameField.Text.Trim() ?? string.Empty;
            lastPassword = dialog.passwordField.Text.Trim() ?? string.Empty;
            app.RequestStop();
            e.Handled = true;
        };

        var cancelButton = new Button
        {
            Text = "Cancel",
            X = Pos.Center() + 5,
            Y = buttonY,
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

    private void AddRecentConnectionsList(
        IReadOnlyList<RecentConnection> recentConnections)
    {
        var recentLabel = new Label
        {
            Text = "Recent:",
            X = 1,
            Y = 1,
        };

        var recentItems = new ObservableCollection<string>(
            recentConnections.Select(x =>
                x.UserName is not null
                    ? $"{x.ServerUrl} ({x.UserName})"
                    : x.ServerUrl));

        var recentList = new ListView
        {
            X = 14,
            Y = 1,
            Width = Dim.Fill(2),
            Height = 3,
        };

        recentList.SetSource(recentItems);

        recentList.ValueChanged += (_, _) =>
        {
            var idx = recentList.SelectedItem ?? -1;
            if (idx >= 0 && idx < recentConnections.Count)
            {
                var conn = recentConnections[idx];
                serverUrlField.Text = conn.ServerUrl;
                authTypeRadio.Value = conn.UserName is not null ? 1 : 0;
                userNameField.Text = conn.UserName ?? string.Empty;
                passwordField.Text = string.Empty;
            }
        };

        Add(recentLabel, recentList);
    }

    private static TextField CreateTextField(
        string initialText,
        int y,
        bool secret = false)
        => new()
        {
            Text = initialText,
            Secret = secret,
            X = 14,
            Y = y,
            Width = Dim.Fill(2),
        };

    private void AddFieldLabels(int yOffset)
    {
        var urlLabel = new Label { Text = "Server URL:", X = 1, Y = 1 + yOffset };
        var authLabel = new Label { Text = "Authentication:", X = 1, Y = 3 + yOffset };

        Add(
            urlLabel,
            serverUrlField,
            authLabel,
            authTypeRadio,
            userNameLabel,
            userNameField,
            passwordLabel,
            passwordField);
    }
}