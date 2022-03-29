namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Ref: https://reference.opcfoundation.org/v104/Core/docs/Part6/5.1.2/
/// </summary>
[SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "OK")]
public enum OpcUaDataEncodingType
{
    None = 0,

    /// <summary>
    /// A two-state logical value(true or false).
    /// </summary>
    Boolean = 1,

    /// <summary>
    /// An integer value between −128 and 127 inclusive.
    /// </summary>
    SByte = 2,

    /// <summary>
    /// An integer value between 0 and 255 inclusive.
    /// </summary>
    Byte = 3,

    /// <summary>
    /// An integer value between −32768 and 32767 inclusive.
    /// </summary>
    Int16 = 4,

    /// <summary>
    /// An integer value between 0 and 65535 inclusive.
    /// </summary>
    UInt16 = 5,

    /// <summary>
    /// An integer value between −2147483648 and 2147483647 inclusive.
    /// </summary>
    Int32 = 6,

    /// <summary>
    /// An integer value between 0 and 4294967295 inclusive.
    /// </summary>
    UInt32 = 7,

    /// <summary>
    /// An integer value between −9223372036854775808 and 9223372036854775807 inclusive.
    /// </summary>
    Int64 = 8,

    /// <summary>
    /// An integer value between 0 and 18446744073709551615 inclusive.
    /// </summary>
    UInt64 = 9,

    /// <summary>
    /// An IEEE single precision(32 bit) floating point value.
    /// </summary>
    Float = 10,

    /// <summary>
    /// An IEEE double precision(64 bit) floating point value.
    /// </summary>
    Double = 11,

    /// <summary>
    /// A sequence of Unicode characters.
    /// </summary>
    String = 12,

    /// <summary>
    /// An instance in time.
    /// </summary>
    DateTime = 13,

    /// <summary>
    /// A 16-byte value that can be used as a globally unique identifier.
    /// </summary>
    Guid = 14,

    /// <summary>
    /// A sequence of octets.
    /// </summary>
    ByteString = 15,

    /// <summary>
    /// An XML element.
    /// </summary>
    XmlElement = 16,

    /// <summary>
    /// An identifier for a node in the address space of an OPC UA Server.
    /// </summary>
    NodeId = 17,

    /// <summary>
    /// A NodeId that allows the namespace URI to be specified instead of an index.
    /// </summary>
    ExpandedNodeId = 18,

    /// <summary>
    /// A numeric identifier for an error or condition that is associated with a value or an operation.
    /// </summary>
    StatusCode = 19,

    /// <summary>
    /// A name qualified by a namespace.
    /// </summary>
    QualifiedName = 20,

    /// <summary>
    /// Human readable text with an optional locale identifier.
    /// </summary>
    LocalizedText = 21,

    /// <summary>
    /// A structure that contains an application specific data type that may not be recognized by the receiver.
    /// </summary>
    ExtensionObject = 22,

    /// <summary>
    /// A data value with an associated status code and timestamps.
    /// </summary>
    DataValue = 23,

    /// <summary>
    /// A union of all of the types specified above.
    /// </summary>
    Variant = 24,

    /// <summary>
    /// A structure that contains detailed error and diagnostic information associated with a StatusCode.
    /// </summary>
    DiagnosticInfo = 25,
}