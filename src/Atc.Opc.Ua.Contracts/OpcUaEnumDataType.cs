namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Represents an OPC UA enumeration data type with its complete definition
/// including all members.
/// </summary>
/// <remarks>
/// <para>
/// This class captures the metadata and member definitions of an OPC UA enumeration,
/// regardless of whether it was defined using <c>EnumStrings</c> (simple) or
/// <c>EnumValues</c> (complex) on the server.
/// </para>
/// <para>
/// Use this type when you need to:
/// <list type="bullet">
/// <item>Display enum options to users in a UI</item>
/// <item>Map numeric values to their symbolic names</item>
/// <item>Generate code or documentation from enum definitions</item>
/// </list>
/// </para>
/// </remarks>
public class OpcUaEnumDataType
{
    /// <summary>
    /// Gets or sets the fully-qualified node-id of the DataType node
    /// (e.g. <c>"i=852"</c> for ServerState or <c>"ns=3;i=3063"</c> for custom types).
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the browse name of the DataType.
    /// </summary>
    /// <remarks>
    /// This is the symbolic type name (e.g. "ServerState", "SimaticOperatingState").
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the localized display name of the DataType.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this enum was defined using
    /// <c>EnumValues</c> (complex) rather than <c>EnumStrings</c> (simple).
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the <see cref="OpcUaEnumMember.Description"/>
    /// property of members may contain additional information.
    /// </remarks>
    public bool HasEnumValues { get; set; }

    /// <summary>
    /// Gets or sets the optional description of the DataType.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collection of members that define the possible values of this enumeration.
    /// </summary>
    /// <remarks>
    /// Members are not guaranteed to be in any particular order. Sort by
    /// <see cref="OpcUaEnumMember.Value"/> if a consistent order is needed.
    /// </remarks>
    public IList<OpcUaEnumMember> Members { get; set; } = new List<OpcUaEnumMember>();

    /// <summary>
    /// Returns a simple string representation of the DataType for diagnostic purposes.
    /// </summary>
    /// <returns>A concise string representation of the DataType.</returns>
    public string ToStringSimple() =>
        $"{nameof(Name)}: {Name}, " +
        $"MemberCount: {Members.Count}, " +
        $"{nameof(HasEnumValues)}: {HasEnumValues}";

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(NodeId)}: {NodeId}, " +
        $"{nameof(Name)}: {Name}, " +
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(HasEnumValues)}: {HasEnumValues}, " +
        $"{nameof(Description)}: {Description}, " +
        $"MemberCount: {Members.Count}";
}