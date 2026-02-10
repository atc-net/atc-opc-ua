// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient - Subscription support.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OPC UA communication can fail with various exception types.")]
public partial class OpcUaClient
{
    private const string SubscriptionDisplayName = "AtcOpcUaSubscription";
    private const string NoActiveSubscriptionMessage = "No active subscription. Call CreateSubscriptionAsync first.";

    /// <summary>
    /// The upper 16 bits of an OPC UA status code identify it: its severity and its code. The lower 16 bits are info
    /// bits, e.g. Overflow when a monitored item's queue dropped values, which aren't part of the status code's name.
    /// </summary>
    private const uint StatusCodeIdentityMask = 0xFFFF0000;

    private readonly MonitoredNodeRegistry monitoredNodes = new();
    private readonly ConcurrentDictionary<NodeId, string?> dataTypeNames = new();
    private readonly SemaphoreSlim subscriptionGate = new(1, 1);
    private volatile Subscription? activeSubscription;
    private SubscriptionOptions activeSubscriptionOptions = new();

    /// <inheritdoc/>
    public event EventHandler<MonitoredNodeValue>? NodeValueChanged;

    /// <inheritdoc/>
    public async Task<(bool Succeeded, string? ErrorMessage)> CreateSubscriptionAsync(
        SubscriptionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await RunExclusiveAsync(
                async () =>
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

                    var subscription = new Subscription(Session.DefaultSubscription)
                    {
                        DisplayName = SubscriptionDisplayName,
                        PublishingInterval = activeSubscriptionOptions.PublishingIntervalMs,
                        PublishingEnabled = true,
                        LifetimeCount = 0,
                        MaxNotificationsPerPublish = 0,
                        Priority = 0,
                    };

                    Session.AddSubscription(subscription);
                    try
                    {
                        await subscription.CreateAsync(cancellationToken);
                    }
                    catch
                    {
                        // Not created on the server: take it out of the session again, so a reconnect doesn't recreate it.
                        await RemoveFromSessionAsync(Session, subscription);
                        throw;
                    }

                    // Published only once it exists on the server, so no other code sees a half-created subscription.
                    activeSubscription = subscription;

                    LogSubscriptionCreated(subscription.Id);

                    return (true, (string?)null);
                },
                cancellationToken);
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
            return await RunExclusiveAsync(
                async () =>
                {
                    var subscription = activeSubscription;
                    if (subscription is null)
                    {
                        return (false, "No active subscription.");
                    }

                    LogSubscriptionRemoving(subscription.Id);

                    DetachMonitoredNodes();

                    await subscription.DeleteAsync(silent: true, cancellationToken);
                    if (Session is not null)
                    {
                        await Session.RemoveSubscriptionAsync(subscription, cancellationToken);
                    }

                    subscription.Dispose();
                    activeSubscription = null;

                    LogSubscriptionRemoved();

                    return (true, (string?)null);
                },
                cancellationToken);
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
            var results = await RunExclusiveAsync(
                () => SubscribeNodesCoreAsync([(nodeId, displayName)], logEachNode: true, cancellationToken),
                cancellationToken);

            if (results is null)
            {
                return (false, 0, NoActiveSubscriptionMessage);
            }

