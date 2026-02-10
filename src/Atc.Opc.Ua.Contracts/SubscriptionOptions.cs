namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Configuration options for an OPC UA subscription.
/// </summary>
/// <remarks>
/// Passed to <c>IOpcUaClient.CreateSubscriptionAsync</c> to control publishing
/// and sampling behaviour. All intervals are expressed in milliseconds.
/// </remarks>
public class SubscriptionOptions
{
    /// <summary>
    /// Gets or sets the publishing interval in milliseconds.
    /// The server will attempt to publish data-change notifications at this rate.
    /// Default is <c>250</c> ms.
    /// </summary>
    public int PublishingIntervalMs { get; set; } = 250;

    /// <summary>
    /// Gets or sets the sampling interval in milliseconds for monitored items.
    /// Controls how often the server samples values from the underlying data source.
    /// Default is <c>250</c> ms.
    /// </summary>
    public int SamplingIntervalMs { get; set; } = 250;

    /// <summary>
    /// Gets or sets the queue size for each monitored item.
    /// When the queue is full, the oldest or newest values are discarded
    /// depending on <see cref="DiscardOldest"/>.
    /// Default is <c>10</c>.
    /// </summary>
    public uint QueueSize { get; set; } = 10;

    /// <summary>
    /// Gets or sets a value indicating whether the oldest queued values
    /// should be discarded when the queue is full.
    /// Default is <see langword="true"/>.
    /// </summary>
    public bool DiscardOldest { get; set; } = true;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(PublishingIntervalMs)}: {PublishingIntervalMs}, " +
        $"{nameof(SamplingIntervalMs)}: {SamplingIntervalMs}, " +
        $"{nameof(QueueSize)}: {QueueSize}, " +
        $"{nameof(DiscardOldest)}: {DiscardOldest}";
}
