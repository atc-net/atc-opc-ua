namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Concrete node that represents an OPC UA <c>Object</c> in the address space.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NodeObject"/> can itself contain child <see cref="NodeObject"/> instances
/// (reflecting a hierarchical composition) and <see cref="NodeVariable"/> instances
/// (data variables exposed by the object).
/// </para>
/// <para>
/// The constructor sets <see cref="NodeClass"/> to <see cref="NodeClassType.Object"/>; the
/// value is therefore fixed for the lifetime of the instance.
/// </para>
/// </remarks>
public class NodeObject : NodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeObject" /> class and sets its
    /// <see cref="NodeClass"/> to <see cref="NodeClassType.Object"/>.
    /// </summary>
    public NodeObject()
    {
        NodeClass = NodeClassType.Object;
    }

    /// <summary>
    /// Gets the collection of child <see cref="NodeObject"/> instances that are
    /// compositionally contained within this object.
    /// The list is never <see langword="null"/>; add or remove items directly
    /// through it.
    /// </summary>
    public IList<NodeObject> NodeObjects { get; } = new List<NodeObject>();

    /// <summary>
    /// Returns a short, single‑line diagnostic string that omits the
    /// identifying information already available in <see cref="NodeBase"/>.
    /// </summary>
    /// <returns>A string of a simple version of the <see langword="ToString()" /></returns>
    public string ToStringSimple() =>
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(NodeObjects)}.Count: {NodeObjects.Count}, " +
        $"{nameof(NodeVariables)}.Count: {NodeVariables.Count}";

    /// <inheritdoc/>
    public override string ToString() =>
        $"{base.ToString()}, " +
        $"{nameof(NodeObjects)}.Count: {NodeObjects.Count}, " +
        $"{nameof(NodeVariables)}.Count: {NodeVariables.Count}";
}