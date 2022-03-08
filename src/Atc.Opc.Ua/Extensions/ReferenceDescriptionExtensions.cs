// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Extensions;

public static class ReferenceDescriptionExtensions
{
    public static NodeObject? MapToNodeObject(
        this ReferenceDescription? node,
        string parentNodeId)
    {
        if (node is null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(parentNodeId))
        {
            return null;
        }

        return new NodeObject
        {
            ParentNodeId = parentNodeId,
            NodeId = node.NodeId.ToString(),
            NodeIdentifier = node.NodeId.Identifier.ToString()!,
            NodeClass = Enum<NodeClassType>.Parse(node.NodeClass.ToString()),
            DisplayName = node.DisplayName.Text,
        };
    }
}