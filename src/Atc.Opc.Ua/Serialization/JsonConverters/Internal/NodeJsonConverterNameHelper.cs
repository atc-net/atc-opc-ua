namespace Atc.Opc.Ua.Serialization.JsonConverters.Internal;

internal sealed class NodeJsonConverterNameHelper
{
    private readonly Func<string, string> converter;

    private NodeJsonConverterNameHelper(Func<string, string> conv)
        => converter = conv;

    public static NodeJsonConverterNameHelper For(JsonNamingPolicy? p)
        => new(s => p?.ConvertName(s) ?? s);

    public string ParentNodeId => converter(nameof(NodeBase.ParentNodeId));

    public string NodeId => converter(nameof(NodeBase.NodeId));

    public string NodeIdentifier => converter(nameof(NodeBase.NodeIdentifier));

    public string NodeClass => converter(nameof(NodeBase.NodeClass));

    public string DisplayName => converter(nameof(NodeBase.DisplayName));

    public string NodeObjects => converter(nameof(NodeObject.NodeObjects));

    public string NodeVariables => converter(nameof(NodeBase.NodeVariables));

    public string DataTypeDotnet => converter(nameof(NodeVariable.DataTypeDotnet));

    public string SampleValue => converter(nameof(NodeVariable.SampleValue));

    public string DataTypeOpcUa => converter(nameof(NodeVariable.DataTypeOpcUa));

    public string OpUaName => converter(nameof(OpUaDataType.Name));

    public string OpUaIsArray => converter(nameof(OpUaDataType.IsArray));
}
