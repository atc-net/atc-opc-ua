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

    // OpUaDataType properties
    public string OpUaNodeId => converter(nameof(OpUaDataType.NodeId));

    public string OpUaName => converter(nameof(OpUaDataType.Name));

    public string OpUaDisplayName => converter(nameof(OpUaDataType.DisplayName));

    public string OpUaIdentifierType => converter(nameof(OpUaDataType.IdentifierType));

    public string OpUaKind => converter(nameof(OpUaDataType.Kind));

    public string OpUaIsArray => converter(nameof(OpUaDataType.IsArray));

    // DotNetTypeDescriptor properties
    public string DotNetKind => converter(nameof(DotNetTypeDescriptor.Kind));

    public string DotNetName => converter(nameof(DotNetTypeDescriptor.Name));

    public string DotNetClrTypeName => converter(nameof(DotNetTypeDescriptor.ClrTypeName));

    public string DotNetArrayElementType => converter(nameof(DotNetTypeDescriptor.ArrayElementType));

    public string DotNetEnumMembers => converter(nameof(DotNetTypeDescriptor.EnumMembers));

    public string DotNetStructureFields => converter(nameof(DotNetTypeDescriptor.StructureFields));

    // DotNetEnumMember properties
    public string EnumMemberValue => converter(nameof(DotNetEnumMember.Value));

    public string EnumMemberName => converter(nameof(DotNetEnumMember.Name));

    public string EnumMemberDisplayName => converter(nameof(DotNetEnumMember.DisplayName));

    // DotNetFieldDescriptor properties
    public string FieldName => converter(nameof(DotNetFieldDescriptor.Name));

    public string FieldType => converter(nameof(DotNetFieldDescriptor.Type));

    public string FieldIsOptional => converter(nameof(DotNetFieldDescriptor.IsOptional));
}