            var result = results[0];
            return (result.Succeeded, result.MonitoredItemHandle, result.ErrorMessage);
        }
        catch (Exception ex)
        {
            LogSubscriptionNodeSubscribeFailure(nodeId, ex.Message);
            return (false, 0, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Succeeded, IReadOnlyList<NodeSubscriptionResult> Results, string? ErrorMessage)> SubscribeToNodesAsync(
        IReadOnlyCollection<string> nodeIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        try
        {
            var results = await RunExclusiveAsync(
                () => SubscribeNodesCoreAsync([.. nodeIds.Select(x => (x, (string?)null))], logEachNode: false, cancellationToken),
                cancellationToken);

            if (results is null)
            {
                return (false, [], NoActiveSubscriptionMessage);
            }

            LogSubscriptionNodesSubscribed(results.Count(x => x.Succeeded), results.Count);

            // The call completed; each result says whether its node is monitored.
            return (true, results, null);
        }
        catch (Exception ex)
        {
            LogSubscriptionNodesSubscribeFailure(nodeIds.Count, ex.Message);
            return (false, [], ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Succeeded, string? ErrorMessage)> UnsubscribeFromNodeAsync(
        uint monitoredItemHandle,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await RunExclusiveAsync(
                async () =>
                {
                    var subscription = activeSubscription;
                    if (subscription is null)
                    {
                        return (false, "No active subscription.");
                    }

                    if (!monitoredNodes.TryGet(monitoredItemHandle, out var node))
                    {
                        return (false, $"Monitored item with handle {monitoredItemHandle} not found.");
                    }

                    LogSubscriptionNodeUnsubscribing(node.Item.StartNodeId.ToString(), monitoredItemHandle);

                    node.Item.Notification -= OnMonitoredItemNotification;
                    subscription.RemoveItem(node.Item);
                    await subscription.ApplyChangesAsync(cancellationToken);

                    monitoredNodes.Remove(monitoredItemHandle);

                    LogSubscriptionNodeUnsubscribed(monitoredItemHandle);

                    return (true, (string?)null);
                },
                cancellationToken);
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
            return await RunExclusiveAsync(
                async () =>
                {
                    var subscription = activeSubscription;
                    if (subscription is null)
                    {
                        return (false, "No active subscription.");
                    }

                    var nodes = monitoredNodes.Nodes;

                    LogSubscriptionUnsubscribeAll(nodes.Count);

                    foreach (var node in nodes)
                    {
                        node.Item.Notification -= OnMonitoredItemNotification;
                    }

                    subscription.RemoveItems(nodes.Select(x => x.Item));
                    await subscription.ApplyChangesAsync(cancellationToken);

                    monitoredNodes.Clear();

                    LogSubscriptionUnsubscribedAll(nodes.Count);

                    return (true, (string?)null);
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            LogSubscriptionUnsubscribeAllFailure(ex.Message);
            return (false, ex.Message);
        }
    }

    /// <summary>
    /// Runs a subscription operation alone, so concurrent calls can't change the subscription at the same time.
    /// </summary>
    /// <typeparam name="TResult">The type of the operation's result.</typeparam>
    /// <param name="operation">The operation; it starts once no other subscription operation runs.</param>
    /// <param name="cancellationToken">Stops waiting for the other operations; the operation itself gets its own token.</param>
    /// <returns>The operation's result.</returns>
    private async Task<TResult> RunExclusiveAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        await subscriptionGate.WaitAsync(cancellationToken);

        try
        {
            return await operation();
        }
        finally
        {
            subscriptionGate.Release();
        }
    }

    /// <summary>
    /// Subscribes nodes with three round trips, however many nodes there are: one read of their display name, data type
    /// and access level, one read of the data type names not seen before (cached per client, so usually skipped), and
    /// one call that creates all monitored items. A node the server rejects isn't monitored and gets its error.
    /// When a round trip fails as a whole, e.g. because the connection drops, it throws, and no node stays monitored.
    /// </summary>
    /// <param name="requests">The nodes to subscribe to, each with the display name to use, or <see langword="null"/> for the node's own.</param>
    /// <param name="logEachNode">Whether to log each node; <see langword="false"/> for a batch, which logs one summary instead.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result per node, in the order of <paramref name="requests"/>, or <see langword="null"/> without an active subscription.</returns>
    private async Task<IReadOnlyList<NodeSubscriptionResult>?> SubscribeNodesCoreAsync(
        IReadOnlyList<(string NodeId, string? DisplayName)> requests,
        bool logEachNode,
        CancellationToken cancellationToken)
    {
        var subscription = activeSubscription;
        if (subscription is null || Session is null)
        {
            return null;
        }

        var results = new NodeSubscriptionResult?[requests.Count];
        var parsed = ParseNodeIds(requests, results);

        // Round trip 1: the display name, data type and access level of every node.
        var attributes = await ReadMonitoredNodeAttributesAsync(Session, [.. parsed.Select(x => x.NodeId)], cancellationToken);

        // Round trip 2, only for data types not seen before: their names.
        await ResolveDataTypeNamesAsync(Session, attributes.Select(x => x.DataTypeId), cancellationToken);

        var prepared = PrepareMonitoredItems(requests, parsed, attributes, results, logEachNode);
        if (prepared.Count > 0)
        {
            // Round trip 3: create all monitored items.
            await CreateMonitoredItemsAsync(subscription, requests, prepared, results, logEachNode, cancellationToken);
        }

        return [.. results.Select(x => x!)];
    }

    /// <summary>
    /// Parses the node ids. A node id that doesn't parse gets its result, <c>BadNodeIdInvalid</c>, right away.
    /// </summary>
    /// <param name="requests">The nodes to subscribe to.</param>
    /// <param name="results">The result per request, by index; filled in here for the node ids that don't parse.</param>
    /// <returns>The parsed node ids, each with its index in <paramref name="requests"/>.</returns>
    private List<(int Index, NodeId NodeId)> ParseNodeIds(
        IReadOnlyList<(string NodeId, string? DisplayName)> requests,
        NodeSubscriptionResult?[] results)
    {
        var parsed = new List<(int Index, NodeId NodeId)>(requests.Count);
        for (var i = 0; i < requests.Count; i++)
        {
            if (TryParseNodeId(requests[i].NodeId, out var nodeId))
            {
                parsed.Add((i, nodeId));
            }
            else
            {
                results[i] = Rejected(requests[i].NodeId, StatusCodes.BadNodeIdInvalid);
            }
        }

        return parsed;
    }

    /// <summary>
    /// Creates a monitored item for every node the server didn't reject in the attribute read, and registers it. The
    /// items aren't on the server yet; <see cref="CreateMonitoredItemsAsync"/> creates them there.
    /// </summary>
    /// <param name="requests">The nodes to subscribe to, each with the display name to use, or <see langword="null"/> for the node's own.</param>
    /// <param name="parsed">The node ids that parsed, each with its index in <paramref name="requests"/>.</param>
    /// <param name="attributes">The attributes read for <paramref name="parsed"/>, in the same order.</param>
    /// <param name="results">The result per request, by index; filled in here for the nodes the attribute read rejected.</param>
    /// <param name="logEachNode">Whether to log each node.</param>
    /// <returns>The items to create, each with its index in <paramref name="requests"/>.</returns>
    private List<(int Index, MonitoredItem Item)> PrepareMonitoredItems(
        IReadOnlyList<(string NodeId, string? DisplayName)> requests,
        List<(int Index, NodeId NodeId)> parsed,
        IReadOnlyList<MonitoredNodeAttributes> attributes,
        NodeSubscriptionResult?[] results,
        bool logEachNode)
    {
        var prepared = new List<(int Index, MonitoredItem Item)>(parsed.Count);
        for (var i = 0; i < parsed.Count; i++)
        {
            var (index, nodeId) = parsed[i];
            var nodeAttributes = attributes[i];
            if (StatusCode.IsBad(nodeAttributes.StatusCode))
            {
                results[index] = Rejected(requests[index].NodeId, nodeAttributes.StatusCode);
                continue;
            }

            var handle = monitoredNodes.NextHandle();
            if (logEachNode)
            {
                LogSubscriptionNodeSubscribing(requests[index].NodeId, handle);
            }

            var monitoredItem = new MonitoredItem
            {
                StartNodeId = nodeId,
                DisplayName = requests[index].DisplayName ?? nodeAttributes.DisplayName ?? requests[index].NodeId,
                SamplingInterval = activeSubscriptionOptions.SamplingIntervalMs,
                QueueSize = activeSubscriptionOptions.QueueSize,
                DiscardOldest = activeSubscriptionOptions.DiscardOldest,
                Handle = handle,
            };

            // Registered before the item is created on the server, so its first value already has the metadata.
            var dataTypeName = nodeAttributes.DataTypeId is null
                ? null
                : dataTypeNames.GetValueOrDefault(nodeAttributes.DataTypeId);

            monitoredNodes.Add(new MonitoredNode(handle, monitoredItem, dataTypeName, nodeAttributes.AccessLevel));
            monitoredItem.Notification += OnMonitoredItemNotification;
            prepared.Add((index, monitoredItem));
        }

        return prepared;
    }

    /// <summary>
    /// Creates the monitored items on the server in one call. The server can reject single items, e.g. a node without a
    /// value, without failing the call; those are removed again. When the call fails as a whole, all items are removed
    /// again before it throws.
    /// </summary>
    /// <param name="subscription">The subscription to create the items in.</param>
    /// <param name="requests">The nodes to subscribe to.</param>
    /// <param name="prepared">The items from <see cref="PrepareMonitoredItems"/>, each with its index in <paramref name="requests"/>.</param>
    /// <param name="results">The result per request, by index; filled in here for every prepared item.</param>
    /// <param name="logEachNode">Whether to log each node.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task CreateMonitoredItemsAsync(
        Subscription subscription,
        IReadOnlyList<(string NodeId, string? DisplayName)> requests,
        List<(int Index, MonitoredItem Item)> prepared,
        NodeSubscriptionResult?[] results,
        bool logEachNode,
        CancellationToken cancellationToken)
    {
        subscription.AddItems(prepared.Select(x => x.Item));

        try
        {
            await subscription.ApplyChangesAsync(cancellationToken);
        }
        catch
        {
            // The call failed as a whole, e.g. the connection dropped: none of the nodes counts as subscribed.
            await RemoveMonitoredItemsAsync(subscription, [.. prepared.Select(x => x.Item)]);
            throw;
        }

        List<(int Index, MonitoredItem Item)> rejected = [.. prepared.Where(x => ServiceResult.IsBad(x.Item.Status.Error))];
        foreach (var (index, item) in prepared.Except(rejected))
        {
            var handle = (uint)item.Handle;
            results[index] = new NodeSubscriptionResult(requests[index].NodeId, Succeeded: true, handle, ErrorMessage: null);
            if (logEachNode)
            {
                LogSubscriptionNodeSubscribed(requests[index].NodeId, handle);
            }
        }

        if (rejected.Count == 0)
        {
            return;
        }

        foreach (var (index, item) in rejected)
        {
            results[index] = Rejected(requests[index].NodeId, item.Status.Error.StatusCode.Code);
        }

        // Rejected items don't exist on the server, so this needs no call to it, and can't fail the nodes that are monitored.
        await RemoveMonitoredItemsAsync(subscription, [.. rejected.Select(x => x.Item)]);
    }

    /// <summary>
    /// Takes monitored items out of the subscription and the registry, and stops their values. Items the client knows
    /// the server created are deleted there too, as far as the connection allows; otherwise the subscription's next
    /// change deletes them. An item whose creation was interrupted can exist on the server without the client knowing
    /// it; its values never reach <see cref="NodeValueChanged"/>, and it goes away with the subscription.
    /// </summary>
    /// <param name="subscription">The subscription the items were added to.</param>
    /// <param name="items">The items to take out.</param>
    private async Task RemoveMonitoredItemsAsync(
        Subscription subscription,
        IReadOnlyList<MonitoredItem> items)
    {
        foreach (var item in items)
        {
            item.Notification -= OnMonitoredItemNotification;
            monitoredNodes.Remove((uint)item.Handle);
        }

        var createdOnServer = items.Any(x => x.Status.Created);
        subscription.RemoveItems(items);

        if (!createdOnServer)
        {
            return;
        }

        try
        {
            await subscription.ApplyChangesAsync(CancellationToken.None);
        }
        catch
        {
            // Ignore: best effort; the original error is what the caller needs.
        }
    }

    /// <summary>
    /// The result of a node that isn't monitored. Logs why.
    /// </summary>
    /// <param name="nodeId">The node, as the caller gave it.</param>
    /// <param name="statusCode">Why the node isn't monitored, e.g. <c>BadNodeIdUnknown</c>.</param>
    /// <returns>The failed result, with the status code's name as its error.</returns>
    private NodeSubscriptionResult Rejected(
        string nodeId,
        uint statusCode)
    {
        var error = GetStatusCodeName(statusCode) ?? $"0x{statusCode:X8}";
        LogSubscriptionNodeSubscribeFailure(nodeId, error);
        return new NodeSubscriptionResult(nodeId, Succeeded: false, MonitoredItemHandle: 0, error);
    }

    /// <summary>
    /// Parses a node id, e.g. <c>ns=3;s=StepUp</c>, without throwing.
    /// </summary>
    /// <param name="nodeId">The node id as text.</param>
    /// <param name="parsed">The parsed node id, or <see langword="null"/> when it doesn't parse.</param>
    /// <returns><see langword="true"/> when it parses.</returns>
    private static bool TryParseNodeId(
        string nodeId,
        [NotNullWhen(true)] out NodeId? parsed)
    {
        try
        {
            parsed = NodeId.Parse(nodeId);
            return true;
        }
        catch (Exception ex) when (ex is ServiceResultException or ArgumentException or FormatException)
        {
            parsed = null;
            return false;
        }
    }

    /// <summary>
    /// Reads the display name, data type and access level of the nodes, in as few reads as the server allows.
    /// </summary>
    /// <param name="session">The session to read with.</param>
    /// <param name="nodeIds">The nodes to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The attributes per node, in the order of <paramref name="nodeIds"/>.</returns>
    private static async Task<IReadOnlyList<MonitoredNodeAttributes>> ReadMonitoredNodeAttributesAsync(
        ISession session,
        IReadOnlyList<NodeId> nodeIds,
        CancellationToken cancellationToken)
    {
        var reads = new ReadValueIdCollection(nodeIds.Count * MonitoredNodeAttributes.AttributeIds.Length);
        foreach (var nodeId in nodeIds)
        {
            reads.AddRange(MonitoredNodeAttributes.AttributeIds.Select(x => new ReadValueId { NodeId = nodeId, AttributeId = x }));
        }

        var values = await ReadAsync(session, reads, cancellationToken);

        var attributes = new List<MonitoredNodeAttributes>(nodeIds.Count);
        for (var i = 0; i < values.Count; i += MonitoredNodeAttributes.AttributeIds.Length)
        {
            attributes.Add(MonitoredNodeAttributes.FromDataValues(values[i], values[i + 1], values[i + 2]));
        }

        return attributes;
    }

    /// <summary>
    /// Reads the names of data types the client hasn't seen before, in as few reads as the server allows, and caches them.
    /// </summary>
    /// <param name="session">The session to read with.</param>
    /// <param name="dataTypeIds">The nodes' data types; <see langword="null"/> for a node whose data type couldn't be read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    private async Task ResolveDataTypeNamesAsync(
        ISession session,
        IEnumerable<NodeId?> dataTypeIds,
        CancellationToken cancellationToken)
    {
        List<NodeId> unknown = [.. dataTypeIds
            .OfType<NodeId>()
            .Distinct()
            .Where(x => !dataTypeNames.ContainsKey(x))];

        if (unknown.Count == 0)
        {
            return;
        }

        var reads = new ReadValueIdCollection(unknown.Select(x => new ReadValueId { NodeId = x, AttributeId = Attributes.DisplayName }));
        var values = await ReadAsync(session, reads, cancellationToken);
        for (var i = 0; i < unknown.Count; i++)
        {
            dataTypeNames[unknown[i]] = StatusCode.IsGood(values[i].StatusCode)
                ? (values[i].Value as LocalizedText)?.Text
                : null;
        }
    }

    /// <summary>
    /// Reads the values in chunks of the server's maximum nodes per read, usually a single read.
    /// </summary>
    /// <param name="session">The session to read with.</param>
    /// <param name="reads">The node attributes to read.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The values, in the order of <paramref name="reads"/>.</returns>
    private static async Task<DataValueCollection> ReadAsync(
        ISession session,
        ReadValueIdCollection reads,
        CancellationToken cancellationToken)
    {
        var maxNodesPerRead = session.OperationLimits?.MaxNodesPerRead is > 0 and var limit
            ? (int)System.Math.Min(limit, int.MaxValue)
            : System.Math.Max(reads.Count, 1);

        var values = new DataValueCollection(reads.Count);
        foreach (var chunk in reads.Chunk(maxNodesPerRead))
        {
            var response = await session.ReadAsync(
                requestHeader: null,
                maxAge: 0,
                TimestampsToReturn.Neither,
                [.. chunk],
                cancellationToken);

            values.AddRange(response.Results);
        }

        return values;
    }

    /// <summary>
    /// Raises <see cref="NodeValueChanged"/> for a new value of a monitored item. Runs on a thread of the OPC UA stack.
    /// </summary>
    /// <param name="monitoredItem">The item whose value changed.</param>
    /// <param name="e">The notification with the new value.</param>
    private void OnMonitoredItemNotification(
        MonitoredItem monitoredItem,
        MonitoredItemNotificationEventArgs e)
    {
        try
        {
            if (e.NotificationValue is not MonitoredItemNotification notification)
            {
                return;
            }

            var handle = monitoredItem.Handle is uint h ? h : 0u;
            monitoredNodes.TryGet(handle, out var node);

            var monitoredNodeValue = new MonitoredNodeValue
            {
                MonitoredItemHandle = handle,
                NodeId = monitoredItem.StartNodeId?.ToString() ?? string.Empty,
                DisplayName = monitoredItem.DisplayName ?? string.Empty,
                Value = notification.Value?.WrappedValue.ToPlainValue(),
                ValueAsText = notification.Value?.WrappedValue.ToInvariantString(),
                ServerTimestamp = notification.Value?.ServerTimestamp,
                SourceTimestamp = notification.Value?.SourceTimestamp,
                StatusCode = notification.Value?.StatusCode.Code ?? 0,
                StatusCodeName = GetStatusCodeName(notification.Value?.StatusCode.Code ?? 0),
                DataTypeName = node?.DataTypeName,
                AccessLevel = node?.AccessLevel,
            };

            NodeValueChanged?.Invoke(this, monitoredNodeValue);
        }
        catch (Exception ex)
        {
            LogSubscriptionNotificationError(ex.Message);
        }
    }

    /// <summary>
    /// The name of a status code, e.g. <c>BadNodeIdUnknown</c>, without its info bits (see <see cref="StatusCodeIdentityMask"/>).
    /// </summary>
    /// <param name="statusCode">The status code, with or without info bits.</param>
    /// <returns>The name, or <see langword="null"/> for a status code the stack doesn't know.</returns>
    private static string? GetStatusCodeName(uint statusCode)
    {
        var name = StatusCodes.GetBrowseName(statusCode & StatusCodeIdentityMask);
        return string.IsNullOrEmpty(name) ? null : name;
    }

    /// <summary>
    /// After a reconnect that needed a new session, the OPC UA stack recreates the subscription and its monitored items
    /// on that session, and the values keep arriving there. This points the client at the recreated subscription, so
    /// unsubscribing and removing it keep working with the same handles. When the stack couldn't recreate it, the
    /// subscription is dropped, and the caller can create a new one.
    /// </summary>
    /// <param name="session">The reconnected session.</param>
    private void RestoreSubscriptionAfterReconnect(ISession session)
    {
        var subscription = activeSubscription;
        if (subscription is null || ReferenceEquals(subscription.Session, session))
        {
            return;
        }

        var recreated = session.Subscriptions.FirstOrDefault(x =>
            string.Equals(x.DisplayName, SubscriptionDisplayName, StringComparison.Ordinal));

        if (recreated is null)
        {
            DetachMonitoredNodes();
            activeSubscription = null;
            LogSubscriptionLostAfterReconnect();
            return;
        }

        foreach (var item in recreated.MonitoredItems)
        {
            // The stack copies the event handlers to the recreated items; make sure the client's is there exactly once.
            item.Notification -= OnMonitoredItemNotification;
            item.Notification += OnMonitoredItemNotification;
        }

        var count = monitoredNodes.Rebind(recreated.MonitoredItems);
        activeSubscription = recreated;

        LogSubscriptionRestoredAfterReconnect(recreated.Id, count);
    }

    /// <summary>
    /// Takes a subscription that wasn't created on the server out of the session again, and disposes it.
    /// </summary>
    /// <param name="session">The session the subscription was added to.</param>
    /// <param name="subscription">The subscription.</param>
    private static async Task RemoveFromSessionAsync(
        ISession session,
        Subscription subscription)
    {
        try
        {
            await session.RemoveSubscriptionAsync(subscription, CancellationToken.None);
        }
        catch
        {
            // Ignore: best effort; the original error is what the caller needs.
        }

        subscription.Dispose();
    }

    /// <summary>
    /// Stops the values of all monitored items and empties the registry. Leaves the items on the server.
    /// </summary>
    private void DetachMonitoredNodes()
    {
        foreach (var node in monitoredNodes.Nodes)
        {
            node.Item.Notification -= OnMonitoredItemNotification;
        }

        monitoredNodes.Clear();
    }

    /// <summary>
    /// Cleans up subscription resources during disconnect/dispose.
    /// Called from the main OpcUaClient disposal path.
    /// </summary>
    private void CleanupSubscription()
    {
        dataTypeNames.Clear();

        var subscription = activeSubscription;
        if (subscription is null)
        {
            return;
        }

        DetachMonitoredNodes();

        try
        {
            subscription.Dispose();
        }
        catch
        {
            // Ignore: best-effort dispose
        }

        activeSubscription = null;
    }
}