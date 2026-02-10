namespace Atc.Opc.Ua.CLI.Tui;

/// <summary>
/// Main application window for the interactive OPC UA TUI.
/// Provides a multi-panel layout for browsing, monitoring, and interacting
/// with OPC UA servers.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
[SuppressMessage("Major Code Smell", "S4487:Unread \"private\" fields should be removed", Justification = "Logger will be used in later TUI phases")]
public sealed class MainWindow : Window
{
    private readonly IApplication app;
    private readonly OpcUaTuiService tuiService;
    private readonly ILogger logger;
    private readonly AddressSpaceView addressSpaceView;
    private readonly MonitoredVariablesView monitoredVariablesView;
    private readonly NodeDetailsView nodeDetailsView;
    private readonly LogView logView;

    public MainWindow(
        IApplication app,
        OpcUaTuiService tuiService,
        ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(tuiService);
        ArgumentNullException.ThrowIfNull(logger);

        this.app = app;
        this.tuiService = tuiService;
        this.logger = logger;

        Title = "atc-opc-ua - Interactive OPC UA Client";

        addressSpaceView = new AddressSpaceView(app, tuiService)
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(35),
            Height = Dim.Percent(60),
        };

        monitoredVariablesView = new MonitoredVariablesView(app)
        {
            X = Pos.Right(addressSpaceView),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(60),
        };

        nodeDetailsView = new NodeDetailsView(app, tuiService)
        {
            X = 0,
            Y = Pos.Bottom(addressSpaceView),
            Width = Dim.Fill(),
            Height = 5,
        };

        logView = new LogView(app)
        {
            X = 0,
            Y = Pos.Bottom(nodeDetailsView),
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
        };

        var statusLabel = new Label
        {
            Text = " [c] Connect  [d] Disconnect  [w] Write  [r] Refresh  [Tab] Switch  [?] Help  [q] Quit",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };

        Add(addressSpaceView, monitoredVariablesView, nodeDetailsView, logView, statusLabel);

