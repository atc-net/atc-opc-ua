namespace Atc.Opc.Ua.Services;

public interface IOpcUaClient
{
    Task<bool> ConnectAsync(
        Uri serverUri);

    Task<bool> ConnectAsync(
        Uri serverUri,
        string userName,
        string password);

    bool IsConnected();

    bool Disconnect();

    Task<NodeVariable?> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue);

    Task<IList<NodeVariable>?> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues);

    Task<NodeObject?> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth = 1);

    bool WriteNode(
        string nodeId,
        object value);

    bool WriteNodes(
        IDictionary<string, object> nodesToWrite);
}