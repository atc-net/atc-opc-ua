namespace Atc.Opc.Ua.Extensions;

/// <summary>
/// Turns OPC UA values into text that doesn't depend on the machine's culture.
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
}