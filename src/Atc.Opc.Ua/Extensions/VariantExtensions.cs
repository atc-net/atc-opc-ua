namespace Atc.Opc.Ua.Extensions;

/// <summary>
/// Turns OPC UA values into plain .NET values, and into text that doesn't depend on the machine's culture.
/// </summary>
internal static class VariantExtensions
{
    /// <summary>
    /// Formats the value with the invariant culture, e.g. <c>21.5</c> for a Float, also on a machine with Danish settings.
    /// Date and time values use the round-trip format <c>O</c>, e.g. <c>2026-11-10T20:42:24.5000000Z</c>.
    /// </summary>
    /// <param name="variant">The value.</param>
    /// <returns>The text, or <see langword="null"/> when the value is null.</returns>
    public static string? ToInvariantString(this Variant variant)
        => variant.Value switch
        {
            null => null,
            DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
            DateTime[] dateTimes => "{" + string.Join('|', dateTimes.Select(x => x.ToString("O", CultureInfo.InvariantCulture))) + "}",
            _ => variant.ToString(format: null, CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Returns the value as a plain .NET value, without OPC UA types, so it can be used without the OPC UA stack.
    /// </summary>
    /// <remarks>
    /// Built-in types keep their .NET type, e.g. <see cref="float"/> for a Float, including arrays of them.
    /// A LocalizedText becomes its text, a NodeId, ExpandedNodeId or QualifiedName its text form, a StatusCode its code,
    /// a Guid a <see cref="Guid"/>, and an XmlElement its XML. Anything else, e.g. a structure, becomes its text, as
    /// <see cref="ToInvariantString"/> formats it.
    /// </remarks>
    /// <param name="variant">The value.</param>
    /// <returns>The plain value, or <see langword="null"/> when the value is null.</returns>
    public static object? ToPlainValue(this Variant variant)
        => variant.Value switch
        {
            null => null,
            bool or sbyte or byte or short or ushort or int or uint or long or ulong or float or double or string or DateTime or byte[] => variant.Value,
            bool[] or sbyte[] or short[] or ushort[] or int[] or uint[] or long[] or ulong[] or float[] or double[] or string[] or DateTime[] or byte[][] => variant.Value,
            Uuid uuid => (Guid)uuid,
            Uuid[] uuids => uuids.Select(x => (Guid)x).ToArray(),
            LocalizedText text => text.Text,
            LocalizedText[] texts => texts.Select(x => x?.Text).ToArray(),
            StatusCode statusCode => statusCode.Code,
            StatusCode[] statusCodes => statusCodes.Select(x => x.Code).ToArray(),
            NodeId or ExpandedNodeId or QualifiedName => variant.Value.ToString(),
            System.Xml.XmlElement xml => xml.OuterXml,
            _ => variant.ToInvariantString(),
        };
}