        WireEvents();
        InitializeKeyBindings();
    }

    private void WireEvents()
    {
        addressSpaceView.NodeSelected += OnNodeSelected;
        addressSpaceView.SubscribeRequested += node => _ = RunGuardedAsync(() => SubscribeToNodeAsync(node));
    }

    private void InitializeKeyBindings()
    {
        app.Keyboard.KeyDown += (_, key) =>
        {
            if (app.TopRunnableView != this)
            {
                return;
            }

            HandleKeyDown(key);
        };
    }

    private void HandleKeyDown(Terminal.Gui.Input.Key key)
    {
        if (key == Terminal.Gui.Input.Key.Q)
        {
            ConfirmQuit();
            key.Handled = true;
        }
        else if (key == Terminal.Gui.Input.Key.C)
        {
            _ = RunGuardedAsync(ShowConnectDialogAsync);
            key.Handled = true;
        }
        else if (key == Terminal.Gui.Input.Key.D)
        {
            _ = RunGuardedAsync(DisconnectAsync);
            key.Handled = true;
        }
        else if (key == Terminal.Gui.Input.Key.W)
        {
            _ = RunGuardedAsync(ShowWriteDialogAsync);
            key.Handled = true;
        }
        else if (key == Terminal.Gui.Input.Key.R)
        {
            _ = RunGuardedAsync(RefreshTreeAsync);
            key.Handled = true;
        }
        else if (key == Terminal.Gui.Input.Key.Tab)
        {
            CycleFocus();
            key.Handled = true;
        }
        else if (key == '?')
        {
            ShowHelp();
            key.Handled = true;
        }
        else if ((key == Terminal.Gui.Input.Key.Delete || key == Terminal.Gui.Input.Key.Backspace) &&
                 monitoredVariablesView.HasFocus)
        {
            monitoredVariablesView.HandleDeleteKey();
            key.Handled = true;
        }
    }

    private void CycleFocus()
    {
        if (addressSpaceView.HasFocus)
        {
            monitoredVariablesView.SetFocus();
        }
        else
        {
            addressSpaceView.SetFocus();
        }
    }

    private void OnNodeSelected(BrowsedNode node)
    {
        _ = RunGuardedAsync(() => nodeDetailsView.ShowNodeAsync(
            node.NodeId,
            node.DisplayName,
            CancellationToken.None));
    }

    private async Task SubscribeToNodeAsync(BrowsedNode node)
    {
        if (!tuiService.IsConnected)
        {
            logView.AddWarning("Not connected to a server.");
            return;
        }

        var (subCreated, subError) = await tuiService
            .CreateSubscriptionAsync(null, CancellationToken.None);

        if (!subCreated)
        {
            logView.AddError($"Failed to create subscription: {subError}");
            return;
        }

        var (succeeded, handle, errorMessage) = await tuiService
            .SubscribeToNodeAsync(node.NodeId, node.DisplayName, CancellationToken.None);

        if (!succeeded)
        {
            logView.AddError($"Subscribe failed for {node.DisplayName}: {errorMessage}");
            return;
        }

        var initialValue = new MonitoredNodeValue
        {
            NodeId = node.NodeId,
            DisplayName = node.DisplayName,
            DataTypeName = node.DataTypeName,
        };

        app.Invoke(() =>
        {
            monitoredVariablesView.AddVariable(handle, initialValue);
            logView.AddInfo($"Subscribed to {node.DisplayName} ({node.NodeId})");
        });
    }

    private async Task DisconnectAsync()
    {
        if (!tuiService.IsConnected)
        {
            logView.AddWarning("Not connected.");
            return;
        }

        var (succeeded, errorMessage) = await tuiService.DisconnectAsync(CancellationToken.None);

        app.Invoke(() =>
        {
            if (succeeded)
            {
                addressSpaceView.Clear();
                monitoredVariablesView.Clear();
                nodeDetailsView.Clear();
                logView.AddInfo("Disconnected.");
            }
            else
            {
                logView.AddError($"Disconnect failed: {errorMessage}");
            }
        });
    }

    private async Task ShowConnectDialogAsync()
    {
        if (tuiService.IsConnected)
        {
            logView.AddWarning("Already connected. Disconnect first.");
            return;
        }

        var dialog = ConnectDialog.Show(app);
        if (!dialog.WasAccepted)
        {
            return;
        }

        logView.AddInfo($"Connecting to {dialog.ServerUrl}...");

        var serverUri = new Uri(dialog.ServerUrl);
        var (succeeded, errorMessage) = await tuiService
            .ConnectAsync(serverUri, dialog.UserName, dialog.Password, CancellationToken.None);

        if (succeeded)
        {
            logView.AddInfo($"Connected to {dialog.ServerUrl}");
            await addressSpaceView.LoadRootAsync(CancellationToken.None);
        }
        else
        {
            logView.AddError($"Connection failed: {errorMessage}");
        }
    }

    private async Task ShowWriteDialogAsync()
    {
        var selectedNode = addressSpaceView.SelectedNode;
        if (selectedNode is null || selectedNode.NodeClass != NodeClassType.Variable)
        {
            logView.AddWarning("Select a variable node first.");
            return;
        }

        if (!tuiService.IsConnected)
        {
            logView.AddWarning("Not connected to a server.");
            return;
        }

        // Read current value for the dialog
        var (attrOk, attributes, _) = await tuiService
            .ReadNodeAttributesAsync(selectedNode.NodeId, CancellationToken.None);

        var currentValue = attrOk ? attributes?.Value : null;
        var dataType = selectedNode.DataTypeName;

        var dialog = WriteValueDialog.Show(app, selectedNode.DisplayName, selectedNode.NodeId, currentValue, dataType);
        if (!dialog.WasAccepted)
        {
            return;
        }

        var (writeOk, writeError) = await tuiService
            .WriteNodeAsync(selectedNode.NodeId, dialog.NewValue, CancellationToken.None);

        if (writeOk)
        {
            logView.AddInfo($"Wrote '{dialog.NewValue}' to {selectedNode.DisplayName}");
        }
        else
        {
            logView.AddError($"Write failed: {writeError}");
        }
    }

    private async Task RefreshTreeAsync()
    {
        if (!tuiService.IsConnected)
        {
            logView.AddWarning("Not connected.");
            return;
        }

        logView.AddInfo("Refreshing address space...");
        await addressSpaceView.LoadRootAsync(CancellationToken.None);
        logView.AddInfo("Address space refreshed.");
    }

    private void ConfirmQuit()
    {
        var result = MessageBox.Query(
            app,
            "Quit",
            "Are you sure you want to exit?",
            "Cancel",
            "Quit");

        if (result == 1)
        {
            app.RequestStop();
        }
    }

    private void ShowHelp()
    {
        const string helpText = """
                                Keyboard Shortcuts
                                ==================

                                Navigation:
                                  Tab          Switch focus between panels
                                  Arrow keys   Navigate within panels

                                Connection:
                                  c            Connect to OPC UA server
                                  d            Disconnect
                                  r            Refresh address space

                                Actions:
                                  Enter        Subscribe to selected variable
                                  Delete       Unsubscribe selected variable
                                  w            Write value to selected variable

                                General:
                                  ?            Show this help
                                  q            Quit
                                """;

        var trimmed = helpText.TrimEnd();
        var lineCount = trimmed.Split('\n').Length;

        var dialog = new Dialog
        {
            Title = "Help",
            Width = 52,
            Height = lineCount + 6,
        };

        var label = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = lineCount,
            Text = trimmed,
            CanFocus = false,
        };

        var okButton = new Button
        {
            X = Pos.Center(),
            Y = lineCount + 1,
            Text = "OK",
            IsDefault = true,
        };

        okButton.Accepting += (_, _) => app.RequestStop();

        dialog.Add(label, okButton);
        app.Run(dialog);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Top-level guard for fire-and-forget async")]
    private async Task RunGuardedAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            app.Invoke(() => logView.AddError(ex.Message));
        }
    }
}
