namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Represents a single member of an enumeration type.
/// </summary>
public sealed class DotNetEnumMember
{
    /// <summary>
    /// Gets or sets the numeric value of the enum member.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Gets or sets the name of the enum member (e.g., "Running", "Failed").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional localized display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Name} = {Value}";
}