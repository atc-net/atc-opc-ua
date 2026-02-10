namespace Atc.Opc.Ua.Services;

public interface IOpcUaClient : IDisposable
{
    /// <summary>
    /// Gets the current OPC UA session, or <see langword="null"/> if not connected.
    /// </summary>
    ISession? Session { get; }

    /// <summary>
    /// Raised when a monitored node delivers a new value from the server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The event is raised on a thread of the OPC UA stack. Keep handlers short, and make them thread-safe; hand
    /// long work over to another thread, e.g. through a channel.
    /// </para>
    /// <para>
    /// <see cref="MonitoredNodeValue.Value"/> is a .NET value, e.g. a <see cref="float"/> for a Float node. Use
    /// <see cref="MonitoredNodeValue.TryGetValue{T}"/> to get it as a specific type, and
    /// <see cref="MonitoredNodeValue.ValueAsText"/> for text formatted with the invariant culture. Check
    /// <see cref="MonitoredNodeValue.IsGood"/> before using a value.
    /// </para>
    /// </remarks>
    event EventHandler<MonitoredNodeValue>? NodeValueChanged;

    /// <summary>
    /// Creates the client's subscription on the current session. A client has one subscription at a time; its
    /// publishing interval, sampling interval and queue size apply to every node it monitors.
    /// </summary>
    /// <remarks>
    /// The subscription survives a lost connection. When the client reconnects to the same session, nothing
    /// changes. When the server lost the session, e.g. after a restart, the OPC UA stack recreates the subscription
    /// on a new session, and the client keeps using it with the same handles. If the subscription couldn't be
    /// recreated, the client drops it and logs a warning, and <see cref="CreateSubscriptionAsync"/> can be called
    /// again.
    /// </remarks>
    /// <param name="options">Optional subscription configuration. Uses defaults when <see langword="null"/>.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    Task<(bool Succeeded, string? ErrorMessage)> CreateSubscriptionAsync(
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the active subscription and all monitored items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    Task<(bool Succeeded, string? ErrorMessage)> RemoveSubscriptionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors a node in the client's subscription. Its values arrive through <see cref="NodeValueChanged"/>, with
    /// the node's data type name and access level, which are read once here.
    /// </summary>
    /// <remarks>
    /// Fails when the server rejects the node, e.g. with <c>BadNodeIdUnknown</c> for a node that doesn't exist.
    /// Subscription calls may be made from several threads; they run one at a time.
    /// </remarks>
    /// <param name="nodeId">The node identifier to subscribe to.</param>
    /// <param name="displayName">Optional display name for the monitored item. Defaults to the node's own display name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A tuple indicating success, the monitored item handle, and an optional error message. The handle is unique
    /// for the client's lifetime, and stays valid when the subscription is recreated after a reconnect.
    /// </returns>
    Task<(bool Succeeded, uint MonitoredItemHandle, string? ErrorMessage)> SubscribeToNodeAsync(
        string nodeId,
        string? displayName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors several nodes in the client's subscription with three round trips to the server, however many nodes
    /// there are. Use it instead of calling <see cref="SubscribeToNodeAsync"/> in a loop.
    /// </summary>
    /// <remarks>
    /// Each node gets its own result: a node the server rejects, e.g. with <c>BadNodeIdUnknown</c>, isn't monitored,
    /// and the others are, so check each result. When the call itself fails, e.g. without an active subscription or
    /// when the connection drops, no node is monitored. Each monitored item's display name is the node's own display name.
    /// </remarks>
    /// <param name="nodeIds">The nodes to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A tuple indicating whether the call completed, a result per node in the order of <paramref name="nodeIds"/>
    /// (empty when the call failed), and an optional error message.
    /// </returns>
    Task<(bool Succeeded, IReadOnlyList<NodeSubscriptionResult> Results, string? ErrorMessage)> SubscribeToNodesAsync(
        IReadOnlyCollection<string> nodeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a monitored item by its handle.
    /// </summary>
    /// <param name="monitoredItemHandle">The handle of the monitored item to remove.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeFromNodeAsync(
        uint monitoredItemHandle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all monitored items from the active subscription.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeAllAsync(
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    bool IsConnected();

    Task<(bool Succeeded, string? ErrorMessage)> DisconnectAsync(
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        int nodeVariableReadDepth = 0,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        int nodeVariableReadDepth = 0,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, NodeObject? NodeObject, string? ErrorMessage)> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth = 1,
        int nodeVariableReadDepth = 0,
        IReadOnlyCollection<string>? includeObjectNodeIds = null,
        IReadOnlyCollection<string>? excludeObjectNodeIds = null,
        IReadOnlyCollection<string>? includeVariableNodeIds = null,
        IReadOnlyCollection<string>? excludeVariableNodeIds = null,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> WriteNodeAsync(
        string nodeId,
        object value,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, string? ErrorMessage)> WriteNodesAsync(
        IDictionary<string, object> nodesToWrite,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IList<MethodExecutionResult>? ExecutionResults, string? ErrorMessage)> ExecuteMethodAsync(
        string parentNodeId,
        string methodNodeId,
        List<MethodExecutionParameter> arguments,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, OpcUaEnumDataType? EnumDataType, string? ErrorMessage)> ReadEnumDataTypeAsync(
        string dataTypeNodeId,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, IList<OpcUaEnumDataType>? EnumDataTypes, string? ErrorMessage)> ReadEnumDataTypesAsync(
        string[] dataTypeNodeIds,
        CancellationToken cancellationToken = default);
}