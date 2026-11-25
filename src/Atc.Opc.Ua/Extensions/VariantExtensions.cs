namespace Atc.Opc.Ua.Extensions;

/// <summary>
/// Turns OPC UA values into plain .NET values, and into text that doesn't depend on the machine's culture.
/// </summary>
internal static class VariantExtensions
{
    private const string NullText = "(null)";

    /// <summary>
    /// Writes array text as it reads, e.g. <c>æøå</c> rather than <c>\u00E6\u00F8\u00E5</c>. The text isn't put into HTML.
    /// </summary>
    private static readonly JsonWriterOptions ArrayWriterOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Formats the value with the invariant culture, e.g. <c>21.5</c> for a Float, also on a machine with Danish settings.
    /// Date and time values use the round-trip format <c>O</c>, e.g. <c>2026-11-10T20:42:24.5000000Z</c>, and a ByteString
    /// is hex. An array is a JSON array, e.g. <c>[1,2]</c> or <c>["a","b;c",""]</c>: numbers and Booleans as JSON values,
    /// everything else as a JSON string of the text a single value gets.
    /// </summary>
    /// <param name="variant">The value.</param>
    /// <returns>The text, or <see langword="null"/> when the value is null.</returns>
    public static string? ToInvariantString(this Variant variant)
    {
        if (variant.IsNull)
        {
            return null;
        }

        var typeInfo = variant.TypeInfo;
        if (typeInfo.IsMatrix)
        {
            return variant.AsBoxedObject(Variant.BoxingBehavior.LegacyWithMatrix) is Matrix matrix
                ? $"{typeInfo.BuiltInType}[{string.Join(',', matrix.Dimensions)}]"
                : NullText;
        }

        if (!typeInfo.IsArray)
        {
            return FormatScalar(variant.AsBoxedObject());
        }

        return variant.AsBoxedObject(Variant.BoxingBehavior.Legacy) is Array elements
            ? FormatArray(elements)
            : NullText;
    }

    /// <summary>
    /// Returns the value as a plain .NET value, without OPC UA types, so it can be used without the OPC UA stack.
    /// </summary>
    /// <remarks>
    /// Built-in types keep their .NET type, e.g. <see cref="float"/> for a Float, including arrays of them. A DateTime
    /// becomes a <see cref="DateTime"/> in UTC, a ByteString a byte array, a Guid a <see cref="Guid"/>, a LocalizedText
    /// its text, a NodeId, ExpandedNodeId or QualifiedName its text form, a StatusCode its code, and an XmlElement its
    /// XML. Anything else, e.g. a structure, becomes its text, as <see cref="ToInvariantString"/> formats it.
    /// </remarks>
    /// <param name="variant">The value.</param>
    /// <returns>The plain value, or <see langword="null"/> when the value is null.</returns>
    public static object? ToPlainValue(this Variant variant)
    {
        if (variant.IsNull)
        {
            return null;
        }

        var typeInfo = variant.TypeInfo;
        if (typeInfo.IsMatrix)
        {
            return variant.ToInvariantString();
        }

        if (!typeInfo.IsArray)
        {
            return ToPlainScalar(variant.AsBoxedObject()) ?? variant.ToInvariantString();
        }

        return (variant.AsBoxedObject(Variant.BoxingBehavior.Legacy) as Array) switch
        {
            null => null,
            bool[] or sbyte[] or byte[] or short[] or ushort[] or int[] or uint[] or long[] or ulong[] or float[] or double[] or string[] => variant.AsBoxedObject(Variant.BoxingBehavior.Legacy),
            DateTimeUtc[] dateTimes => dateTimes.Select(ToDateTime).ToArray(),
            ByteString[] byteStrings => byteStrings.Select(x => x.ToArray()).ToArray(),
            Uuid[] uuids => uuids.Select(x => x.Guid).ToArray(),
            LocalizedText[] texts => texts.Select(x => x.Text).ToArray(),
            StatusCode[] statusCodes => statusCodes.Select(x => x.Code).ToArray(),
            _ => variant.ToInvariantString(),
        };
    }

