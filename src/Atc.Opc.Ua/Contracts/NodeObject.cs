namespace Atc.Opc.Ua.Contracts;

public class NodeObject : NodeBase
{
    public NodeObject()
    {
        NodeClass = NodeClassType.Object;
    }

    public IList<NodeObject> NodeObjects { get; } = new List<NodeObject>();

    public IList<NodeVariable> NodeVariables { get; } = new List<NodeVariable>();

    public string ToStringSimple()
        => $"{nameof(DisplayName)}: {DisplayName}, {nameof(NodeObjects)}.Count: {NodeObjects.Count}, {nameof(NodeVariables)}.Count: {NodeVariables.Count}";

    public override string ToString()
        => $"{base.ToString()}, {nameof(NodeObjects)}.Count: {NodeObjects.Count}, {nameof(NodeVariables)}.Count: {NodeVariables.Count}";
}