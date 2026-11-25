namespace Atc.Opc.Ua.Services;

/// <summary>
/// An OPC UA client: connects to a server, reads and writes nodes, calls methods and monitors nodes.
/// </summary>
/// <remarks>
/// <para>
/// A failed call throws the OPC UA stack's exception, usually a <see cref="ServiceResultException"/> whose
/// <see cref="ServiceResultException.StatusCode"/> is the server's status code, e.g. <c>BadUserAccessDenied</c>.
/// A cancelled call throws <see cref="OperationCanceledException"/>, with the stack's exception as
/// <see cref="Exception.InnerException"/>.
/// </para>
/// <para>
/// Calls that need a connection throw <see cref="InvalidOperationException"/> when the client isn't connected, and
/// a missing or blank node id throws <see cref="ArgumentException"/>. A read returns <see langword="null"/> only when
/// the node doesn't exist.
/// </para>
/// </remarks>
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
    /// <returns>A task that completes when the subscription exists on the server.</returns>
    /// <exception cref="InvalidOperationException">The client isn't connected, or already has a subscription.</exception>
    Task CreateSubscriptionAsync(
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the active subscription and all monitored items.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><see langword="true"/> when the subscription was removed; <see langword="false"/> when there was none.</returns>
    Task<bool> RemoveSubscriptionAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors a node in the client's subscription. Its values arrive through <see cref="NodeValueChanged"/>, with
    /// the node's data type name and access level, which are read once here.
    /// </summary>
    /// <remarks>
    /// Subscription calls may be made from several threads; they run one at a time.
    /// </remarks>
    /// <param name="nodeId">The node identifier to subscribe to.</param>
    /// <param name="displayName">Optional display name for the monitored item. Defaults to the node's own display name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// The monitored item's handle. It's unique for the client's lifetime, and stays valid when the subscription is
    /// recreated after a reconnect.
    /// </returns>
    /// <exception cref="InvalidOperationException">There is no subscription; call <see cref="CreateSubscriptionAsync"/> first.</exception>
    /// <exception cref="ServiceResultException">The server rejects the node, e.g. with <c>BadNodeIdUnknown</c> for a node that doesn't exist.</exception>
    Task<uint> SubscribeToNodeAsync(
        string nodeId,
        string? displayName = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors several nodes in the client's subscription with three round trips to the server, however many nodes
    /// there are. Use it instead of calling <see cref="SubscribeToNodeAsync"/> in a loop.
    /// </summary>
    /// <remarks>
    /// Each node gets its own result: a node the server rejects, e.g. with <c>BadNodeIdUnknown</c>, isn't monitored,
    /// and the others are, so check each result. When the call itself fails, e.g. when the connection drops, it
    /// throws, and no node is monitored. Each monitored item's display name is the node's own display name.
    /// </remarks>
    /// <param name="nodeIds">The nodes to subscribe to.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result per node, in the order of <paramref name="nodeIds"/>.</returns>
    /// <exception cref="InvalidOperationException">There is no subscription; call <see cref="CreateSubscriptionAsync"/> first.</exception>
    Task<IReadOnlyList<NodeSubscriptionResult>> SubscribeToNodesAsync(
        IReadOnlyCollection<string> nodeIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a monitored item by its handle.
    /// </summary>
    /// <param name="monitoredItemHandle">The handle of the monitored item to remove.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><see langword="true"/> when it was removed; <see langword="false"/> when there is no subscription or no item with the handle.</returns>
    Task<bool> UnsubscribeFromNodeAsync(
        uint monitoredItemHandle,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all monitored items from the active subscription. Does nothing when there is no subscription.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the items are removed.</returns>
    Task UnsubscribeAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a server anonymously. Does nothing, with a warning, when the client already has a session.
    /// That includes a session whose connection is down, which the client reconnects in the background; to start over
    /// instead, call <see cref="DisconnectAsync"/> first. When the session was closed, e.g. through <c>Session</c>, it's
    /// disposed first, and its subscription is dropped: create the subscription again after connecting.
    /// </summary>
    /// <param name="serverUri">The server, e.g. <c>opc.tcp://localhost:50000</c>.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the client is connected.</returns>
    Task ConnectAsync(
        Uri serverUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to a server with a username and password, or anonymously when either is empty. Does nothing, with a
    /// warning, when the client already has a session.
    /// That includes a session whose connection is down, which the client reconnects in the background; to start over
    /// instead, call <see cref="DisconnectAsync"/> first. When the session was closed, e.g. through <c>Session</c>, it's
    /// disposed first, and its subscription is dropped: create the subscription again after connecting.
    /// </summary>
    /// <param name="serverUri">The server, e.g. <c>opc.tcp://localhost:50000</c>.</param>
    /// <param name="userName">The username.</param>
    /// <param name="password">The password.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the client is connected.</returns>
    Task ConnectAsync(
        Uri serverUri,
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the client has a session with a server: one was created and hasn't been closed. It doesn't check that
    /// the server still answers. While the connection is down, it stays <see langword="true"/>, calls fail with the
    /// stack's status code, e.g. <c>BadConnectionClosed</c>, and the client reconnects in the background.
    /// </summary>
    /// <returns><see langword="true"/> when the client has a session.</returns>
    bool IsConnected();

    /// <summary>
    /// Disconnects from the server. Does nothing when the client isn't connected. The session is released even when
    /// closing it on the server fails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the session is closed.</returns>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a variable and, down to <paramref name="nodeVariableReadDepth"/>, its child variables. A child that
    /// doesn't exist or may not be read, e.g. without access, is skipped with a warning. Any other failure, e.g. a
    /// dropped connection, fails the read.
    /// </summary>
    /// <param name="nodeId">The variable, e.g. <c>ns=2;s=Demo.Dynamic.Scalar.Float</c>.</param>
    /// <param name="includeSampleValue">Whether to read the variables' values.</param>
    /// <param name="nodeVariableReadDepth">How deep to read child variables (0 = none).</param>
    /// <param name="skippedNodeIds">Optional: receives the nodes that were skipped.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The variable, or <see langword="null"/> when the node doesn't exist.</returns>
    /// <exception cref="ArgumentException">The node isn't a Variable, or <paramref name="nodeId"/> isn't a valid node id.</exception>
    Task<NodeVariable?> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        int nodeVariableReadDepth = 0,
        ICollection<string>? skippedNodeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads several variables, one at a time.
    /// </summary>
    /// <param name="nodeIds">The variables.</param>
    /// <param name="includeSampleValues">Whether to read the variables' values.</param>
    /// <param name="nodeVariableReadDepth">How deep to read child variables (0 = none).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The variables, in the order of <paramref name="nodeIds"/>; a node that doesn't exist is left out, with a warning.</returns>
    /// <exception cref="ArgumentException">A node isn't a Variable, or a node id is missing or invalid.</exception>
    Task<IList<NodeVariable>> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        int nodeVariableReadDepth = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an object and, down to the given depths, its child objects and variables. A child that doesn't exist or
    /// may not be read, e.g. without access, is skipped with a warning. Any other failure, e.g. a dropped connection,
    /// fails the read.
    /// </summary>
    /// <param name="nodeId">The object, e.g. <c>ns=2;s=Demo.Dynamic.Scalar</c>.</param>
    /// <param name="includeObjects">Whether to read child objects.</param>
    /// <param name="includeVariables">Whether to read child variables.</param>
    /// <param name="includeSampleValues">Whether to read the variables' values.</param>
    /// <param name="nodeObjectReadDepth">How deep to follow child objects.</param>
    /// <param name="nodeVariableReadDepth">How deep to read child variables (0 = only direct variables).</param>
    /// <param name="includeObjectNodeIds">Optional: only these child objects.</param>
    /// <param name="excludeObjectNodeIds">Optional: not these child objects.</param>
    /// <param name="includeVariableNodeIds">Optional: only these child variables.</param>
    /// <param name="excludeVariableNodeIds">Optional: not these child variables.</param>
    /// <param name="skippedNodeIds">
    /// Optional: receives the nodes that were skipped, because they couldn't be read or their children couldn't be
    /// browsed.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The object, or <see langword="null"/> when the node doesn't exist.</returns>
    /// <exception cref="ArgumentException">The node isn't an Object, or <paramref name="nodeId"/> isn't a valid node id.</exception>
    Task<NodeObject?> ReadNodeObjectAsync(
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
        ICollection<string>? skippedNodeIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a value to a variable.
    /// </summary>
    /// <param name="nodeId">The variable.</param>
    /// <param name="value">The value, of the variable's data type.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the server accepted the value.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="nodeId"/> isn't a valid node id, or the value's type has no OPC UA type, e.g. <see cref="decimal"/>.
    /// </exception>
    /// <exception cref="ServiceResultException">The server rejected the value, e.g. with <c>BadTypeMismatch</c>.</exception>
    Task WriteNodeAsync(
        string nodeId,
        object value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes values to several variables in one call.
    /// </summary>
    /// <param name="nodesToWrite">The variables and their values.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the server accepted every value.</returns>
    /// <exception cref="ArgumentException">
    /// A node id isn't valid, or a value's type has no OPC UA type, e.g. <see cref="decimal"/>.
    /// </exception>
    /// <exception cref="ServiceResultException">
    /// The server rejected a value. The message names every rejected node; the status code is the first one's.
    /// </exception>
    Task WriteNodesAsync(
        IDictionary<string, object> nodesToWrite,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a method.
    /// </summary>
    /// <param name="parentNodeId">The object the method belongs to.</param>
    /// <param name="methodNodeId">The method.</param>
    /// <param name="arguments">The method's input arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The method's output arguments.</returns>
    /// <exception cref="ArgumentException">
    /// A node id isn't valid, or an argument's value isn't valid for its data type or can't be sent as a method argument.
    /// </exception>
    /// <exception cref="ServiceResultException">The server rejected the call, e.g. with <c>BadArgumentsMissing</c>.</exception>
    Task<IList<MethodExecutionResult>> ExecuteMethodAsync(
        string parentNodeId,
        string methodNodeId,
        List<MethodExecutionParameter> arguments,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an enumeration DataType, with its members.
    /// </summary>
    /// <param name="dataTypeNodeId">The DataType, e.g. <c>i=852</c> for ServerState.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The enum DataType, or <see langword="null"/> when the node doesn't exist.</returns>
    /// <exception cref="ArgumentException">The node isn't a DataType, or isn't an enumeration.</exception>
    Task<OpcUaEnumDataType?> ReadEnumDataTypeAsync(
        string dataTypeNodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads several enumeration DataTypes, one at a time.
    /// </summary>
    /// <param name="dataTypeNodeIds">The DataTypes.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The enum DataTypes, in the order of <paramref name="dataTypeNodeIds"/>; a node that doesn't exist is left out, with a warning.</returns>
    /// <exception cref="ArgumentException">A node isn't a DataType or isn't an enumeration, or a node id is missing.</exception>
    Task<IList<OpcUaEnumDataType>> ReadEnumDataTypesAsync(
        string[] dataTypeNodeIds,
        CancellationToken cancellationToken = default);
}