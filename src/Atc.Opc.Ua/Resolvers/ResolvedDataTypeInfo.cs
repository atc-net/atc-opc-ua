namespace Atc.Opc.Ua.Resolvers;

/// <summary>
/// Contains resolved DataType information for both OPC UA and .NET perspectives.
/// </summary>
/// <param name="OpcUaDataType">The OPC UA type descriptor.</param>
/// <param name="DotNetType">The .NET type descriptor.</param>
public sealed record ResolvedDataTypeInfo(
    OpUaDataType OpcUaDataType,
    DotNetTypeDescriptor DotNetType);