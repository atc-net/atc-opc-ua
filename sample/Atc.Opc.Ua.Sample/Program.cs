namespace Atc.Opc.Ua.Sample;

public static class Program
{
    private const string? UserName = "Administrator";
    private const string? Password = "KepserverPassword1";
    private static readonly Uri ServerUri = new("opc.tcp://127.0.0.1:49320");

    private static readonly string[] NodeIds =
    [
        "ns=2;s=Simulation Examples.Functions.Ramp1",
        "ns=2;s=Simulation Examples.Functions.Ramp2"
    ];

    // Enum DataType NodeIds to read definitions for
    private static readonly string[] EnumDataTypeNodeIds =
    [
        "ns=3;i=3063",    // SimaticOperatingState (custom enum using EnumValues)
        "i=852",          // ServerState (standard OPC UA enum using EnumStrings)
    ];

    private static ILogger<OpcUaClient>? clientLogger;
    private static ILogger<OpcUaScanner>? scannerLogger;

    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole())
            .BuildServiceProvider();

        var loggingFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        clientLogger = loggingFactory.CreateLogger<OpcUaClient>();
        scannerLogger = loggingFactory.CreateLogger<OpcUaScanner>();

        using var cts = new CancellationTokenSource();

        using var client = new OpcUaClient(clientLogger);
        await client.ConnectAsync(ServerUri, UserName!, Password!, cts.Token);

        await DemoReadEnumDataTypesAsync(client, cts.Token);

        await DemoScanWithVariableFilterAsync(client, cts.Token);

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cts.Token))
        {
            await EnsureConnectedAsync(client, cts.Token);
            var (succeeded, nodeVariables, errorMessage) = await client.ReadNodeVariablesAsync(
                NodeIds,
                includeSampleValues: true,
                cancellationToken: cts.Token);

            if (!succeeded)
            {
                clientLogger.LogError("Failed to read node variables: {ErrorMessage}", errorMessage);
                continue;
            }

            if (nodeVariables is null)
            {
                continue;
            }

            foreach (var nodeVariable in nodeVariables)
            {
                clientLogger.LogInformation("Node: {NodeId}, DisplayName: {DisplayName}, Value: {Value}", nodeVariable.NodeId, nodeVariable.DisplayName, nodeVariable.SampleValue);
            }
        }
    }

    private static async Task DemoReadEnumDataTypesAsync(
        OpcUaClient client,
        CancellationToken cancellationToken)
    {
        clientLogger!.LogInformation("=== Reading Enum DataType Definitions ===");

        foreach (var nodeId in EnumDataTypeNodeIds)
        {
            var (succeeded, enumDataType, errorMessage) = await client.ReadEnumDataTypeAsync(nodeId, cancellationToken);
            if (succeeded && enumDataType is not null)
            {
                clientLogger!.LogInformation(
                    "Enum: {Name} ({NodeId}) - {MemberCount} members, HasEnumValues: {HasEnumValues}",
                    enumDataType.Name,
                    enumDataType.NodeId,
                    enumDataType.Members.Count,
                    enumDataType.HasEnumValues);

                foreach (var member in enumDataType.Members.OrderBy(x => x.Value))
                {
                    clientLogger!.LogInformation("  {Value} = {Name}", member.Value, member.Name);
                }
            }
            else
            {
                clientLogger!.LogWarning("Failed to read enum DataType {NodeId}: {ErrorMessage}", nodeId, errorMessage);
            }
        }

        clientLogger!.LogInformation("=== End Enum DataType Definitions ===\n");
    }

    private static async Task DemoScanWithVariableFilterAsync(
        OpcUaClient client,
        CancellationToken cancellationToken)
    {
        clientLogger!.LogInformation("=== Scanning with Variable Node ID Filter ===");

        var scanner = new OpcUaScanner(scannerLogger!);
        var options = new OpcUaScannerOptions
        {
            StartingNodeId = "i=85", // ObjectsFolder
            ObjectDepth = 10,
            VariableDepth = 10,
            IncludeSampleValues = true,
        };

        // Add the variable node IDs we want to include
        foreach (var nodeId in NodeIds)
        {
            options.IncludeVariableNodeIds.Add(nodeId);
        }

        clientLogger!.LogInformation(
            "Scanning from {StartingNodeId} with IncludeVariableNodeIds: [{NodeIds}]",
            options.StartingNodeId,
            string.Join(", ", options.IncludeVariableNodeIds));

        var startNew = Stopwatch.StartNew();

        var result = await scanner.ScanAsync(client, options, cancellationToken);

        startNew.Stop();

        System.Console.WriteLine(startNew.Elapsed.TotalMinutes);

        if (result.Succeeded && result.Root is not null)
        {
            clientLogger!.LogInformation("Scan succeeded!");
            PrintNode(result.Root, indent: 0);
        }
        else
        {
            clientLogger!.LogWarning("Scan failed: {ErrorMessage}", result.ErrorMessage);
        }

        clientLogger!.LogInformation("=== End Scan Demo ===\n");
    }

    private static void PrintNode(NodeBase node, int indent)
    {
        var prefix = new string(' ', indent * 2);

        if (node is NodeObject nodeObject)
        {
            clientLogger!.LogInformation("{Prefix}[Object] {DisplayName} ({NodeId})", prefix, nodeObject.DisplayName, nodeObject.NodeId);

            foreach (var childVar in nodeObject.NodeVariables)
            {
                PrintNode(childVar, indent + 1);
            }

            foreach (var childObj in nodeObject.NodeObjects)
            {
                PrintNode(childObj, indent + 1);
            }
        }
        else if (node is NodeVariable nodeVariable)
        {
            var typeInfo = nodeVariable.DataTypeDotnet;
            var opcUaType = nodeVariable.DataTypeOpcUa;

            clientLogger!.LogInformation(
                "{Prefix}[Variable] {DisplayName} = {Value} ({NodeId})",
                prefix,
                nodeVariable.DisplayName,
                nodeVariable.SampleValue,
                nodeVariable.NodeId);

            // Display OPC UA type information
            if (opcUaType is not null)
            {
                clientLogger!.LogInformation(
                    "{Prefix}  OPC UA: {Name} (Kind: {Kind}, NodeId: {NodeId})",
                    prefix,
                    opcUaType.Name,
                    opcUaType.Kind,
                    opcUaType.NodeId);
            }

            // Display .NET type information
            if (typeInfo is not null)
            {
                clientLogger!.LogInformation(
                    "{Prefix}  .NET:   {Name} (Kind: {Kind}, CLR: {ClrTypeName})",
                    prefix,
                    typeInfo.Name,
                    typeInfo.Kind,
                    typeInfo.ClrTypeName);

                // Display enum members if this is an enum type
                if (typeInfo is { Kind: DotNetTypeKind.Enum, EnumMembers.Count: > 0 })
                {
                    clientLogger!.LogInformation("{Prefix}  Enum Members:", prefix);
                    foreach (var member in typeInfo.EnumMembers.OrderBy(x => x.Value))
                    {
                        clientLogger!.LogInformation(
                            "{Prefix}    {Value} = {Name}",
                            prefix,
                            member.Value,
                            member.Name);
                    }
                }

                // For arrays, show element type
                if (typeInfo is { Kind: DotNetTypeKind.Array, ArrayElementType: not null })
                {
                    clientLogger!.LogInformation(
                        "{Prefix}  Element Type: {Name} (Kind: {Kind})",
                        prefix,
                        typeInfo.ArrayElementType.Name,
                        typeInfo.ArrayElementType.Kind);
                }
            }

            foreach (var childVar in nodeVariable.NodeVariables)
            {
                PrintNode(childVar, indent + 1);
            }
        }
    }

    private static async ValueTask<bool> EnsureConnectedAsync(
        OpcUaClient client,
        CancellationToken cancellationToken = default)
    {
        if (client.IsConnected())
        {
            return true;
        }

        (bool IsConnected, string? Message) result = await client.ConnectAsync(ServerUri, UserName!, Password!, cancellationToken);
        if (!result.IsConnected)
        {
            clientLogger!.LogError("Failed to connect to OPC server: {Message}", result.Message);
        }

        return result.IsConnected;
    }
}