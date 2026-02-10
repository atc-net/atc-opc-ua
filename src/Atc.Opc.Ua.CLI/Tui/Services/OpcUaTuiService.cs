namespace Atc.Opc.Ua.CLI.Tui.Services;

/// <summary>
/// Thin adapter between the core OPC UA library and the TUI views.
/// Wraps <see cref="IOpcUaClient"/> and <see cref="IOpcUaNodeBrowser"/>,
/// marshals events to the UI thread, and manages TUI-specific state.
/// </summary>
public sealed class OpcUaTuiService : IDisposable
{
    private readonly IApplication app;
    private readonly IOpcUaClient client;
    private readonly IOpcUaNodeBrowser nodeBrowser;
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

    public bool IsConnected => client.IsConnected();

    public Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string? userName,
        string? password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(password))
        {
            return client.ConnectAsync(serverUri, cancellationToken);
        }

        return client.ConnectAsync(serverUri, userName, password, cancellationToken);
    }

    public Task<(bool Succeeded, string? ErrorMessage)> DisconnectAsync(
        CancellationToken cancellationToken)
        => client.DisconnectAsync(cancellationToken);

    public Task<(bool Succeeded, IList<NodeBrowseResult>? Children, string? ErrorMessage)> BrowseChildrenAsync(
        string parentNodeId,
        CancellationToken cancellationToken)
        => nodeBrowser.BrowseChildrenAsync(client, parentNodeId, cancellationToken);

    public Task<(bool Succeeded, NodeAttributeSet? Attributes, string? ErrorMessage)> ReadNodeAttributesAsync(
        string nodeId,
        CancellationToken cancellationToken)
        => nodeBrowser.ReadNodeAttributesAsync(client, nodeId, cancellationToken);

    public Task<(bool Succeeded, string? ErrorMessage)> CreateSubscriptionAsync(
        SubscriptionOptions? options,
        CancellationToken cancellationToken)
        => client.CreateSubscriptionAsync(options, cancellationToken);

    public Task<(bool Succeeded, string? ErrorMessage)> RemoveSubscriptionAsync(
        CancellationToken cancellationToken)
        => client.RemoveSubscriptionAsync(cancellationToken);

    public Task<(bool Succeeded, uint MonitoredItemHandle, string? ErrorMessage)> SubscribeToNodeAsync(
        string nodeId,
        string? displayName,
        CancellationToken cancellationToken)
        => client.SubscribeToNodeAsync(nodeId, displayName, cancellationToken);

    public Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeFromNodeAsync(
        uint handle,
        CancellationToken cancellationToken)
        => client.UnsubscribeFromNodeAsync(handle, cancellationToken);

    public Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeAllAsync(
        CancellationToken cancellationToken)
        => client.UnsubscribeAllAsync(cancellationToken);

    public Task<(bool Succeeded, string? ErrorMessage)> WriteNodeAsync(
        string nodeId,
        object value,
        CancellationToken cancellationToken)
        => client.WriteNodeAsync(nodeId, value, cancellationToken);

    private void OnNodeValueChanged(object? sender, MonitoredNodeValue e)
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
