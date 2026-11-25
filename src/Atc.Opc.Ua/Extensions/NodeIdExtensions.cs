namespace Atc.Opc.Ua.Extensions;

/// <summary>
/// Extension methods for OPC UA NodeId.
/// </summary>
public static class NodeIdExtensions
{
    /// <summary>
    /// What <see cref="NodeBase.NodeIdentifier"/> holds for an opaque identifier. Consumers compare it, so it stays.
    /// </summary>
    private const string OpaqueIdentifierText = "System.Byte[]";

    /// <summary>
    /// Gets the identifier value as a string representation.
    /// </summary>
    /// <remarks>
    /// Handles different identifier types appropriately:
    /// <list type="bullet">
    ///   <item>Numeric, String, Guid: the identifier's text, with surrounding quotes removed from a String</item>
    ///   <item>Opaque: Returns hex string (e.g., "A1-B2-C3")</item>
    /// </list>
    /// </remarks>
    /// <param name="nodeId">The NodeId to extract the identifier from.</param>
    /// <returns>String representation of the identifier, or empty string if not available.</returns>
    public static string GetIdentifierAsString(this NodeId nodeId)
        => nodeId.IdType switch
        {
            IdType.Opaque => nodeId.TryGetValue(out ByteString bytes)
                ? BitConverter.ToString(bytes.ToArray())
                : string.Empty,
            _ => StripQuotes(nodeId.GetIdentifierText()),
        };

    /// <summary>
    /// The identifier as text, as <see cref="NodeBase.NodeIdentifier"/> holds it: a number, a string as it is, a GUID in
    /// lower case, and <c>System.Byte[]</c> for an opaque identifier.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <returns>The text, or empty when there's no identifier.</returns>
    internal static string GetIdentifierText(this NodeId nodeId)
        => nodeId.IdType switch
        {
            IdType.Numeric => nodeId.TryGetValue(out uint number) ? number.ToString(CultureInfo.InvariantCulture) : string.Empty,
            IdType.String => nodeId.TryGetValue(out string text) ? text : string.Empty,
            IdType.Guid => nodeId.TryGetValue(out Guid guid) ? guid.ToString() : string.Empty,
            IdType.Opaque => OpaqueIdentifierText,
            _ => string.Empty,
        };

    /// <summary>
    /// The identifier as text, as <see cref="NodeBase.NodeIdentifier"/> holds it; see
    /// <see cref="GetIdentifierText(NodeId)"/>.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <returns>The text, or empty when there's no identifier.</returns>
    internal static string GetIdentifierText(this ExpandedNodeId nodeId)
        => nodeId.IdType switch
        {
            IdType.Numeric => nodeId.TryGetValue(out uint number) ? number.ToString(CultureInfo.InvariantCulture) : string.Empty,
            IdType.String => nodeId.TryGetValue(out string text) ? text : string.Empty,
            IdType.Guid => nodeId.TryGetValue(out Guid guid) ? guid.ToString() : string.Empty,
            IdType.Opaque => OpaqueIdentifierText,
            _ => string.Empty,
        };

    private static string StripQuotes(string text)
        => text.Length > 1 && text[0] == '"' && text[^1] == '"'
            ? text[1..^1]
            : text;
}