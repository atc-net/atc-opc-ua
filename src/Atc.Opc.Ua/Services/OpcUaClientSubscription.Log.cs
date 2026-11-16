namespace Atc.Opc.Ua.Services;

public partial class OpcUaClient
{
    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionCreating,
        Level = LogLevel.Information,
        Message = "Creating subscription with publishing interval {PublishingIntervalMs}ms")]
    private partial void LogSubscriptionCreating(int publishingIntervalMs);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionCreated,
        Level = LogLevel.Information,
        Message = "Subscription created with id {SubscriptionId}")]
    private partial void LogSubscriptionCreated(uint subscriptionId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionRemoving,
        Level = LogLevel.Information,
        Message = "Removing subscription with id {SubscriptionId}")]
    private partial void LogSubscriptionRemoving(uint subscriptionId);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionRemoved,
        Level = LogLevel.Information,
        Message = "Subscription removed")]
    private partial void LogSubscriptionRemoved();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNodeSubscribing,
        Level = LogLevel.Trace,
        Message = "Subscribing to node '{NodeId}' with handle {Handle}")]
    private partial void LogSubscriptionNodeSubscribing(string nodeId, uint handle);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNodeSubscribed,
        Level = LogLevel.Information,
        Message = "Subscribed to node '{NodeId}' with handle {Handle}")]
    private partial void LogSubscriptionNodeSubscribed(string nodeId, uint handle);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNodeRejected,
        Level = LogLevel.Warning,
        Message = "The server rejected node '{NodeId}': '{ErrorMessage}'")]
    private partial void LogSubscriptionNodeRejected(
        string nodeId,
        string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNodeUnsubscribing,
        Level = LogLevel.Trace,
        Message = "Unsubscribing from node '{NodeId}' with handle {Handle}")]
    private partial void LogSubscriptionNodeUnsubscribing(string nodeId, uint handle);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNodeUnsubscribed,
        Level = LogLevel.Information,
        Message = "Unsubscribed monitored item with handle {Handle}")]
    private partial void LogSubscriptionNodeUnsubscribed(uint handle);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionUnsubscribeAll,
        Level = LogLevel.Information,
        Message = "Unsubscribing all {Count} monitored items")]
    private partial void LogSubscriptionUnsubscribeAll(int count);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionUnsubscribedAll,
        Level = LogLevel.Information,
        Message = "Unsubscribed all {Count} monitored items")]
    private partial void LogSubscriptionUnsubscribedAll(int count);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionRestoredAfterReconnect,
        Level = LogLevel.Information,
        Message = "Subscription restored on the reconnected session with id {SubscriptionId} and {Count} monitored items")]
    private partial void LogSubscriptionRestoredAfterReconnect(uint subscriptionId, int count);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionLostAfterReconnect,
        Level = LogLevel.Warning,
        Message = "The subscription wasn't recreated on the reconnected session; create it again to receive values")]
    private partial void LogSubscriptionLostAfterReconnect();

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNodesSubscribed,
        Level = LogLevel.Information,
        Message = "Subscribed to {Subscribed} of {Total} nodes")]
    private partial void LogSubscriptionNodesSubscribed(int subscribed, int total);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNotificationError,
        Level = LogLevel.Error,
        Message = "Error processing subscription notification: '{ErrorMessage}'")]
    private partial void LogSubscriptionNotificationError(string errorMessage);
}