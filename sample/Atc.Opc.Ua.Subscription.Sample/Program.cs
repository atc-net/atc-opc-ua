namespace Atc.Opc.Ua.Subscription.Sample;

/// <summary>
/// Demonstrates the OPC UA subscription/monitoring capabilities of the core library.
/// Connects to a server, creates a subscription, monitors node values, and cleans up.
/// </summary>
public static class Program
{
    private const string? UserName = "Administrator";
    private const string? Password = "KepserverPassword1";
    private static readonly Uri ServerUri = new("opc.tcp://127.0.0.1:49320");

    private static readonly string[] NodeIds =
    [
        "ns=2;s=Simulation Examples.Functions.Ramp1",
        "ns=2;s=Simulation Examples.Functions.Ramp2",
    ];

    private static ILogger<OpcUaClient>? clientLogger;
    private static ILogger<OpcUaNodeBrowser>? browserLogger;

    public static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging(loggingBuilder => loggingBuilder
                .SetMinimumLevel(LogLevel.Trace)
                .AddConsole())
            .BuildServiceProvider();

        var loggingFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        clientLogger = loggingFactory.CreateLogger<OpcUaClient>();
        browserLogger = loggingFactory.CreateLogger<OpcUaNodeBrowser>();

        using var cts = new CancellationTokenSource();

        // Handle Ctrl+C for graceful shutdown
        System.Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using var client = new OpcUaClient(clientLogger);

        // 1. Connect
        clientLogger.LogInformation("=== Connecting to {ServerUri} ===", ServerUri);
        var (connected, connectError) = await client.ConnectAsync(ServerUri, UserName!, Password!, cts.Token);
        if (!connected)
        {
            clientLogger.LogError("Failed to connect: {Error}", connectError);
            return;
        }

        clientLogger.LogInformation("Connected successfully");

        // 2. Browse root nodes to demonstrate IOpcUaNodeBrowser
        await DemoBrowseAsync(client, cts.Token);

        // 3. Create subscription and monitor
        await DemoSubscriptionAsync(client, cts.Token);

        // 4. Disconnect
        clientLogger.LogInformation("=== Disconnecting ===");
        await client.DisconnectAsync(cts.Token);
        clientLogger.LogInformation("Done.");
    }

    private static async Task DemoSubscriptionAsync(
        OpcUaClient client,
        CancellationToken cancellationToken)
    {
        var log = clientLogger!;
        log.LogInformation("=== Creating Subscription ===");
        var subscriptionOptions = new SubscriptionOptions
        {
            PublishingIntervalMs = 250,
            SamplingIntervalMs = 250,
        };

        var (subCreated, subError) = await client.CreateSubscriptionAsync(subscriptionOptions, cancellationToken);
        if (!subCreated)
        {
            log.LogError("Failed to create subscription: {Error}", subError);
            return;
        }

        // Subscribe to node value changes
        client.NodeValueChanged += OnNodeValueChanged;

        var handles = new List<uint>();
        foreach (var nodeId in NodeIds)
        {
            var (subscribed, handle, subscribeError) = await client.SubscribeToNodeAsync(nodeId, cancellationToken: cancellationToken);
            if (subscribed)
            {
                handles.Add(handle);
                log.LogInformation("Subscribed to {NodeId} with handle {Handle}", nodeId, handle);
            }
            else
            {
                log.LogWarning("Failed to subscribe to {NodeId}: {Error}", nodeId, subscribeError);
            }
        }

        // Monitor for 15 seconds
        log.LogInformation("=== Monitoring for 15 seconds (press Ctrl+C to stop early) ===");
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
        }
        catch (OperationCanceledException ex)
        {
            log.LogInformation(ex, "Monitoring interrupted by user");
        }

        // Unsubscribe from first node individually (if we have at least one)
        if (handles.Count > 0)
        {
            log.LogInformation("=== Unsubscribing first node (handle {Handle}) ===", handles[0]);
            var (unsubscribed, unsubError) = await client.UnsubscribeFromNodeAsync(handles[0], cancellationToken);
            if (unsubscribed)
            {
                log.LogInformation("Unsubscribed handle {Handle}", handles[0]);
            }
            else
            {
                log.LogWarning("Failed to unsubscribe handle {Handle}: {Error}", handles[0], unsubError);
            }
        }

        // Unsubscribe all remaining
        log.LogInformation("=== Unsubscribing all remaining ===");
        var (allUnsubscribed, allUnsubError) = await client.UnsubscribeAllAsync(cancellationToken);
        if (!allUnsubscribed)
        {
            log.LogWarning("Failed to unsubscribe all: {Error}", allUnsubError);
        }

        // Remove subscription
        log.LogInformation("=== Removing subscription ===");
        var (removed, removeError) = await client.RemoveSubscriptionAsync(cancellationToken);
        if (!removed)
        {
            log.LogWarning("Failed to remove subscription: {Error}", removeError);
        }
    }

    private static async Task DemoBrowseAsync(
        OpcUaClient client,
        CancellationToken cancellationToken)
    {
        var log = clientLogger!;
        log.LogInformation("=== Browsing Root Nodes ===");

        var browser = new OpcUaNodeBrowser(browserLogger!);

        // Browse the Objects folder (i=85)
        var (succeeded, children, errorMessage) = await browser.BrowseChildrenAsync(
            client,
            "i=85",
            cancellationToken);

        if (succeeded && children is not null)
        {
            log.LogInformation("Found {Count} top-level nodes:", children.Count);
            foreach (var child in children)
            {
                log.LogInformation(
                    "  [{NodeClass}] {DisplayName} ({NodeId}) HasChildren={HasChildren}",
                    child.NodeClass,
                    child.DisplayName,
                    child.NodeId,
                    child.HasChildren);
            }

            // Read attributes of the first node
            if (children.Count > 0)
            {
                await DemoReadAttributesAsync(browser, client, children[0], cancellationToken);
            }
        }
        else
        {
            log.LogWarning("Failed to browse root: {Error}", errorMessage);
        }

        log.LogInformation("=== End Browse Demo ===\n");
    }

    private static async Task DemoReadAttributesAsync(
        OpcUaNodeBrowser browser,
        OpcUaClient client,
        NodeBrowseResult node,
        CancellationToken cancellationToken)
    {
        var log = clientLogger!;
        log.LogInformation("=== Reading Attributes of '{DisplayName}' ===", node.DisplayName);

        var (attrSucceeded, attributes, attrError) = await browser.ReadNodeAttributesAsync(
            client,
            node.NodeId,
            cancellationToken);

        if (attrSucceeded && attributes is not null)
        {
            log.LogInformation("  NodeId: {NodeId}", attributes.NodeId);
            log.LogInformation("  DisplayName: {DisplayName}", attributes.DisplayName);
            log.LogInformation("  BrowseName: {BrowseName}", attributes.BrowseName);
            log.LogInformation("  NodeClass: {NodeClass}", attributes.NodeClass);
            log.LogInformation("  Description: {Description}", attributes.Description ?? "(none)");
        }
        else
        {
            log.LogWarning("Failed to read attributes: {Error}", attrError);
        }
    }

    private static void OnNodeValueChanged(object? sender, MonitoredNodeValue e)
    {
        var status = e.IsGood ? "Good" : $"0x{e.StatusCode:X8}";
        clientLogger!.LogInformation(
            "Value changed: {DisplayName} ({NodeId}) = {Value} [{Status}] at {Timestamp}",
            e.DisplayName,
            e.NodeId,
            e.Value ?? "(null)",
            status,
            e.ServerTimestamp?.ToString("O") ?? "(null)");
    }
}
