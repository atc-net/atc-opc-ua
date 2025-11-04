namespace Atc.Opc.Ua.Services;

public interface IOpcUaClient : IDisposable
{
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
}