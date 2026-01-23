namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Specifies the kind of an OPC UA data type.
/// </summary>
public enum OpcUaTypeKind
{
    /// <summary>
    /// The data type kind could not be determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// A built-in primitive type (e.g., Int32, String, Boolean).
    /// </summary>
    Primitive,

    /// <summary>
    /// An enumeration type with named values.
    /// </summary>
    Enum,

    /// <summary>
    /// A structured type with fields.
    /// </summary>
    Structure,
}