    /// <summary>
    /// Turns a plain .NET value into an OPC UA value, for writing it: the reverse of <see cref="ToPlainValue"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="byte"/> array is a ByteString, and an array of them an array of ByteStrings; an
    /// <see cref="System.Xml.XmlElement"/> is an XmlElement. Other built-in .NET types and the OPC UA types map to the
    /// OPC UA type of the same kind, e.g. <see cref="float"/> to Float and <see cref="DateTime"/> to DateTime.
    /// </remarks>
    /// <param name="value">The value.</param>
    /// <param name="paramName">The caller's parameter, for the exception.</param>
    /// <returns>The OPC UA value.</returns>
    /// <exception cref="ArgumentException">The value's type has no OPC UA type, e.g. <see cref="decimal"/>.</exception>
    public static Variant ToVariant(
        object? value,
        string paramName)
        => value switch
        {
            null => Variant.Null,
            Variant variant => variant,

            // .NET lets an sbyte array pass as a byte array, so match the exact type: an sbyte array is an SByte array.
            byte[] bytes when value.GetType() == typeof(byte[]) => Variant.From(ByteString.From(bytes)),
            byte[][] byteArrays when value.GetType() == typeof(byte[][]) => Variant.From(byteArrays.Select(ByteString.From).ToArray()),
            System.Xml.XmlElement xml => Variant.From(XmlElement.From(xml)),
            System.Xml.XmlElement[] xmlElements => Variant.From(xmlElements.Select(XmlElement.From).ToArray()),
            _ when VariantHelper.TryCastFromWithReflectionFallback(value, out var variant) => variant,
            _ => throw new ArgumentException($"A value of type {value.GetType().Name} can't be written to an OPC UA node.", paramName),
        };

    /// <summary>
    /// A date and time as a <see cref="DateTime"/> in UTC. OPC UA's "no date" becomes <see cref="DateTime.MinValue"/>.
    /// </summary>
    /// <param name="dateTime">The OPC UA date and time.</param>
    /// <returns>The date and time.</returns>
    public static DateTime ToDateTime(this DateTimeUtc dateTime)
        => dateTime.IsNull
            ? DateTime.MinValue
            : dateTime.ToDateTime();

    /// <summary>
    /// A timestamp as a <see cref="DateTime"/> in UTC, or <see langword="null"/> when there is none.
    /// </summary>
    /// <param name="dateTime">The OPC UA timestamp.</param>
    /// <returns>The timestamp, or <see langword="null"/>.</returns>
    public static DateTime? ToTimestamp(this DateTimeUtc dateTime)
        => dateTime.IsNull
            ? null
            : dateTime.ToDateTime();

    private static object? ToPlainScalar(object? value)
        => value switch
        {
            null => null,
            bool or sbyte or byte or short or ushort or int or uint or long or ulong or float or double or string => value,
            DateTimeUtc dateTime => dateTime.ToDateTime(),
            ByteString byteString => byteString.ToArray(),
            Uuid uuid => uuid.Guid,
            LocalizedText text => text.Text,
            StatusCode statusCode => statusCode.Code,
            NodeId or ExpandedNodeId or QualifiedName => value.ToString(),
            XmlElement xml => xml.OuterXml ?? string.Empty,
            _ => null,
        };

    private static string FormatArray(Array elements)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, ArrayWriterOptions))
        {
            writer.WriteStartArray();
            foreach (var element in elements)
            {
                WriteElement(writer, element);
            }

            writer.WriteEndArray();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void WriteElement(
        Utf8JsonWriter writer,
        object? element)
    {
        switch (element)
        {
            case null:
                writer.WriteNullValue();
                break;
            case bool boolean:
                writer.WriteBooleanValue(boolean);
                break;
            case sbyte or byte or short or ushort or int or long:
                writer.WriteNumberValue(Convert.ToInt64(element, CultureInfo.InvariantCulture));
                break;
            case uint or ulong:
                writer.WriteNumberValue(Convert.ToUInt64(element, CultureInfo.InvariantCulture));
                break;
            case float number when float.IsFinite(number):
                writer.WriteRawValue(number.ToString(CultureInfo.InvariantCulture));
                break;
            case double number when double.IsFinite(number):
                writer.WriteRawValue(number.ToString(CultureInfo.InvariantCulture));
                break;
            default:
                writer.WriteStringValue(FormatScalar(element));
                break;
        }
    }

    private static string FormatScalar(object? value)
        => value switch
        {
            null => NullText,
            DateTimeUtc dateTime => dateTime.ToDateTime().ToString("O", CultureInfo.InvariantCulture),
            ByteString byteString => Convert.ToHexString(byteString.ToArray()),
            byte[] bytes => Convert.ToHexString(bytes),
            Uuid uuid => uuid.Guid.ToString(),
            LocalizedText text => text.Text ?? string.Empty,
            StatusCode statusCode => statusCode.SymbolicId ?? $"0x{statusCode.Code:X8}",
            XmlElement xml => xml.OuterXml ?? string.Empty,
            ExtensionObject extensionObject => extensionObject.IsNull ? NullText : extensionObject.ToString() ?? string.Empty,
            Variant inner => inner.ToInvariantString() ?? NullText,
            IFormattable formattable => formattable.ToString(format: null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
}