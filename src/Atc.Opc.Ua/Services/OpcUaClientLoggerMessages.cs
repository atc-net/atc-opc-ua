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
        Message = "Session is connecting.")]
    private partial void LogSessionConnecting();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionConnected,
        Level = LogLevel.Information,
        Message = "Session with name '{sessionName}' is connected.")]
    private partial void LogSessionConnected(string sessionName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionConnectionFailure,
        Level = LogLevel.Error,
        Message = "Session connection failure: '{errorMessage}'.")]
    private partial void LogSessionConnectionFailure(string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionAlreadyConnected,
        Level = LogLevel.Warning,
        Message = "Session is already connected.")]
    private partial void LogSessionAlreadyConnected();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionNotConnected,
        Level = LogLevel.Warning,
        Message = "Session is not connected.")]
    private partial void LogSessionNotConnected();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionUntrustedCertificateAccepted,
        Level = LogLevel.Information,
        Message = "Untrusted Certificate accepted '{subjectName}'.")]
    private partial void LogSessionUntrustedCertificateAccepted(string subjectName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionDisconnecting,
        Level = LogLevel.Information,
        Message = "Session with name '{sessionName}' is disconnecting.")]
    private partial void LogSessionDisconnecting(string sessionName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionDisconnected,
        Level = LogLevel.Information,
        Message = "Session with name '{sessionName}' is disconnected.")]
    private partial void LogSessionDisconnected(string sessionName);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionDisconnectionFailure,
        Level = LogLevel.Error,
        Message = "Session disconnection failure: '{errorMessage}'.")]
    private partial void LogSessionDisconnectionFailure(string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionNodeNotFound,
        Level = LogLevel.Warning,
        Message = "Could not find node by nodeId '{nodeId}'.")]
    private partial void LogSessionNodeNotFound(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionParentNodeNotFound,
        Level = LogLevel.Trace,
        Message = "Could not find parent for node with nodeId '{nodeId}'.")]
    private partial void LogSessionParentNodeNotFound(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionLoadComplexTypeSystem,
        Level = LogLevel.Trace,
        Message = "Loading Complex Type System for node with nodeId '{nodeId}' and dataTypeId '{dataTypeId}'.")]
    private partial void LogLoadingComplexTypeSystem(string nodeId, string dataTypeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeObject,
        Level = LogLevel.Information,
        Message = "Reading node with nodeId '{nodeId}'.")]
    private partial void LogSessionReadNodeObject(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeObjectWithMaxDepth,
        Level = LogLevel.Information,
        Message = "Reading node with nodeId '{nodeId}' with max depth set to '{nodeObjectReadDepth}'.")]
    private partial void LogSessionReadNodeObjectWithMaxDepth(string nodeId, int nodeObjectReadDepth);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeObjectSucceeded,
        Level = LogLevel.Trace,
        Message = "Successfully read node object with nodeId '{nodeId}'.")]
    private partial void LogSessionReadNodeObjectSucceeded(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeVariableSucceeded,
        Level = LogLevel.Trace,
        Message = "Successfully read node variable with nodeId '{nodeId}'.")]
    private partial void LogSessionReadNodeVariableSucceeded(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionWriteNodeVariableFailure,
        Level = LogLevel.Error,
        Message = "Writing node variable(s) failed: {errorMessage}.")]
    private partial void LogSessionWriteNodeVariableFailure(string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeVariableValueFailure,
        Level = LogLevel.Error,
        Message = "Retrieving value for variable with nodeId '{nodeId}' failed: '{errorMessage}'.")]
    private partial void LogSessionReadNodeVariableValueFailure(string nodeId, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionNodeHasWrongClass,
        Level = LogLevel.Warning,
        Message = "Node with nodeId '{nodeId}' has wrong NodeClass '{actual}', expected '{expected}'.")]
    private partial void LogSessionNodeHasWrongClass(string nodeId, NodeClass actual, NodeClass expected);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeNotSupportedNodeClass,
        Level = LogLevel.Warning,
        Message = "Encountered not supported node class '{nodeClass}', when reading node with nodeId '{nodeId}'.")]
    private partial void LogSessionReadNodeNotSupportedNodeClass(string nodeId, NodeClass nodeClass);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SessionReadNodeFailure,
        Level = LogLevel.Error,
        Message = "Reading node with nodeId '{nodeId}' failed: '{errorMessage}'.")]
    private partial void LogSessionReadNodeFailure(string nodeId, string errorMessage);
}