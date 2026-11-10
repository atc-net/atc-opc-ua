namespace Atc.Opc.Ua.Reconnect.Sample;

/// <summary>
/// Demonstrates that a subscription survives a lost connection: the server freezing, after which the client
/// reconnects to the same session, and the server restarting, after which the OPC UA stack recreates the subscription
/// on a new session and the client keeps using it with the same handles.
/// </summary>
/// <remarks>
/// <para>
/// Start Microsoft's OPC PLC simulator in Docker first:
/// <c>docker run -d --name opcplc -p 50000:50000 mcr.microsoft.com/iotedge/opc-plc:2.15.11 --pn=50000 --autoaccept --ut --ph=localhost</c>
/// </para>
/// <para>
/// Without arguments, the sample monitors two nodes until Ctrl+C and logs how many values arrived each second. Run
/// <c>docker pause opcplc</c>, <c>docker unpause opcplc</c> or <c>docker restart opcplc</c> in another terminal and
/// watch the client reconnect and the values come back.
/// </para>
/// <para>
/// With <c>--docker</c>, the sample pauses and restarts the container itself, checks that the values come back,
/// that unsubscribing and removing the subscription still work afterwards, and exits with 0 when every check passed.
/// </para>
/// </remarks>
public static class Program
{
    private const string ContainerName = "opcplc";
    private static readonly Uri ServerUri = new("opc.tcp://localhost:50000");
    private static readonly TimeSpan RecoveryTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PauseDuration = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan SilenceDuration = TimeSpan.FromSeconds(3);

    private static readonly (string NodeId, string DisplayName)[] Nodes =
    [
        ("i=2258", "CurrentTime"),
        ("ns=3;s=StepUp", "StepUp"),
    ];

    private static readonly ConcurrentDictionary<uint, int> ValueCounts = new();
    private static readonly Dictionary<uint, string> DisplayNames = [];
    private static readonly List<(string Check, bool Passed)> Checks = [];

    private static ILogger? logger;

