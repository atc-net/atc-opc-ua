namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Base type that captures the common metadata shared by every OPC UA node
/// in an address space.
/// </summary>
/// <remarks>
/// • The class is <see langword="abstract"/>; derive from it for concrete node kinds
///   (Variable, Object, Method, …).
/// • All string properties are initialised with non‑<see langword="null"/> defaults so that
///   logging and <see langword="null"/> checks never throw a <see cref="NullReferenceException"/>.
/// • <see cref="NodeClass"/> is <see langword="init"/>‑only because it is an
///   intrinsic characteristic of a node and should not change after construction.
/// </remarks>
public abstract class NodeBase
{
    /// <summary>
    /// Gets or sets the identifier of the logical parent of this node.
    /// Defaults to <see cref="Constants.UnknownNodeId"/> when the parent
    /// is unknown or the node is the root of a hierarchy.
    /// </summary>
    public string ParentNodeId { get; set; } = Constants.UnknownNodeId;

    /// <summary>
    /// Gets or sets the fully‑qualified node‑id (e.g. <c>"ns=2;i=1234"</c>).
    /// This value is unique within the server's namespace.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier part of <see cref="NodeId"/>
    /// (everything after the namespace index).
    /// </summary>
    /// <example>
    /// For the node‑id <c>ns=2;i=1234</c>, the <c>NodeIdentifier</c> is <c>"1234"</c>.
    /// </example>
    public string NodeIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets the OPC UA <c>NodeClass</c> of this node (Variable, Object, Method, …).
    /// The value is supplied by the derived class's constructor and is therefore
    /// immutable after initialisation.
    /// </summary>
    public NodeClassType NodeClass { get; init; }

    /// <summary>
    /// Gets or sets the localised display name shown in UA clients.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the collection of <see cref="NodeVariable"/> children that
    /// belong to this node.
    /// The returned list instance is never <see langword="null"/>; add or
    /// remove items directly through it.
    /// </summary>
    public IList<NodeVariable> NodeVariables { get; } = new List<NodeVariable>();

    /// <inheritdoc/>
    public override string ToString() =>
        $"{nameof(ParentNodeId)}: {ParentNodeId}, " +
        $"{nameof(NodeId)}: {NodeId}, " +
        $"{nameof(NodeIdentifier)}: {NodeIdentifier}, " +
        $"{nameof(NodeClass)}: {NodeClass}, " +
        $"{nameof(DisplayName)}: {DisplayName}";
}