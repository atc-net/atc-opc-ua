namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Describes an OPC UA built-in or user-defined data type and
/// whether the value is an array of that type.
/// </summary>
/// <remarks>
/// This lightweight DTO is typically used to carry datatype information
/// together with metadata such as array rank when browsing or importing
/// an address space.
/// </remarks>
public class OpUaDataType
{
    /// <summary>
    /// Gets or sets the NodeId of the data type (e.g., "i=852", "ns=3;i=3063").
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbolic name of the OPC UA data type
    /// (e.g. <c>"UInt32"</c>, <c>"Float"</c>, or a custom structure name).
    /// The property defaults to <see cref="string.Empty"/> and is therefore
    /// never <see langword="null"/>.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the localized display name of the data type.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier type (e.g., "Numeric", "String", "Guid", "Opaque").
    /// </summary>
    public string IdentifierType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier value (e.g., "852", "CustomTypeName", or hex for opaque).
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the kind of data type (Primitive, Enum, Structure, Unknown).
    /// </summary>
    public OpcUaTypeKind Kind { get; set; }

    /// <summary>
    /// Gets or sets a value that indicates whether the data value is
    /// an array (<see langword="true"/>) or a scalar (<see langword="false"/>).
    /// </summary>
    public bool IsArray { get; set; }

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(NodeId)}: {NodeId}, {nameof(Name)}: {Name}, {nameof(IdentifierType)}: {IdentifierType}, {nameof(Identifier)}: {Identifier}, {nameof(Kind)}: {Kind}, {nameof(IsArray)}: {IsArray}";
}