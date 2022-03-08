namespace Atc.Opc.Ua.Contracts;

public class NodeVariable : NodeBase
{
    public NodeVariable()
    {
        NodeClass = NodeClassType.Variable;
    }

    public OpUaDataType? DataTypeOpcUa { get; set; }

    public string DataTypeDotnet { get; set; } = string.Empty;

    public string SampleValue { get; set; } = string.Empty;

    public string ToStringSimple()
        => $"{nameof(DisplayName)}: {DisplayName}, {nameof(DataTypeDotnet)}: {DataTypeDotnet}, {nameof(SampleValue)}: {SampleValue}";

    public override string ToString()
        => $"{base.ToString()}, {nameof(DataTypeOpcUa)}: {DataTypeOpcUa},  {nameof(DataTypeDotnet)}: {DataTypeDotnet}, {nameof(SampleValue)}: {SampleValue}";
}