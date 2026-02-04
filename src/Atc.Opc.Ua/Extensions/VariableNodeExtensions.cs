namespace Atc.Opc.Ua.Extensions;

public static class VariableNodeExtensions
{
    public static OpUaDataType CreateBasicOpcUaDataType(this VariableNode variableNode)
    {
        ArgumentNullException.ThrowIfNull(variableNode);

        var builtInType = TypeInfo.GetBuiltInType(variableNode.DataType);
        var isBuiltIn = builtInType != BuiltInType.Null;

        return new OpUaDataType
        {
            NodeId = variableNode.DataType.ToString(),
            Name = isBuiltIn ? builtInType.ToString() : variableNode.GetNonBuiltInTypeName(),
            DisplayName = isBuiltIn ? builtInType.ToString() : variableNode.GetNonBuiltInTypeName(),
            IdentifierType = variableNode.DataType.IdType.GetIdentifierTypeName(),
            Identifier = variableNode.DataType.GetIdentifierAsString(),
            Kind = isBuiltIn ? OpcUaTypeKind.Primitive : OpcUaTypeKind.Unknown,
            IsArray = variableNode.ArrayDimensions.Count > 0,
        };
    }

    public static DotNetTypeDescriptor CreateBasicDotNetTypeDescriptor(this VariableNode variableNode)
    {
        ArgumentNullException.ThrowIfNull(variableNode);

        var builtInType = TypeInfo.GetBuiltInType(variableNode.DataType);
        var isBuiltIn = builtInType != BuiltInType.Null;
        var isArray = variableNode.ArrayDimensions.Count > 0;

        var clrType = OpcUaToDotNetDataTypeMapper.GetSystemTypeAsString(
            variableNode.DataType,
            variableNode.ArrayDimensions);

        if (!isArray)
        {
            return new DotNetTypeDescriptor
            {
                Kind = isBuiltIn ? DotNetTypeKind.Primitive : DotNetTypeKind.Unknown,
                Name = isBuiltIn ? builtInType.ToString() : variableNode.GetNonBuiltInTypeName(),
                ClrTypeName = clrType,
            };
        }

        var elementClrType = isBuiltIn
            ? TypeInfo.GetSystemType(builtInType, ValueRanks.Scalar)?.BeautifyTypeName() ?? builtInType.ToString().ToLowerInvariant()
            : "object";

        return new DotNetTypeDescriptor
        {
            Kind = DotNetTypeKind.Array,
            Name = clrType,
            ClrTypeName = clrType,
            ArrayElementType = new DotNetTypeDescriptor
            {
                Kind = isBuiltIn ? DotNetTypeKind.Primitive : DotNetTypeKind.Unknown,
                Name = isBuiltIn ? builtInType.ToString() : variableNode.GetNonBuiltInTypeName(),
                ClrTypeName = elementClrType,
            },
        };
    }

    private static string GetNonBuiltInTypeName(this VariableNode variableNode)
    {
        var identifier = variableNode.DataType.GetIdentifierAsString();
        return string.IsNullOrEmpty(identifier) ? "N/A" : identifier;
    }
}