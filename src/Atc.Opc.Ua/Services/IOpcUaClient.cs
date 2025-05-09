namespace Atc.Opc.Ua.Services;

public interface IOpcUaClient
{
    Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri);

    Task<(bool Succeeded, string? ErrorMessage)> ConnectAsync(
        Uri serverUri,
        string userName,
        string password);

    bool IsConnected();

    (bool Succeeded, string? ErrorMessage) Disconnect();

    Task<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        int nodeVariableReadDepth = 0);

    Task<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        int nodeVariableReadDepth = 0);

    Task<(bool Succeeded, NodeObject? NodeObject, string? ErrorMessage)> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth = 1);

    (bool Succeeded, string? ErrorMessage) WriteNode(
        string nodeId,
        object value);

    (bool Succeeded, string? ErrorMessage) WriteNodes(
        IDictionary<string, object> nodesToWrite);

    (bool Succeeded, IList<MethodExecutionResult>? ExecutionResults, string? ErrorMessage) ExecuteMethod(
        string parentNodeId,
        string methodNodeId,
        List<MethodExecutionParameter> arguments);
}