namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// NodeClassType.
/// </summary>
/// <remarks>
/// NodeClassType is the same enum as Opc.Ua.DataType-NodeClass.
/// </remarks>
[SuppressMessage("Microsoft.Naming", "CA1720:Identifiers should not contain type names", Justification = "OK")]
[SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "OK - By Design")]
public enum NodeClassType
{
    Unspecified = 0,

    Object = 1,

    Variable = 2,

    Method = 4,

    ObjectType = 8,

    VariableType = 16,

    ReferenceType = 32,

    DataType = 64,

    View = 128,
}