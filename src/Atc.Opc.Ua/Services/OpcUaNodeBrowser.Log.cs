namespace Atc.Opc.Ua.Services;

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
        EventId = LoggingEventIdConstants.ReadAttributes,
        Level = LogLevel.Trace,
        Message = "Reading attributes of node '{NodeId}'")]
    private partial void LogReadAttributes(string nodeId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.ReadAttributesSucceeded,
        Level = LogLevel.Debug,
        Message = "Read attributes of node '{NodeId}'")]
    private partial void LogReadAttributesSucceeded(string nodeId);
}