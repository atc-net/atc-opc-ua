namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient LoggerMessages.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    private readonly ILogger<OpcUaClient> logger;

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionConnecting,
        Level = LogLevel.Information,
        Message = "Session is connecting to '{OpcUaUri}'")]
    private partial void LogSessionConnecting(string opcUaUri);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionConnected,
        Level = LogLevel.Information,
        Message = "Session with name '{SessionName}' is connected")]
    private partial void LogSessionConnected(string sessionName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionConnectionFailure,
        Level = LogLevel.Error,
        Message = "Session failed to connect to '{OpcUaUri}': '{ErrorMessage}'")]
    private partial void LogSessionConnectionFailure(string opcUaUri, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReconnected,
        Level = LogLevel.Trace,
        Message = "Session with name '{SessionName}' was reconnected")]
    private partial void LogSessionReconnected(string sessionName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReconnectFailure,
        Level = LogLevel.Error,
        Message = "Session failed to reconnect")]
    private partial void LogSessionReconnectFailure(Exception ex);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionAlreadyConnected,
        Level = LogLevel.Warning,
        Message = "Session is already connected")]
    private partial void LogSessionAlreadyConnected();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionNotConnected,
        Level = LogLevel.Warning,
        Message = "Session is not connected")]
    private partial void LogSessionNotConnected();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionUntrustedCertificateAccepted,
        Level = LogLevel.Information,
        Message = "Untrusted Certificate accepted '{SubjectName}'")]
    private partial void LogSessionUntrustedCertificateAccepted(string subjectName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionDisconnecting,
        Level = LogLevel.Information,
        Message = "Session with name '{SessionName}' is disconnecting")]
    private partial void LogSessionDisconnecting(string sessionName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionDisconnected,
        Level = LogLevel.Information,
        Message = "Session with name '{SessionName}' is disconnected")]
    private partial void LogSessionDisconnected(string sessionName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionDisconnectionFailure,
        Level = LogLevel.Error,
        Message = "Session disconnection failure: '{ErrorMessage}'")]
    private partial void LogSessionDisconnectionFailure(string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionNodeNotFound,
        Level = LogLevel.Warning,
        Message = "Could not find node by nodeId '{NodeId}'")]
    private partial void LogSessionNodeNotFound(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionParentNodeNotFound,
        Level = LogLevel.Trace,
        Message = "Could not find parent for node with nodeId '{NodeId}'")]
    private partial void LogSessionParentNodeNotFound(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionLoadComplexTypeSystem,
        Level = LogLevel.Trace,
        Message = "Loading Complex Type System for node with nodeId '{NodeId}' and dataTypeId '{DataTypeId}'")]
    private partial void LogLoadingComplexTypeSystem(string nodeId, string dataTypeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeObject,
        Level = LogLevel.Information,
        Message = "Reading node object with nodeId '{NodeId}'")]
    private partial void LogSessionReadNodeObject(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionHandlingNode,
        Level = LogLevel.Information,
        Message = "Handling node with nodeId '{NodeId}'")]
    private partial void LogSessionHandlingNode(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeVariable,
        Level = LogLevel.Information,
        Message = "Reading node variable with nodeId '{NodeId}'")]
    private partial void LogSessionReadNodeVariable(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeObjectWithMaxDepth,
        Level = LogLevel.Information,
        Message =
            "Starting to read node tree from node with nodeId '{NodeId}' with max depth set to '{NodeObjectReadDepth}'")]
    private partial void LogSessionReadNodeObjectWithMaxDepth(string nodeId, int nodeObjectReadDepth);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeObjectSucceeded,
        Level = LogLevel.Trace,
        Message = "Successfully read node object with nodeId '{NodeId}'")]
    private partial void LogSessionReadNodeObjectSucceeded(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeVariableSucceeded,
        Level = LogLevel.Trace,
        Message = "Successfully read node variable with nodeId '{NodeId}'")]
    private partial void LogSessionReadNodeVariableSucceeded(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionWriteNodeVariableFailure,
        Level = LogLevel.Error,
        Message = "Writing node variable(s) failed: {ErrorMessage}")]
    private partial void LogSessionWriteNodeVariableFailure(string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeVariableValueEmpty,
        Level = LogLevel.Warning,
        Message = "Could not retrieve value for variable with nodeId '{NodeId}' and statusCode '{StatusCode}'")]
    private partial void LogSessionReadNodeVariableValueEmpty(string nodeId, string statusCode);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionNodeHasWrongClass,
        Level = LogLevel.Warning,
        Message = "Node with nodeId '{NodeId}' has wrong NodeClass '{Actual}', expected '{Expected}'")]
    private partial void LogSessionNodeHasWrongClass(string nodeId, NodeClass actual, NodeClass expected);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeNotSupportedNodeClass,
        Level = LogLevel.Warning,
        Message = "Encountered not supported node class '{NodeClass}', when reading node with nodeId '{NodeId}'")]
    private partial void LogSessionReadNodeNotSupportedNodeClass(string nodeId, NodeClass nodeClass);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeFailure,
        Level = LogLevel.Error,
        Message = "Reading node with nodeId '{NodeId}' failed: '{ErrorMessage}'")]
    private partial void LogSessionReadNodeFailure(string nodeId, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadParentNodeFailure,
        Level = LogLevel.Warning,
        Message = "Reading parent node of nodeId '{NodeId}' failed: '{ErrorMessage}'")]
    private partial void LogSessionReadParentNodeFailure(string nodeId, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionExecuteCommandRequest,
        Level = LogLevel.Trace,
        Message =
            "Executing method for parentNodeId '{ParentNodeId}' and methodNodeId '{MethodNodeId}' with '{Arguments}'")]
    private partial void LogSessionExecuteCommandRequest(string parentNodeId, string methodNodeId, string arguments);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionExecuteCommandFailure,
        Level = LogLevel.Error,
        Message =
            "Executing method for parentNodeId '{ParentNodeId}' and methodNodeId '{MethodNodeId}' failed: '{ErrorMessage}'")]
    private partial void LogSessionExecuteCommandFailure(string parentNodeId, string methodNodeId, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionKeepAliveRequestFailure,
        Level = LogLevel.Error,
        Message = "KeepAlive request failed")]
    private partial void LogSessionKeepAliveRequestFailure(Exception ex);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionKeepAliveFailureCountReset,
        Level = LogLevel.Trace,
        Message = "KeepAlive failure count reset")]
    private partial void LogSessionKeepAliveFailureCountReset();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionKeepAliveFailure,
        Level = LogLevel.Trace,
        Message = "KeepAlive request failed: '{ServiceResult}', consecutive failures: '{ConsecutiveKeepAliveFailures}'")]
    private partial void LogSessionKeepAliveFailure(
        string serviceResult,
        int consecutiveKeepAliveFailures);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadEnumDataType,
        Level = LogLevel.Information,
        Message = "Reading enum DataType with nodeId '{NodeId}'")]
    private partial void LogSessionReadEnumDataType(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadEnumDataTypeSucceeded,
        Level = LogLevel.Trace,
        Message = "Successfully read enum DataType with nodeId '{NodeId}' containing {MemberCount} members")]
    private partial void LogSessionReadEnumDataTypeSucceeded(string nodeId, int memberCount);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadEnumDataTypeFailure,
        Level = LogLevel.Error,
        Message = "Reading enum DataType with nodeId '{NodeId}' failed: '{ErrorMessage}'")]
    private partial void LogSessionReadEnumDataTypeFailure(string nodeId, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadEnumDataTypeNotEnum,
        Level = LogLevel.Warning,
        Message = "DataType with nodeId '{NodeId}' is not an enumeration or has no enum definition")]
    private partial void LogSessionReadEnumDataTypeNotEnum(string nodeId);
}