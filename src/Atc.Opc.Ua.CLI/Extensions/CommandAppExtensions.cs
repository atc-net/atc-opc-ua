namespace Atc.Opc.Ua.CLI.Extensions;

public static class CommandAppExtensions
{
    private const string SampleOpcUaServerUrl = "opc.tcp://opcuaserver.com:48010";

    public static void ConfigureCommands(
        this CommandApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Configure(config =>
        {
            ConfigureTestConnectionCommand(config);
            config.AddBranch("node", ConfigureNodeCommands());
        });
    }

    private static void ConfigureTestConnectionCommand(
        IConfigurator config)
        => config.AddCommand<TestConnectionCommand>("testconnection")
            .WithDescription("Tests if a connection can be made to a given server.")
            .WithExample(new[] { $"testconnection -s {SampleOpcUaServerUrl}" })
            .WithExample(new[] { $"testconnection -s {SampleOpcUaServerUrl} -u username -p password" });

    private static Action<IConfigurator<CommandSettings>> ConfigureNodeCommands()
        => node =>
        {
            node.SetDescription("Operations related to nodes.");
            ConfigureNodeReadCommands(node);
            ConfigureNodeWriteCommands(node);
        };

    private static void ConfigureNodeReadCommands(
        IConfigurator<CommandSettings> node)
        => node.AddBranch("read", read =>
        {
            read.SetDescription("Operations related to reading nodes.");
            read.AddCommand<NodeReadObjectCommand>("object")
                .WithDescription("Reads a given node object.")
                .WithExample(new[] { $"node read object -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar\"" });

            read.AddBranch("variable", variable =>
            {
                variable.SetDescription("Reads one or more node variable(s).");
                variable.AddCommand<NodeReadVariableSingleCommand>("single")
                    .WithDescription("Reads a single node variable.")
                    .WithExample(new[] { $"node read variable single -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar.Float\"" });

                variable.AddCommand<NodeReadVariableMultiCommand>("multi")
                    .WithDescription("Reads a list of node variables.")
                    .WithExample(new[] { $"node read variable multi -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar.Float\" -n \"ns=2;s=Demo.Dynamic.Scalar.Int32\"" });
            });
        });

    private static void ConfigureNodeWriteCommands(IConfigurator<CommandSettings> node)
        => node.AddBranch("write", write =>
        {
            write.SetDescription("Operations related to writing nodes.");

            write.AddBranch("variable", variable =>
            {
                variable.SetDescription("Writes a value to one or more node variable(s).");
                variable.AddCommand<NodeWriteVariableSingleCommand>("single")
                    .WithDescription("Write a value to a single node variable.")
                    .WithExample(new[] { $"node write variable single -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar.Float\" -d float --value \"100.5\"" });
            });
        });
}