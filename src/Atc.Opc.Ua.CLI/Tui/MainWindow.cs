namespace Atc.Opc.Ua.CLI.Tui;

/// <summary>
/// Main application window for the interactive OPC UA TUI.
/// Provides a multi-panel layout for browsing, monitoring, and interacting
/// with OPC UA servers.
/// </summary>
[SuppressMessage("Major Code Smell", "S4487:Unread \"private\" fields should be removed", Justification = "Fields will be used in later TUI phases.")]
public sealed class MainWindow : Window
{
    private readonly IApplication app;
    private readonly OpcUaTuiService tuiService;
    private readonly ILogger logger;

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

        InitializeLayout();
        InitializeKeyBindings();
    }

    private void InitializeLayout()
    {
        var statusLabel = new Label
        {
            Text = "Press 'c' to connect, '?' for help, 'q' to quit",
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
        };

        Add(statusLabel);
    }

    private void InitializeKeyBindings()
    {
        app.Keyboard.KeyDown += (_, e) =>
        {
            switch (e.KeyCode)
            {
                case Terminal.Gui.Drivers.KeyCode.Q:
                    if (!e.IsAlt && !e.IsCtrl)
                    {
                        app.RequestStop();
                        e.Handled = true;
                    }

                    break;
            }
        };
    }
}
