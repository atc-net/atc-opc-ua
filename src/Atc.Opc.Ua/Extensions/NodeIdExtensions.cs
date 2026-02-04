namespace Atc.Opc.Ua.Extensions;

/// <summary>
/// Extension methods for OPC UA NodeId.
/// </summary>
public static class NodeIdExtensions
{
    /// <summary>
    /// Gets the identifier value as a string representation.
    /// </summary>
    /// <remarks>
    /// Handles different identifier types appropriately:
    /// <list type="bullet">
    ///   <item>Numeric, String, Guid: Returns ToString() with quote stripping for String type</item>
    ///   <item>Opaque: Returns hex string (e.g., "A1-B2-C3")</item>
    /// </list>
    /// </remarks>
    /// <param name="nodeId">The NodeId to extract the identifier from.</param>
    /// <returns>String representation of the identifier, or empty string if not available.</returns>
    public static string GetIdentifierAsString(this NodeId nodeId)
    {
        ArgumentNullException.ThrowIfNull(nodeId);

        return nodeId.IdType switch
        {
            IdType.Numeric or IdType.String or IdType.Guid =>
                nodeId.Identifier?.ToString() is { } str
                    ? (str.Length > 1 && str[0] == '"' && str[^1] == '"'
                        ? str[1..^1]
                        : str)
                    : string.Empty,

            IdType.Opaque =>
                nodeId.Identifier is byte[] bytes
                    ? BitConverter.ToString(bytes)
                    : string.Empty,

            _ => string.Empty,
        };
    }
}
