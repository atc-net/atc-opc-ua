namespace Atc.Opc.Ua.Services;

public interface IOpcUaClient : IDisposable
{
    Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        CancellationToken cancellationToken);

    Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string userName,
        string password,
        CancellationToken cancellationToken);

    bool IsConnected();

    Task<(bool Succeeded, string? ErrorMessage)> DisconnectAsync(CancellationToken cancellationToken);

    Task<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        CancellationToken cancellationToken,
        int nodeVariableReadDepth = 0);

    Task<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        CancellationToken cancellationToken,
        int nodeVariableReadDepth = 0);

    Task<(bool Succeeded, NodeObject? NodeObject, string? ErrorMessage)> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        CancellationToken cancellationToken,
        int nodeObjectReadDepth = 1,
        int nodeVariableReadDepth = 0,
        IReadOnlyCollection<string>? includeObjectNodeIds = null,
        IReadOnlyCollection<string>? excludeObjectNodeIds = null,
        IReadOnlyCollection<string>? includeVariableNodeIds = null,
        IReadOnlyCollection<string>? excludeVariableNodeIds = null);

    Task<(bool Succeeded, string? ErrorMessage)> WriteNodeAsync(
        string nodeId,
        object value,
        CancellationToken cancellationToken);

    Task<(bool Succeeded, string? ErrorMessage)> WriteNodesAsync(
        IDictionary<string, object> nodesToWrite,
        CancellationToken cancellationToken);

    Task<(bool Succeeded, IList<MethodExecutionResult>? ExecutionResults, string? ErrorMessage)> ExecuteMethodAsync(
        string parentNodeId,
        string methodNodeId,
        List<MethodExecutionParameter> arguments,
        CancellationToken cancellationToken);
}