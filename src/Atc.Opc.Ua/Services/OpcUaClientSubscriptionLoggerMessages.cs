namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient Subscription LoggerMessages.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
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
        EventId = LoggingEventIdConstants.SubscriptionCreateFailure,
        Level = LogLevel.Error,
        Message = "Failed to create subscription: '{ErrorMessage}'")]
    private partial void LogSubscriptionCreateFailure(string errorMessage);

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
        EventId = LoggingEventIdConstants.SubscriptionRemoveFailure,
        Level = LogLevel.Error,
        Message = "Failed to remove subscription: '{ErrorMessage}'")]
    private partial void LogSubscriptionRemoveFailure(string errorMessage);

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
        EventId = LoggingEventIdConstants.SubscriptionNodeSubscribeFailure,
        Level = LogLevel.Error,
        Message = "Failed to subscribe to node '{NodeId}': '{ErrorMessage}'")]
    private partial void LogSubscriptionNodeSubscribeFailure(string nodeId, string errorMessage);

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
        EventId = LoggingEventIdConstants.SubscriptionNodeUnsubscribeFailure,
        Level = LogLevel.Error,
        Message = "Failed to unsubscribe monitored item with handle {Handle}: '{ErrorMessage}'")]
    private partial void LogSubscriptionNodeUnsubscribeFailure(uint handle, string errorMessage);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionUnsubscribeAll,
        Level = LogLevel.Information,
        Message = "Unsubscribing all {Count} monitored items")]
    private partial void LogSubscriptionUnsubscribeAll(int count);

    [LoggerMessage(
        EventId = LoggingEventIdConstants.SubscriptionNotificationError,
        Level = LogLevel.Error,
        Message = "Error processing subscription notification: '{ErrorMessage}'")]
    private partial void LogSubscriptionNotificationError(string errorMessage);
}
