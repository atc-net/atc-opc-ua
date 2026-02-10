namespace Atc.Opc.Ua.CLI;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration.GetSection("ConsoleLogger").Bind(consoleLoggerConfiguration);

        ProgramCsHelper.SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);

        serviceCollection.AddTransient<IOpcUaClient, OpcUaClient>();
        serviceCollection.AddTransient<IOpcUaScanner, OpcUaScanner>();
        serviceCollection.AddTransient<IOpcUaNodeBrowser, OpcUaNodeBrowser>();
        serviceCollection.AddTransient<IOpcUaInteractiveRunner, TerminalGuiRunner>();

        var app = CommandAppFactory.CreateWithRootCommand<InteractiveCommand>(serviceCollection);
        app.ConfigureCommands();

        if (IsNonInteractiveTerminal(args))
        {
            args = [CommandConstants.ArgumentShortHelp];
        }

        return app.RunAsync(args);
    }

    private static bool IsNonInteractiveTerminal(string[] args)
        => args.Length == 0 &&
           (System.Console.IsInputRedirected || System.Console.IsOutputRedirected);
}
