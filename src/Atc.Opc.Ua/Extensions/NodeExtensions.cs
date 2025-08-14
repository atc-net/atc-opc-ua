// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Extensions;

public static class NodeExtensions
{
    public static NodeVariable? MapToNodeVariable(
        this Node? node,
        string? parentNodeId)
    {
        if (node is null)
        {
            return null;
        }

        var data = new NodeVariable
        {
            NodeId = node.NodeId.ToString(),
            NodeIdentifier = node.NodeId.Identifier.ToString()!,
            NodeClass = Enum<NodeClassType>.Parse(node.NodeClass.ToString()),
            DisplayName = node.DisplayName.Text,
        };

        if (!string.IsNullOrEmpty(parentNodeId))
        {
            data.ParentNodeId = parentNodeId;
        }

        if (node.DataLock is VariableNode variableNode)
        {
            data.DataTypeOpcUa = new OpUaDataType
            {
                Name = GetOpcUaDataTypeName(variableNode),
                IsArray = variableNode.ArrayDimensions.Count > 0,
            };

            data.DataTypeDotnet = OpcUaToDotNetDataTypeMapper.GetSystemTypeAsString(
                variableNode.DataType,
                variableNode.ArrayDimensions);
        }

        return data;
    }

    private static string GetOpcUaDataTypeName(VariableNode variableNode)
    {
        var builtInType = TypeInfo.GetBuiltInType(variableNode.DataType);

        if (builtInType != BuiltInType.Null)
        {
            return builtInType.ToString();
        }

        try
        {
            return variableNode.DataType.IdType switch
            {
                IdType.Numeric or IdType.String or IdType.Guid =>
                    variableNode.DataType.Identifier?.ToString() is { } str
                        ? (str.Length > 1 && str[0] == '"' && str[^1] == '"'
                            ? str[1..^1]
                            : str)
                        : "N/A",

                IdType.Opaque =>
                    variableNode.DataType.Identifier is byte[] bytes
                        ? BitConverter.ToString(bytes)
                        : "N/A",

                _ => throw new SwitchCaseDefaultException(variableNode.DataType.IdType),
            };
        }
        catch
        {
            return "N/A";
        }
    }

    public static NodeVariable? MapToNodeVariableWithValue(
        this Node? node,
        string? parentNodeId,
        DataValue? nodeDataValue)
    {
        if (node is null)
        {
            return null;
        }

        var data = MapToNodeVariable(node, parentNodeId);
        if (data is not null && nodeDataValue is not null)
        {
            data.SampleValue = nodeDataValue.ToString();
        }

        return data;
    }

    public static NodeObject? MapToNodeObject(
        this Node? node,
        string? parentNodeId)
    {
        if (node is null)
        {
            return null;
        }

        var result = new NodeObject
        {
            NodeId = node.NodeId.ToString(),
            NodeIdentifier = node.NodeId.Identifier.ToString()!,
            NodeClass = Enum<NodeClassType>.Parse(node.NodeClass.ToString()),
            DisplayName = node.DisplayName.Text,
        };

        if (!string.IsNullOrEmpty(parentNodeId))
        {
            result.ParentNodeId = parentNodeId;
        }

        return result;
    }
}