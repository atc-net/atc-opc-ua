namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Represents a field in a structure type.
/// Reserved for future use when structure support is added.
/// </summary>
public sealed class DotNetFieldDescriptor
{
    /// <summary>
    /// Gets or sets the name of the field.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type descriptor for this field.
    /// </summary>
    public DotNetTypeDescriptor? Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this field is optional.
    /// </summary>
    public bool IsOptional { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Name}: {Type?.Name ?? "unknown"}{(IsOptional ? "?" : string.Empty)}";
}