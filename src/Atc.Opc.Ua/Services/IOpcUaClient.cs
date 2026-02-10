namespace Atc.Opc.Ua.Services;

public interface IOpcUaClient : IDisposable
{
    /// <summary>
    /// Gets the current OPC UA session, or <see langword="null"/> if not connected.
    /// </summary>
    ISession? Session { get; }

    /// <summary>
    /// Raised when a monitored item delivers a new value from the server.
    /// </summary>
    event EventHandler<MonitoredNodeValue>? NodeValueChanged;

    /// <summary>
    /// Creates a subscription on the current session.
    /// </summary>
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
    /// Adds a monitored item for the specified node to the active subscription.
    /// </summary>
    /// <param name="nodeId">The node identifier to subscribe to.</param>
    /// <param name="displayName">Optional display name for the monitored item.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A tuple indicating success, the monitored item handle, and an optional error message.</returns>
    Task<(bool Succeeded, uint MonitoredItemHandle, string? ErrorMessage)> SubscribeToNodeAsync(
        string nodeId,
        string? displayName = null,
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

    Task<(bool Succeeded, string? ErrorMessage)> DisconnectAsync(CancellationToken cancellationToken = default);

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