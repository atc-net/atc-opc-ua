namespace Atc.Opc.Ua.CLI;

public static class Program
{
    public static Task<int> Main(string[] args)
    {
        if (args.Length == default)
        {
            args = new[]
            {
                //"node read object -s opc.tcp://opcuaserver.com:48010 -n \"ns=2;s=Demo.Dynamic.Scalar\"",
                ////"node read variable single -s opc.tcp://milo.digitalpetri.com:62541/milo -n \"ns=2;s=CTT/SecurityAccess/AccessLevel_CurrentRead_NotUser\"",
                ////node write variable single -s opc.tcp://milo.digitalpetri.com:62541/milo -n "ns=2;s=Dynamic/RandomFloat" -v 100.5
                "node", "read", "object",
                ////"node", "read", "variable", "single",
                ////"node", "write", "variable", "single",
                ////"testconnection",
                ////"-s", "opc.tcp://milo.digitalpetri.com:62541/milo",
                "-s", "opc.tcp://opcuaserver.com:48010",
                "-n", "\"ns=2;s=Demo.Dynamic.Scalar\"",
                ////"-n", "\"ns=9;i=56768\"",
                ////"-d", "string",
                ////"-d", "dummy",
                ////"-v", "Channel 1#",
                ////"-n", "\"ns=2;s=Dynamic/RandomInt32\"",
                ////"-n", "ns=2;s=CTT/SecurityAccess/AccessLevel_CurrentRead_NotUser",
                "--includeObjects",
                "--includeVariables",
                ////"--nodeObjectReadDepth", "1",
                ////"--outputAsJson",
                ////"--outputToFilePath", @"C:\Temp\asd.txt",
                ////"--outputFormat", "some_invalid_value",
                ////"--outputFileFolder", @"C:\Temp\flow\",
            };
        }

        ArgumentNullException.ThrowIfNull(args);

        args = SetHelpArgumentIfNeeded(args);

        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var consoleLoggerConfiguration = new ConsoleLoggerConfiguration();
        configuration.GetSection("ConsoleLogger").Bind(consoleLoggerConfiguration);

        ProgramCsHelper.SetMinimumLogLevelIfNeeded(args, consoleLoggerConfiguration);

        var serviceCollection = ServiceCollectionFactory.Create(consoleLoggerConfiguration);

        serviceCollection.AddTransient<IOpcUaClient, OpcUaClient>();

        var app = CommandAppFactory.Create(serviceCollection);
        app.ConfigureCommands();
        return app.RunAsync(args);
    }

    private static string[] SetHelpArgumentIfNeeded(string[] args)
    {
        if (args.Length == 0)
        {
            return new[] { CommandConstants.ArgumentShortHelp };
        }

        // TODO: Add multiple command help commands
        return args;
    }
}