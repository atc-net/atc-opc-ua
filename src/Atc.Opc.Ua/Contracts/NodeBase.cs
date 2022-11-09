namespace Atc.Opc.Ua.Contracts;

public abstract class NodeBase
{
    /// <summary>
    /// Parent node id.
    /// </summary>
    public string ParentNodeId { get; set; } = Constants.UnknownNodeId;

    /// <summary>
    /// Id of node.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// The node identifier.
    /// </summary>
    public string NodeIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// The nodeClass of node.
    /// </summary>
    public NodeClassType NodeClass { get; init; }

    /// <summary>
    /// Display name of node.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public override string ToString()
        => $"{nameof(ParentNodeId)}: {ParentNodeId}, {nameof(NodeId)}: {NodeId}, {nameof(NodeIdentifier)}: {NodeIdentifier}, {nameof(NodeClass)}: {NodeClass}, {nameof(DisplayName)}: {DisplayName}";
}