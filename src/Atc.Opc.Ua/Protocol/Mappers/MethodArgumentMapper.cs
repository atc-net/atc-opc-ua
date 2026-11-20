namespace Atc.Opc.Ua.Protocol.Mappers;

/// <summary>
/// Converts a method's arguments: the input arguments, given as text with a data type, into the values sent to the
/// server, and the output arguments the server returns into text with a data type.
/// </summary>
internal static class MethodArgumentMapper
{
    private const DateTimeStyles UtcDateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

    /// <summary>
    /// Converts a method's input arguments.
    /// </summary>
    /// <param name="arguments">The arguments, in the method's order.</param>
    /// <returns>One value for each argument, in the same order.</returns>
    /// <remarks>
    /// Numbers are read with an invariant English culture. A date and time without a time zone is read as UTC.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// An argument's value isn't valid for its data type, or its data type can't be sent as a method argument.
    /// </exception>
    public static VariantCollection ToVariants(
        IReadOnlyList<MethodExecutionParameter> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        var variants = new VariantCollection(arguments.Count);
        for (var i = 0; i < arguments.Count; i++)
        {
            variants.Add(ToVariant(arguments, i));
        }

        return variants;
    }

    /// <summary>
    /// Converts a method's output arguments.
    /// </summary>
    /// <param name="outputArguments">The output arguments the server returned.</param>
    /// <returns>
    /// One result for each output argument, in the same order. An output without a value, or of a type that has no
    /// <see cref="OpcUaDataEncodingType"/>, gets <see cref="OpcUaDataEncodingType.None"/>.
    /// </returns>
    public static List<MethodExecutionResult> ToResults(
        VariantCollection outputArguments)
    {
        ArgumentNullException.ThrowIfNull(outputArguments);

        var results = new List<MethodExecutionResult>(outputArguments.Count);
        foreach (var outputArgument in outputArguments)
        {
            // The enum's values are the OPC UA built-in type ids; an empty output has no type info.
            var dataEncoding = (OpcUaDataEncodingType)(outputArgument.TypeInfo?.BuiltInType ?? BuiltInType.Null);
            if (!Enum.IsDefined(dataEncoding))
            {
                dataEncoding = OpcUaDataEncodingType.None;
            }

            results.Add(new MethodExecutionResult(dataEncoding, outputArgument.Value?.ToString() ?? string.Empty));
        }

        return results;
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK - one case per data type keeps each conversion visible.")]
    private static Variant ToVariant(
        IReadOnlyList<MethodExecutionParameter> arguments,
        int index)
    {
        var (dataEncoding, value) = arguments[index];
        var culture = GlobalizationConstants.EnglishCultureInfo;

        switch (dataEncoding)
        {
            case OpcUaDataEncodingType.Boolean:
                if (bool.TryParse(value, out var boolean))
                {
                    return new Variant(boolean);
                }

                break;
            case OpcUaDataEncodingType.SByte:
                if (sbyte.TryParse(value, NumberStyles.Any, culture, out var sbyteValue))
                {
                    return new Variant(sbyteValue);
                }

                break;
            case OpcUaDataEncodingType.Byte:
                if (byte.TryParse(value, NumberStyles.Any, culture, out var byteValue))
                {
                    return new Variant(byteValue);
                }

                break;
            case OpcUaDataEncodingType.Int16:
                if (short.TryParse(value, NumberStyles.Any, culture, out var int16))
                {
                    return new Variant(int16);
                }

                break;
            case OpcUaDataEncodingType.UInt16:
                if (ushort.TryParse(value, NumberStyles.Any, culture, out var uint16))
                {
                    return new Variant(uint16);
                }

                break;
            case OpcUaDataEncodingType.Int32:
                if (int.TryParse(value, NumberStyles.Any, culture, out var int32))
                {
                    return new Variant(int32);
                }

                break;
            case OpcUaDataEncodingType.UInt32:
                if (uint.TryParse(value, NumberStyles.Any, culture, out var uint32))
                {
                    return new Variant(uint32);
                }

                break;
            case OpcUaDataEncodingType.Int64:
                if (long.TryParse(value, NumberStyles.Any, culture, out var int64))
                {
                    return new Variant(int64);
                }

                break;
            case OpcUaDataEncodingType.UInt64:
                if (ulong.TryParse(value, NumberStyles.Any, culture, out var uint64))
                {
                    return new Variant(uint64);
                }

                break;
            case OpcUaDataEncodingType.Float:
                if (float.TryParse(value, NumberStyles.Any, culture, out var single))
                {
                    return new Variant(single);
                }

                break;
            case OpcUaDataEncodingType.Double:
                if (double.TryParse(value, NumberStyles.Any, culture, out var number))
                {
                    return new Variant(number);
                }

                break;
            case OpcUaDataEncodingType.String:
                return new Variant(value);
            case OpcUaDataEncodingType.DateTime:
                if (DateTime.TryParse(value, culture, UtcDateTimeStyles, out var dateTime))
                {
                    return new Variant(dateTime);
                }

                break;
            case OpcUaDataEncodingType.Guid:
                if (Guid.TryParse(value, out var guid))
                {
                    return new Variant(guid);
                }

                break;
            default:
                throw new ArgumentException(
                    $"Method argument {index + 1} has data type {dataEncoding}, which can't be sent as a method argument.",
                    nameof(arguments));
        }

        throw new ArgumentException(
            $"Method argument {index + 1} ('{value}') isn't a valid {dataEncoding}.",
            nameof(arguments));
    }
}