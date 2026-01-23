namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Specifies the kind of a .NET type representation.
/// </summary>
public enum DotNetTypeKind
{
    /// <summary>
    /// The type kind could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// A primitive type (e.g., int, string, bool).
    /// </summary>
    Primitive,

    /// <summary>
    /// An enumeration type with named values.
    /// </summary>
    Enum,

    /// <summary>
    /// A complex type (object, structure, or other non-primitive type).
    /// </summary>
    Complex,

    /// <summary>
    /// An array type.
    /// </summary>
    Array,
}