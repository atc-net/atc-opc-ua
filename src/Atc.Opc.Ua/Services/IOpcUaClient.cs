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
        string nodeId);

    Task<IList<NodeVariable>?> ReadNodeVariablesAsync(
        string[] nodeIds);

    Task<NodeObject?> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        int nodeObjectReadDepth = 1);

    bool WriteNode(
        string nodeId,
        object value);

    bool WriteNodes(
        IDictionary<string, object> nodesToWrite);
}