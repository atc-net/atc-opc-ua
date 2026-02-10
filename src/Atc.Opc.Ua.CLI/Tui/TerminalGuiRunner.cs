namespace Atc.Opc.Ua.CLI.Tui;

/// <summary>
/// Terminal.Gui-based implementation of <see cref="IOpcUaInteractiveRunner"/>.
/// Initializes the Terminal.Gui application, creates the main window, and runs
/// until the user exits or cancellation is requested.
/// </summary>
public sealed class TerminalGuiRunner : IOpcUaInteractiveRunner
{
    private readonly IOpcUaClient opcUaClient;
    private readonly IOpcUaNodeBrowser nodeBrowser;
    private readonly ILogger<TerminalGuiRunner> logger;

    public TerminalGuiRunner(
        IOpcUaClient opcUaClient,
        IOpcUaNodeBrowser nodeBrowser,
        ILogger<TerminalGuiRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(opcUaClient);
        ArgumentNullException.ThrowIfNull(nodeBrowser);
        ArgumentNullException.ThrowIfNull(logger);

        this.opcUaClient = opcUaClient;
        this.nodeBrowser = nodeBrowser;
        this.logger = logger;
    }

    public Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        using var app = Application.Create().Init();
        using var registration = cancellationToken.Register(() => app.Invoke(() => app.RequestStop()));

        using var tuiService = new OpcUaTuiService(app, opcUaClient, nodeBrowser);
        var configService = new TuiConfigurationService();
        using var csvRecorder = new CsvRecorder();
        var mainWindow = new MainWindow(app, tuiService, configService, csvRecorder, logger);

        app.Run(mainWindow);

        return Task.FromResult(0);
    }
}
