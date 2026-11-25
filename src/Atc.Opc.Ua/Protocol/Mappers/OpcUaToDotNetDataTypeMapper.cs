namespace Atc.Opc.Ua.Protocol.Mappers;

public static class OpcUaToDotNetDataTypeMapper
{
    public static string GetSystemTypeAsString(
        NodeId nodeId,
        IList<uint> arrayDimensions)
    {
        ArgumentNullException.ThrowIfNull(arrayDimensions);

        return arrayDimensions.Count > 0
            ? StandardDataTypes.GetTypeName(StandardDataTypes.GetBuiltInType(nodeId), arrayDimensions.Count)
            : StandardDataTypes.GetScalarTypeName(nodeId);
    }
}