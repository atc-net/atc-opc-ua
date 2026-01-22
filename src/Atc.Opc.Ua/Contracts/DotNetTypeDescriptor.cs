namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Describes a data type in pure .NET terms, suitable for systems
/// that don't know about OPC UA.
/// </summary>
public sealed class DotNetTypeDescriptor
{
    /// <summary>
    /// Gets or sets the kind of type (Primitive, Enum, Object, Array).
    /// </summary>
    public DotNetTypeKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the type name (e.g., "ServerState", "Int32", "BuildInfo").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CLR type name (e.g., "int", "string", "object").
    /// For primitives: the C# keyword or type name.
    /// For enums: the underlying type (e.g., "int").
    /// For structures: "object".
    /// </summary>
    public string ClrTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the element type descriptor for arrays.
    /// Only populated when <see cref="Kind"/> is <see cref="DotNetTypeKind.Array"/>.
    /// </summary>
    public DotNetTypeDescriptor? ArrayElementType { get; set; }

    /// <summary>
    /// Gets or sets the enum members.
    /// Only populated when <see cref="Kind"/> is <see cref="DotNetTypeKind.Enum"/>.
    /// </summary>
    public IList<DotNetEnumMember>? EnumMembers { get; set; }

    /// <summary>
    /// Gets or sets the structure fields.
    /// Only populated when <see cref="Kind"/> is <see cref="DotNetTypeKind.Complex"/>.
    /// Reserved for future use.
    /// </summary>
    public IList<DotNetFieldDescriptor>? StructureFields { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(Kind)}: {Kind}, {nameof(Name)}: {Name}, {nameof(ClrTypeName)}: {ClrTypeName}";
}