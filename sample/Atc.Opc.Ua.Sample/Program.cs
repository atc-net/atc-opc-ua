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
        await client.ConnectAsync(ServerUri, UserName!, Password!);

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));
        while (await timer.WaitForNextTickAsync(cts.Token))
        {
            await EnsureConnectedAsync(client);
            var (succeeded, nodeVariables, errorMessage) = await client.ReadNodeVariablesAsync(NodeIds, true);

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

    private static async ValueTask<bool> EnsureConnectedAsync(OpcUaClient client)
    {
        if (client.IsConnected())
        {
            return true;
        }

        (bool IsConnected, string? Message) result = await client.ConnectAsync(ServerUri, UserName!, Password!);
        if (!result.IsConnected)
        {
            logger!.LogError("Failed to connect to OPC server: {Message}", result.Message);
        }

        return result.IsConnected;
    }
}