    public static async Task<int> Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Information)
                .AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                }))
            .BuildServiceProvider();

        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        logger = loggerFactory.CreateLogger("Atc.Opc.Ua.Reconnect.Sample");

        using var cts = new CancellationTokenSource();

        // Handle Ctrl+C for graceful shutdown
        System.Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        // Short keep-alive settings, so a lost connection is noticed within seconds.
        using var client = new OpcUaClient(
            loggerFactory.CreateLogger<OpcUaClient>(),
            new OptionsWrapper<OpcUaSecurityOptions>(new OpcUaSecurityOptions()),
            new OptionsWrapper<OpcUaClientOptions>(new OpcUaClientOptions()),
            new OptionsWrapper<OpcUaClientKeepAliveOptions>(new OpcUaClientKeepAliveOptions
            {
                IntervalMilliseconds = 2000,
                MaxFailuresBeforeReconnect = 2,
                ReconnectPeriodMilliseconds = 2000,
            }));

        // 1. Connect
        logger.LogInformation("=== Connecting to {ServerUri} ===", ServerUri);
        var (connected, connectError) = await client.ConnectAsync(ServerUri, cts.Token);
        if (!connected)
        {
            logger.LogError("Failed to connect: {Error}. Is the OPC PLC container running?", connectError);
            return 1;
        }

        // 2. Subscribe
        if (!await SubscribeAsync(client, cts.Token))
        {
            return 1;
        }

        // 3. Monitor, or run the checks
        try
        {
            if (args.Contains("--docker", StringComparer.OrdinalIgnoreCase))
            {
                await RunChecksAsync(client, cts.Token);
            }
            else
            {
                await MonitorAsync(cts.Token);
            }
        }
        catch (OperationCanceledException ex)
        {
            logger.LogInformation(ex, "Stopped by user");
        }

        // 4. Disconnect
        logger.LogInformation("=== Disconnecting ===");
        await client.DisconnectAsync(CancellationToken.None);

        return ReportChecks();
    }

    private static async Task<bool> SubscribeAsync(
        OpcUaClient client,
        CancellationToken cancellationToken)
    {
        var log = logger!;
        log.LogInformation("=== Creating Subscription ===");

        var (created, createError) = await client.CreateSubscriptionAsync(
            new SubscriptionOptions { PublishingIntervalMs = 500, SamplingIntervalMs = 500 },
            cancellationToken);

        if (!created)
        {
            log.LogError("Failed to create subscription: {Error}", createError);
            return false;
        }

        client.NodeValueChanged += (_, e) => ValueCounts.AddOrUpdate(e.MonitoredItemHandle, 1, (_, count) => count + 1);

        foreach (var (nodeId, displayName) in Nodes)
        {
            var (subscribed, handle, subscribeError) = await client.SubscribeToNodeAsync(nodeId, displayName, cancellationToken);
            if (!subscribed)
            {
                log.LogError("Failed to subscribe to {NodeId}: {Error}", nodeId, subscribeError);
                return false;
            }

            DisplayNames[handle] = displayName;
            log.LogInformation("Subscribed to {DisplayName} ({NodeId}) with handle {Handle}", displayName, nodeId, handle);
        }

        return true;
    }

    private static async Task MonitorAsync(CancellationToken cancellationToken)
    {
        var log = logger!;
        log.LogInformation(
            "=== Monitoring until Ctrl+C; try docker pause, unpause or restart of container '{Container}' ===",
            ContainerName);

        while (!cancellationToken.IsCancellationRequested)
        {
            var before = SnapshotCounts();
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            log.LogInformation("Values in the last second: {Values}", DescribeIncrease(before));
        }
    }

    private static async Task RunChecksAsync(
        OpcUaClient client,
        CancellationToken cancellationToken)
    {
        var log = logger!;
        var allHandles = DisplayNames.Keys.ToArray();

        log.LogInformation("=== Check 1: values arrive ===");
        Check("Values arrive", await WaitForValuesAsync(allHandles, cancellationToken));

        log.LogInformation("=== Check 2: the server freezes for {Seconds} seconds ===", PauseDuration.TotalSeconds);
        await DockerAsync($"pause {ContainerName}", cancellationToken);
        await Task.Delay(PauseDuration, cancellationToken);
        await DockerAsync($"unpause {ContainerName}", cancellationToken);
        Check("Values come back after the server froze", await WaitForValuesAsync(allHandles, cancellationToken));

        log.LogInformation("=== Check 3: the server restarts and loses the session ===");
        await DockerAsync($"restart {ContainerName}", cancellationToken);
        Check("Values come back after the server restarted", await WaitForValuesAsync(allHandles, cancellationToken));

        log.LogInformation("=== Check 4: unsubscribing a node after the restart ===");
        var (stepUpHandle, stepUpName) = DisplayNames.First(x => x.Value == "StepUp");
        var (unsubscribed, unsubscribeError) = await client.UnsubscribeFromNodeAsync(stepUpHandle, cancellationToken);
        Check($"Unsubscribing {stepUpName} succeeds ({unsubscribeError ?? "OK"})", unsubscribed);
        Check($"No more values from {stepUpName}", await ExpectNoValuesAsync([stepUpHandle], cancellationToken));

        log.LogInformation("=== Check 5: removing the subscription after the restart ===");
        var (removed, removeError) = await client.RemoveSubscriptionAsync(cancellationToken);
        Check($"Removing the subscription succeeds ({removeError ?? "OK"})", removed);
        Check("No more values at all", await ExpectNoValuesAsync(allHandles, cancellationToken));
    }

    private static async Task<bool> WaitForValuesAsync(
        uint[] handles,
        CancellationToken cancellationToken)
    {
        var before = SnapshotCounts();
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < RecoveryTimeout)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            if (handles.All(handle => ValueCounts.GetValueOrDefault(handle) > before.GetValueOrDefault(handle)))
            {
                logger!.LogInformation("Values arrived after {Seconds:0.0} seconds", stopwatch.Elapsed.TotalSeconds);
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> ExpectNoValuesAsync(
        uint[] handles,
        CancellationToken cancellationToken)
    {
        var before = SnapshotCounts();
        await Task.Delay(SilenceDuration, cancellationToken);
        return handles.All(handle => ValueCounts.GetValueOrDefault(handle) == before.GetValueOrDefault(handle));
    }

    private static Dictionary<uint, int> SnapshotCounts()
        => ValueCounts.ToDictionary(x => x.Key, x => x.Value);

    private static string DescribeIncrease(Dictionary<uint, int> before)
        => string.Join(
            ", ",
            DisplayNames.Select(x => $"{x.Value} +{ValueCounts.GetValueOrDefault(x.Key) - before.GetValueOrDefault(x.Key)}"));

    private static void Check(
        string description,
        bool passed)
    {
        Checks.Add((description, passed));
        if (passed)
        {
            logger!.LogInformation("PASS: {Check}", description);
        }
        else
        {
            logger!.LogError("FAIL: {Check}", description);
        }
    }

    private static int ReportChecks()
    {
        if (Checks.Count == 0)
        {
            return 0;
        }

        var failed = Checks.Count(x => !x.Passed);
        logger!.LogInformation("=== {Passed} of {Total} checks passed ===", Checks.Count - failed, Checks.Count);
        return failed == 0 ? 0 : 1;
    }

    private static async Task DockerAsync(
        string arguments,
        CancellationToken cancellationToken)
    {
#pragma warning disable S4036 // The sample runs docker from the PATH.
        using var process = Process.Start(new ProcessStartInfo("docker", arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
#pragma warning restore S4036

        if (process is null)
        {
            throw new InvalidOperationException("Docker could not be started.");
        }

        await process.WaitForExitAsync(cancellationToken);
        logger!.LogInformation("docker {Arguments}: exit code {ExitCode}", arguments, process.ExitCode);
    }
}