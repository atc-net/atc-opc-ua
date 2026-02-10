namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Represents a value change notification for a monitored OPC UA node.
/// </summary>
/// <remarks>
/// Instances of this class are raised through the <c>NodeValueChanged</c> event
/// on <c>IOpcUaClient</c> whenever a subscription delivers a new data-change
/// notification from the server.
/// </remarks>
[SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "MonitoredNodeValue is a better API name than MonitoredNodeValueEventArgs.")]
[SuppressMessage("Major Code Smell", "S3376:Make this class name end with 'EventArgs'.", Justification = "MonitoredNodeValue is a better API name than MonitoredNodeValueEventArgs.")]
public class MonitoredNodeValue : EventArgs
{
    /// <summary>
    /// The bits of an OPC UA status code that hold its severity.
    /// </summary>
    private const uint SeverityMask = 0xC0000000;

    /// <summary>
    /// Gets or sets the handle that uniquely identifies the monitored item
    /// within the subscription.
    /// </summary>
    public uint MonitoredItemHandle { get; set; }

    /// <summary>
    /// Gets or sets the fully-qualified node-id (e.g. <c>"ns=2;s=Demo.Dynamic.Scalar.Float"</c>).
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the localised display name of the monitored node.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value as a .NET value, e.g. <c>21.5f</c> for a Float, <c>true</c> for a Boolean or <c>(short)42</c>
    /// for an Int16, or <see langword="null"/> when the value is null.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Built-in types keep their .NET type: <see cref="bool"/>, the integer types, <see cref="float"/>,
    /// <see cref="double"/>, <see cref="string"/>, <see cref="DateTime"/>, <see cref="Guid"/>, <c>byte[]</c>, and arrays
    /// of them. A LocalizedText is its text, a NodeId or QualifiedName its text form, and a StatusCode its code.
    /// Structures and other types without a .NET equivalent are their text, as in <see cref="ValueAsText"/>.
    /// </para>
    /// <para>
    /// Use <see cref="TryGetValue{T}"/> to get it as a specific type. After JSON deserialization, the value is a
    /// <see cref="JsonElement"/>, which <see cref="TryGetValue{T}"/> also reads.
    /// </para>
    /// </remarks>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the value as text, formatted with the invariant culture: <c>21.5</c>, also on a machine with
    /// Danish settings. Date and time values use the round-trip format <c>O</c>. <see langword="null"/> when the value
    /// is null.
    /// </summary>
    public string? ValueAsText { get; set; }

    /// <summary>
    /// Gets or sets the server timestamp of the value change,
    /// or <see langword="null"/> if unknown.
    /// </summary>
    public DateTime? ServerTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the source timestamp of the value change,
    /// or <see langword="null"/> if unknown.
    /// </summary>
    public DateTime? SourceTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the OPC UA status code for this value.
    /// The two highest bits are the severity: <c>00</c> Good, <c>01</c> Uncertain, <c>10</c> Bad. The lower bits can carry
    /// more information, e.g. the Overflow bit when a monitored item's queue dropped values, which is still Good.
    /// </summary>
    public uint StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the status code's name, e.g. <c>Good</c> or <c>BadNodeIdUnknown</c>,
    /// or <see langword="null"/> when it's unknown.
    /// </summary>
    public string? StatusCodeName { get; set; }

    /// <summary>
    /// Gets or sets the name of the data type (e.g. <c>"Float"</c>, <c>"Int32"</c>),
    /// or <see langword="null"/> if unknown.
    /// </summary>
    public string? DataTypeName { get; set; }

    /// <summary>
    /// Gets or sets the access level bitmask for the node,
    /// or <see langword="null"/> if not yet read.
    /// </summary>
    public byte? AccessLevel { get; set; }

    /// <summary>
    /// Gets a value indicating whether the status code's severity is <c>Good</c>, also when it carries more information
    /// such as the Overflow bit.
    /// </summary>
    public bool IsGood => (StatusCode & SeverityMask) == 0;

    /// <summary>
    /// Gets the value as <typeparamref name="T"/>, when it is one or converts to it without losing anything: a whole
    /// number to another integer type it fits in, or any number to <see cref="double"/> or <see cref="decimal"/>.
    /// A number with decimals is never converted to an integer type.
    /// </summary>
    /// <example>
    /// <code>
    /// if (e.TryGetValue&lt;double&gt;(out var temperature)) // a Float 21.5 gives 21.5
    /// </code>
    /// </example>
    /// <typeparam name="T">The type to get the value as.</typeparam>
    /// <param name="value">The value, when it could be converted.</param>
    /// <returns><see langword="true"/> when the value could be converted.</returns>
    public bool TryGetValue<T>(out T value)
    {
        var converted = Value switch
        {
            T typed => typed,
            JsonElement json => FromJson(json, typeof(T)),
            _ => ConvertNumber(Value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)),
        };

        if (converted is T result)
        {
            value = result;
            return true;
        }

        value = default!;
        return false;
    }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(NodeId)}: {NodeId}, " +
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(Value)}: {ValueAsText ?? "(null)"}, " +
        $"{nameof(StatusCode)}: 0x{StatusCode:X8}, " +
        $"{nameof(ServerTimestamp)}: {ServerTimestamp?.ToString("O", CultureInfo.InvariantCulture) ?? "(null)"}";

    private static object? FromJson(
        JsonElement json,
        Type type)
    {
        try
        {
            return json.Deserialize(type);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or InvalidOperationException)
        {
            return null;
        }
    }

    private static object? ConvertNumber(
        object? source,
        Type target)
    {
        if (source is null || !IsNumber(source.GetType()) || !IsNumber(target))
        {
            return null;
        }

        // Never drop decimals, and never lose precision from double to float.
        if ((IsInteger(target) && !IsInteger(source.GetType())) ||
            (target == typeof(float) && source is double or decimal))
        {
            return null;
        }

        try
        {
            return Convert.ChangeType(source, target, CultureInfo.InvariantCulture);
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static bool IsInteger(Type type)
        => type == typeof(sbyte) || type == typeof(byte) ||
           type == typeof(short) || type == typeof(ushort) ||
           type == typeof(int) || type == typeof(uint) ||
           type == typeof(long) || type == typeof(ulong);

    private static bool IsNumber(Type type)
        => IsInteger(type) || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
}