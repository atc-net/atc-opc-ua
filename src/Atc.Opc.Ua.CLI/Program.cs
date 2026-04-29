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

        var app = CommandAppFactory.Create(serviceCollection);
        app.ConfigureCommands();

        if (args.Contains("--no-update-check", StringComparer.OrdinalIgnoreCase))
        {
            UpdateCheckRunner.SuppressForThisProcess();
            args = [.. args.Where(a => !string.Equals(a, "--no-update-check", StringComparison.OrdinalIgnoreCase))];
        }

        args = SetHelpArgumentIfNeeded(args);

        return app.RunAsync(args);
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
    {
        if (args.Length == 0)
        {
            return [CommandConstants.ArgumentShortHelp];
        }

        // TODO: Add multiple command help commands
        return args;
    }
}