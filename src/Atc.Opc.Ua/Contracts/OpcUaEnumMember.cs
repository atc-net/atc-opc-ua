namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Represents a single member (field) of an OPC UA enumeration data type.
/// </summary>
/// <remarks>
/// <para>
/// OPC UA enumerations can be defined either as simple <c>EnumStrings</c> (where the
/// index is the enum value) or as complex <c>EnumValues</c> (where each field has
/// explicit value, name, display name, and description properties).
/// </para>
/// <para>
/// This class normalizes both representations into a common structure so that
/// consumers don't need to worry about how the enum was originally defined.
/// </para>
/// </remarks>
public class OpcUaEnumMember
{
    /// <summary>
    /// Gets or sets the numeric value of this enum member.
    /// </summary>
    /// <remarks>
    /// For <c>EnumStrings</c> this is the zero-based index of the string.
    /// For <c>EnumValues</c> this is the explicit <c>Value</c> field.
    /// </remarks>
    public long Value { get; set; }

    /// <summary>
    /// Gets or sets the symbolic name of this enum member.
    /// </summary>
    /// <remarks>
    /// For <c>EnumStrings</c> this equals <see cref="DisplayName"/>.
    /// For <c>EnumValues</c> this is the <c>Name</c> field which is typically
    /// a code-friendly identifier (e.g. "Running", "NotSupported").
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the localized display name of this enum member.
    /// </summary>
    /// <remarks>
    /// This is the human-readable text suitable for display in user interfaces.
    /// </remarks>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional description of this enum member.
    /// </summary>
    /// <remarks>
    /// Only available when the enum is defined using <c>EnumValues</c>.
    /// Empty string when not available.
    /// </remarks>
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(Value)}: {Value}, " +
        $"{nameof(Name)}: {Name}, " +
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(Description)}: {Description}";
}