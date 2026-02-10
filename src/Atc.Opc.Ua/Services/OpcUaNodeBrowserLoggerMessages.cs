namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaNodeBrowser LoggerMessages.
/// </summary>
public partial class OpcUaNodeBrowser
{
    [LoggerMessage(
        EventId = LoggingEventIdConstants.BrowseChildren,
        Level = LogLevel.Trace,
        Message = "Browsing children of node '{ParentNodeId}'")]
    private partial void LogBrowseChildren(string parentNodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BrowseChildrenSucceeded,
        Level = LogLevel.Debug,
        Message = "Browsed {Count} children of node '{ParentNodeId}'")]
    private partial void LogBrowseChildrenSucceeded(string parentNodeId, int count);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.BrowseChildrenFailure,
        Level = LogLevel.Error,
        Message = "Failed to browse children of node '{ParentNodeId}': '{ErrorMessage}'")]
    private partial void LogBrowseChildrenFailure(string parentNodeId, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ReadAttributes,
        Level = LogLevel.Trace,
        Message = "Reading attributes of node '{NodeId}'")]
    private partial void LogReadAttributes(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ReadAttributesSucceeded,
        Level = LogLevel.Debug,
        Message = "Read attributes of node '{NodeId}'")]
    private partial void LogReadAttributesSucceeded(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ReadAttributesFailure,
        Level = LogLevel.Error,
        Message = "Failed to read attributes of node '{NodeId}': '{ErrorMessage}'")]
    private partial void LogReadAttributesFailure(string nodeId, string errorMessage);
}
