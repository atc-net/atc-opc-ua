namespace Atc.Opc.Ua.Sample;

public static class Program
{
    private const string? UserName = "Administrator";
    private const string? Password = "KepserverPassword1";
    private static readonly Uri ServerUri = new("opc.tcp://127.0.0.1:49320");

    private static readonly string[] NodeIds = new[]
    {
        "ns=2;s=Simulation Examples.Functions.Ramp1",
        "ns=2;s=Simulation Examples.Functions.Ramp2",
    };

    // Enum DataType NodeIds to read definitions for
    private static readonly string[] EnumDataTypeNodeIds =
    [
        "ns=3;i=3063",    // SimaticOperatingState (custom enum using EnumValues)
        "i=852",          // ServerState (standard OPC UA enum using EnumStrings)
    ];

    private static ILogger<OpcUaClient>? logger;

    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole())
            .BuildServiceProvider();

        var loggingFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        logger = loggingFactory.CreateLogger<OpcUaClient>();

        using var cts = new CancellationTokenSource();

        using var client = new OpcUaClient(logger);
        await client.ConnectAsync(ServerUri, UserName!, Password!, cts.Token);

        // Demo: Read enum DataType definitions once
        await DemoReadEnumDataTypesAsync(client, cts.Token);

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
                logger.LogError("Failed to read node variables: {ErrorMessage}", errorMessage);
                continue;
            }

            if (nodeVariables is null)
            {
                continue;
            }

            foreach (var nodeVariable in nodeVariables)
            {
                logger.LogInformation("Node: {NodeId}, DisplayName: {DisplayName}, Value: {Value}", nodeVariable.NodeId, nodeVariable.DisplayName, nodeVariable.SampleValue);
            }
        }
    }

    private static async Task DemoReadEnumDataTypesAsync(
        OpcUaClient client,
        CancellationToken cancellationToken)
    {
        logger!.LogInformation("=== Reading Enum DataType Definitions ===");

        foreach (var nodeId in EnumDataTypeNodeIds)
        {
            var (succeeded, enumDataType, errorMessage) = await client.ReadEnumDataTypeAsync(nodeId, cancellationToken);
            if (succeeded && enumDataType is not null)
            {
                logger!.LogInformation(
                    "Enum: {Name} ({NodeId}) - {MemberCount} members, HasEnumValues: {HasEnumValues}",
                    enumDataType.Name,
                    enumDataType.NodeId,
                    enumDataType.Members.Count,
                    enumDataType.HasEnumValues);

                foreach (var member in enumDataType.Members.OrderBy(m => m.Value))
                {
                    logger!.LogInformation("  {Value} = {Name}", member.Value, member.Name);
                }
            }
            else
            {
                logger!.LogWarning("Failed to read enum DataType {NodeId}: {ErrorMessage}", nodeId, errorMessage);
            }
        }

        logger!.LogInformation("=== End Enum DataType Definitions ===\n");
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
            logger!.LogError("Failed to connect to OPC server: {Message}", result.Message);
        }

        return result.IsConnected;
    }
}