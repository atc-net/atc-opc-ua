namespace Atc.Opc.Ua.Protocol.Mappers;

public static class OpcUaToDotNetDataTypeMapper
{
    public static string GetSystemTypeAsString(
        NodeId nodeId,
        IList<uint> arrayDimensions)
    {
        ArgumentNullException.ThrowIfNull(nodeId);
        ArgumentNullException.ThrowIfNull(arrayDimensions);

        var systemType = arrayDimensions.Count > 0
            ? TypeInfo.GetSystemType(TypeInfo.GetBuiltInType(nodeId), arrayDimensions.Count)
            : TypeInfo.GetSystemType(nodeId, new EncodeableFactory());

        return systemType.BeautifyTypeName();
    }
}