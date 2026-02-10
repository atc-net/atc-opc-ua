namespace Atc.Opc.Ua.CLI.Tui.Services;

/// <summary>
/// Thin adapter between the core OPC UA library and the TUI views.
/// Wraps <see cref="IOpcUaClient"/> and <see cref="IOpcUaNodeBrowser"/>,
/// marshals events to the UI thread, and manages TUI-specific state.
/// </summary>
/// <remarks>
/// The library throws when a call fails; the views show the failure instead. So each call returns whether it
/// succeeded, and the exception's message when it didn't.
/// </remarks>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "The TUI shows a failed call to the user instead of crashing.")]
public sealed class OpcUaTuiService : IDisposable
{
    private readonly IApplication app;
    private readonly IOpcUaClient client;
    private readonly IOpcUaNodeBrowser nodeBrowser;
    private bool hasSubscription;
    private bool disposed;

    public OpcUaTuiService(
        IApplication app,
        IOpcUaClient client,
        IOpcUaNodeBrowser nodeBrowser)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(nodeBrowser);

        this.app = app;
        this.client = client;
        this.nodeBrowser = nodeBrowser;

        client.NodeValueChanged += OnNodeValueChanged;
    }

    /// <summary>
    /// Raised on the UI thread when a monitored node value changes.
    /// </summary>
    public event EventHandler<MonitoredNodeValue>? NodeValueChanged;

    /// <summary>
    /// Whether the client has a session. It stays <see langword="true"/> while the connection is down.
    /// </summary>
    public bool IsConnected => client.IsConnected();

    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string? userName,
        string? password,
        CancellationToken cancellationToken)
    {
        // A new session starts without a subscription.
        hasSubscription = false;

        return RunAsync(() => string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password)
            ? client.ConnectAsync(serverUri, cancellationToken)
            : client.ConnectAsync(serverUri, userName, password, cancellationToken));
    }

    public Task<(bool Succeeded, string? ErrorMessage)> DisconnectAsync(
        CancellationToken cancellationToken)
    {
        hasSubscription = false;

        return RunAsync(() => client.DisconnectAsync(cancellationToken));
    }

    public async Task<(bool Succeeded, IList<NodeBrowseResult>? Children, string? ErrorMessage)> BrowseChildrenAsync(
        string parentNodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            return (true, await nodeBrowser.BrowseChildrenAsync(client, parentNodeId, cancellationToken), null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    public async Task<(bool Succeeded, NodeAttributeSet? Attributes, string? ErrorMessage)> ReadNodeAttributesAsync(
        string nodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var attributes = await nodeBrowser.ReadNodeAttributesAsync(client, nodeId, cancellationToken);

            return attributes is null
                ? (false, null, $"Node '{nodeId}' doesn't exist.")
                : (true, attributes, null);
        }
        catch (Exception ex)
        {
            return (false, null, ex.Message);
        }
    }

    /// <summary>
    /// Creates the subscription, unless this service already did since connecting. The views call it before every
    /// subscribe.
    /// </summary>
    /// <param name="options">The subscription's options, or <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Whether there is a subscription, and why not.</returns>
    public async Task<(bool Succeeded, string? ErrorMessage)> CreateSubscriptionAsync(
        SubscriptionOptions? options,
        CancellationToken cancellationToken)
    {
        if (hasSubscription)
        {
            return (true, null);
        }

        var result = await RunAsync(() => client.CreateSubscriptionAsync(options, cancellationToken));
        hasSubscription = result.Succeeded;

        return result;
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> RemoveSubscriptionAsync(
        CancellationToken cancellationToken)
    {
        var result = await RunAsync(async () => _ = await client.RemoveSubscriptionAsync(cancellationToken));
        if (result.Succeeded)
        {
            hasSubscription = false;
        }

        return result;
    }

    public async Task<(bool Succeeded, uint MonitoredItemHandle, string? ErrorMessage)> SubscribeToNodeAsync(
        string nodeId,
        string? displayName,
        CancellationToken cancellationToken)
    {
        try
        {
            return (true, await client.SubscribeToNodeAsync(nodeId, displayName, cancellationToken), null);
        }
        catch (InvalidOperationException ex)
        {
            // E.g. the client dropped the subscription after a reconnect: create it again on the next subscribe.
            hasSubscription = false;
            return (false, 0, ex.Message);
        }
        catch (Exception ex)
        {
            return (false, 0, ex.Message);
        }
    }

    public async Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeFromNodeAsync(
        uint handle,
        CancellationToken cancellationToken)
    {
        try
        {
            return await client.UnsubscribeFromNodeAsync(handle, cancellationToken)
                ? (true, null)
                : (false, $"No monitored item with handle {handle}.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeAllAsync(
        CancellationToken cancellationToken)
        => RunAsync(() => client.UnsubscribeAllAsync(cancellationToken));

    public Task<(bool Succeeded, string? ErrorMessage)> WriteNodeAsync(
        string nodeId,
        object value,
        CancellationToken cancellationToken)
        => RunAsync(() => client.WriteNodeAsync(nodeId, value, cancellationToken));

    /// <summary>
    /// Runs a call, and returns whether it succeeded, with the exception's message when it didn't.
    /// </summary>
    /// <param name="call">The call.</param>
    /// <returns>Whether the call succeeded, and why not.</returns>
    private static async Task<(bool Succeeded, string? ErrorMessage)> RunAsync(
        Func<Task> call)
    {
        try
        {
            await call();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void OnNodeValueChanged(
        object? sender,
        MonitoredNodeValue e)
    {
        // Marshal to UI thread via instance-based invoke
        app.Invoke(() => NodeValueChanged?.Invoke(this, e));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        client.NodeValueChanged -= OnNodeValueChanged;
        disposed = true;
    }
}