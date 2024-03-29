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
                Name = TypeInfo.GetBuiltInType(variableNode.DataType).ToString(),
                IsArray = variableNode.ArrayDimensions.Count > 0,
            };

            data.DataTypeDotnet = OpcUaToDotNetDataTypeMapper.GetSystemTypeAsString(
                variableNode.DataType,
                variableNode.ArrayDimensions);
        }

        return data;
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