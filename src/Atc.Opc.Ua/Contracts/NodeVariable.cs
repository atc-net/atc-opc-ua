namespace Atc.Opc.Ua.Contracts;

/// <summary>
/// Concrete node that represents an OPC UA <see langword="Variable"/> in the
/// address space.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NodeVariable"/> holds a value (or set of values) and exposes
/// its datatype both in OPC UA terms (<see cref="DataTypeOpcUa"/>) and as the
/// fully‑qualified .NET type name (<see cref="DataTypeDotnet"/>).
/// </para>
/// <para>
/// The constructor fixes <see cref="NodeClass"/> to
/// <see cref="NodeClassType.Variable"/> so the classification cannot be
/// changed after initialisation.
/// </para>
/// </remarks>
public class NodeVariable : NodeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NodeVariable"/> class and sets its
    /// <see cref="NodeClass"/> to <see cref="NodeClassType.Variable"/>.
    /// </summary>
    public NodeVariable()
    {
        NodeClass = NodeClassType.Variable;
    }

    /// <summary>
    /// Gets or sets the datatype of the variable expressed in OPC UA terms,
    /// or <see langword="null"/> if the datatype is unknown.
    /// </summary>
    public OpUaDataType? DataTypeOpcUa { get; set; }

    /// <summary>
    /// Gets or sets the .NET type descriptor that corresponds to
    /// <see cref="DataTypeOpcUa"/>.
    /// </summary>
    public DotNetTypeDescriptor? DataTypeDotnet { get; set; }

    /// <summary>
    /// Gets or sets a representative sample value encoded as a string.
    /// Clients can display or log this to illustrate the expected format.
    /// </summary>
    public string SampleValue { get; set; } = string.Empty;

    /// <summary>
    /// Returns a concise diagnostic string that focuses on the variable-specific
    /// details and omits the identifying information already provided by
    /// <see cref="NodeBase"/>.
    /// </summary>
    /// <returns>A string of a simple version of the <see langword="ToString()" /></returns>
    public string ToStringSimple() =>
        $"{nameof(DisplayName)}: {DisplayName}, " +
        $"{nameof(DataTypeDotnet)}: {DataTypeDotnet?.Name ?? "N/A"}, " +
        $"{nameof(SampleValue)}: {SampleValue}";

    /// <inheritdoc/>
    public override string ToString() =>
        $"{base.ToString()}, " +
        $"{nameof(DataTypeOpcUa)}: {DataTypeOpcUa}, " +
        $"{nameof(DataTypeDotnet)}: {DataTypeDotnet}, " +
        $"{nameof(SampleValue)}: {SampleValue}";
}