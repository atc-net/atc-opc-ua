// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient - Subscription support.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OPC UA communication can fail with various exception types.")]
public partial class OpcUaClient
{
    private readonly Dictionary<uint, MonitoredItem> monitoredItemsByHandle = [];
    private Subscription? activeSubscription;
    private SubscriptionOptions activeSubscriptionOptions = new();
    private uint nextClientHandle;

    /// <inheritdoc/>
    public event EventHandler<MonitoredNodeValue>? NodeValueChanged;

    /// <inheritdoc/>
    public async Task<(bool Succeeded, string? ErrorMessage)> CreateSubscriptionAsync(
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (Session is null || !Session.Connected)
            {
                return (false, "Session is not connected.");
            }

            if (activeSubscription is not null)
            {
                return (false, "A subscription already exists. Remove it first.");
            }

            activeSubscriptionOptions = options ?? new SubscriptionOptions();

            LogSubscriptionCreating(activeSubscriptionOptions.PublishingIntervalMs);

            activeSubscription = new Subscription(Session.DefaultSubscription)
            {
                DisplayName = "AtcOpcUaSubscription",
                PublishingInterval = activeSubscriptionOptions.PublishingIntervalMs,
                PublishingEnabled = true,
                LifetimeCount = 0,
                MaxNotificationsPerPublish = 0,
                Priority = 0,
            };

            Session.AddSubscription(activeSubscription);
            await activeSubscription.CreateAsync(cancellationToken);

            LogSubscriptionCreated(activeSubscription.Id);

            return (true, null);
        }
        catch (Exception ex)
        {
            LogSubscriptionCreateFailure(ex.Message);
            return (false, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Succeeded, string? ErrorMessage)> RemoveSubscriptionAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (activeSubscription is null)
            {
                return (false, "No active subscription.");
            }

            LogSubscriptionRemoving(activeSubscription.Id);

            // Remove all monitored items first
            foreach (var item in monitoredItemsByHandle.Values)
            {
                item.Notification -= OnMonitoredItemNotification;
            }

            monitoredItemsByHandle.Clear();

            await activeSubscription.DeleteAsync(silent: true, cancellationToken);
            if (Session is not null)
            {
                await Session.RemoveSubscriptionAsync(activeSubscription, cancellationToken);
            }

            activeSubscription.Dispose();
            activeSubscription = null;

            LogSubscriptionRemoved();

            return (true, null);
        }
        catch (Exception ex)
        {
            LogSubscriptionRemoveFailure(ex.Message);
            return (false, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Succeeded, uint MonitoredItemHandle, string? ErrorMessage)> SubscribeToNodeAsync(
        string nodeId,
        string? displayName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (activeSubscription is null)
            {
                return (false, 0, "No active subscription. Call CreateSubscriptionAsync first.");
            }

            var handle = ++nextClientHandle;

            LogSubscriptionNodeSubscribing(nodeId, handle);

            var monitoredItem = new MonitoredItem
            {
                StartNodeId = nodeId,
                DisplayName = displayName ?? nodeId,
                SamplingInterval = activeSubscriptionOptions.SamplingIntervalMs,
                QueueSize = activeSubscriptionOptions.QueueSize,
                DiscardOldest = activeSubscriptionOptions.DiscardOldest,
                Handle = handle,
            };

            monitoredItem.Notification += OnMonitoredItemNotification;

            activeSubscription.AddItem(monitoredItem);
            await activeSubscription.ApplyChangesAsync(cancellationToken);

            monitoredItemsByHandle[handle] = monitoredItem;

            LogSubscriptionNodeSubscribed(nodeId, handle);

            return (true, handle, null);
        }
        catch (Exception ex)
        {
            LogSubscriptionNodeSubscribeFailure(nodeId, ex.Message);
            return (false, 0, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeFromNodeAsync(
        uint monitoredItemHandle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (activeSubscription is null)
            {
                return (false, "No active subscription.");
            }

            if (!monitoredItemsByHandle.TryGetValue(monitoredItemHandle, out var monitoredItem))
            {
                return (false, $"Monitored item with handle {monitoredItemHandle} not found.");
            }

            LogSubscriptionNodeUnsubscribing(monitoredItem.StartNodeId.ToString(), monitoredItemHandle);

            monitoredItem.Notification -= OnMonitoredItemNotification;
            activeSubscription.RemoveItem(monitoredItem);
            await activeSubscription.ApplyChangesAsync(cancellationToken);

            monitoredItemsByHandle.Remove(monitoredItemHandle);

            LogSubscriptionNodeUnsubscribed(monitoredItemHandle);

            return (true, null);
        }
        catch (Exception ex)
        {
            LogSubscriptionNodeUnsubscribeFailure(monitoredItemHandle, ex.Message);
            return (false, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeAllAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (activeSubscription is null)
            {
                return (false, "No active subscription.");
            }

            LogSubscriptionUnsubscribeAll(monitoredItemsByHandle.Count);

            foreach (var item in monitoredItemsByHandle.Values)
            {
                item.Notification -= OnMonitoredItemNotification;
            }

            activeSubscription.RemoveItems([.. monitoredItemsByHandle.Values]);
            await activeSubscription.ApplyChangesAsync(cancellationToken);

            monitoredItemsByHandle.Clear();

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private void OnMonitoredItemNotification(MonitoredItem monitoredItem, MonitoredItemNotificationEventArgs e)
    {
        try
        {
            if (e.NotificationValue is not MonitoredItemNotification notification)
            {
                return;
            }

            var handle = monitoredItem.Handle is uint h ? h : 0u;

            var monitoredNodeValue = new MonitoredNodeValue
            {
                MonitoredItemHandle = handle,
                NodeId = monitoredItem.StartNodeId?.ToString() ?? string.Empty,
                DisplayName = monitoredItem.DisplayName ?? string.Empty,
                Value = notification.Value?.WrappedValue.ToString(),
                ServerTimestamp = notification.Value?.ServerTimestamp,
                SourceTimestamp = notification.Value?.SourceTimestamp,
                StatusCode = notification.Value?.StatusCode.Code ?? 0,
            };

            NodeValueChanged?.Invoke(this, monitoredNodeValue);
        }
        catch (Exception ex)
        {
            LogSubscriptionNotificationError(ex.Message);
        }
    }

    /// <summary>
    /// Cleans up subscription resources during disconnect/dispose.
    /// Called from the main OpcUaClient disposal path.
    /// </summary>
    private void CleanupSubscription()
    {
        if (activeSubscription is not null)
        {
            foreach (var item in monitoredItemsByHandle.Values)
            {
                item.Notification -= OnMonitoredItemNotification;
            }

            monitoredItemsByHandle.Clear();

            try
            {
                activeSubscription.Dispose();
            }
            catch
            {
                // Ignore: best-effort dispose
            }

            activeSubscription = null;
        }

        nextClientHandle = 0;
    }
}
