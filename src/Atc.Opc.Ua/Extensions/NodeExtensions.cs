// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Extensions;

public static class NodeExtensions
{
    /// <summary>
    /// Maps an OPC UA Node to a NodeVariable with pre-resolved DataType information.
    /// </summary>
    /// <param name="node">The OPC UA node.</param>
    /// <param name="parentNodeId">The parent node ID.</param>
    /// <param name="resolvedTypeInfo">Pre-resolved type information from DataTypeInfoResolver.</param>
    /// <returns>A NodeVariable with rich type information, or null if node is null.</returns>
    public static NodeVariable? MapToNodeVariable(
        this Node? node,
        string? parentNodeId,
        ResolvedDataTypeInfo? resolvedTypeInfo)
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

        if (resolvedTypeInfo is not null)
        {
            data.DataTypeOpcUa = resolvedTypeInfo.OpcUaDataType;
            data.DataTypeDotnet = resolvedTypeInfo.DotNetType;
        }
        else if (node.DataLock is VariableNode variableNode)
        {
            // Fallback for backward compatibility when resolver is not used
            data.DataTypeOpcUa = variableNode.CreateBasicOpcUaDataType();
            data.DataTypeDotnet = variableNode.CreateBasicDotNetTypeDescriptor();
        }

        return data;
    }

    /// <summary>
    /// Maps an OPC UA Node to a NodeVariable without resolved DataType information.
    /// </summary>
    /// <param name="node">The OPC UA node.</param>
    /// <param name="parentNodeId">The parent node ID.</param>
    /// <returns>A NodeVariable with basic type information, or null if node is null.</returns>
    public static NodeVariable? MapToNodeVariable(
        this Node? node,
        string? parentNodeId)
        => MapToNodeVariable(node, parentNodeId, resolvedTypeInfo: null);

    public static NodeVariable? MapToNodeVariableWithValue(
        this Node? node,
        string? parentNodeId,
        DataValue? nodeDataValue,
        ResolvedDataTypeInfo? resolvedTypeInfo = null)
    {
        if (node is null)
        {
            return null;
        }

        var data = MapToNodeVariable(node, parentNodeId, resolvedTypeInfo);
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
