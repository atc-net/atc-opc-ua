namespace Atc.Opc.Ua.CLI.Extensions;

public static class CommandAppExtensions
{
    [SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "OK.")]
    private const string SampleOpcUaServerUrl = "opc.tcp://opcuaserver.com:48010";

    public static void ConfigureCommands(
        this CommandApp app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.Configure(config =>
        {
            ConfigureTestConnectionCommand(config);
            config.AddBranch("node", ConfigureNodeCommands());

            config.AddBranch("method", ConfigureMethodCommands());
        });
    }

    private static void ConfigureTestConnectionCommand(
        IConfigurator config)
        => config.AddCommand<TestConnectionCommand>("testconnection")
            .WithDescription("Tests if a connection can be made to a given server.")
            .WithExample($"testconnection -s {SampleOpcUaServerUrl}")
            .WithExample($"testconnection -s {SampleOpcUaServerUrl} -u username -p password");

    private static Action<IConfigurator<CommandSettings>> ConfigureNodeCommands()
        => node =>
        {
            node.SetDescription("Operations related to nodes.");
            ConfigureNodeReadCommands(node);
            ConfigureNodeWriteCommands(node);
            node.AddCommand<NodeScanCommand>("scan")
                .WithDescription("Scans part of the address space starting at a given node.")
                .WithExample($"node scan -s {SampleOpcUaServerUrl} --starting-node-id \"ns=2;s=Demo.Dynamic.Scalar\" --object-depth 2 --variable-depth 1");
        };

    private static Action<IConfigurator<CommandSettings>> ConfigureMethodCommands()
        => method =>
        {
            method.SetDescription("Operations related to methods.");

            method.AddCommand<ExecuteMethodCommand>("execute")
                .WithDescription("Used to execute a given method.");
        };

    private static void ConfigureNodeReadCommands(
        IConfigurator<CommandSettings> node)
        => node.AddBranch("read", read =>
        {
            read.SetDescription("Operations related to reading nodes.");
            read.AddCommand<NodeReadObjectCommand>("object")
                .WithDescription("Reads a given node object.")
                .WithExample($"node read object -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar\"");

            read.AddBranch("variable", variable =>
            {
                variable.SetDescription("Reads one or more node variable(s).");
                variable.AddCommand<NodeReadVariableSingleCommand>("single")
                    .WithDescription("Reads a single node variable.")
                    .WithExample($"node read variable single -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar.Float\"");

                variable.AddCommand<NodeReadVariableMultiCommand>("multi")
                    .WithDescription("Reads a list of node variables.")
                    .WithExample($"node read variable multi -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar.Float\" -n \"ns=2;s=Demo.Dynamic.Scalar.Int32\"");
            });

            read.AddBranch("datatype", datatype =>
            {
                datatype.SetDescription("Reads one or more DataType definition(s) (enumerations).");
                datatype.AddCommand<NodeReadDataTypeSingleCommand>("single")
                    .WithDescription("Reads a single enum DataType definition.")
                    .WithExample($"node read datatype single -s {SampleOpcUaServerUrl} -n \"i=852\"");

                datatype.AddCommand<NodeReadDataTypeMultiCommand>("multi")
                    .WithDescription("Reads a list of enum DataType definitions.")
                    .WithExample($"node read datatype multi -s {SampleOpcUaServerUrl} -n \"i=852\" -n \"ns=3;i=3063\"");
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
                    .WithExample($"node write variable single -s {SampleOpcUaServerUrl} -n \"ns=2;s=Demo.Dynamic.Scalar.Float\" -d float --value \"100.5\"");
            });
        });
}