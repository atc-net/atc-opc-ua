namespace Atc.Opc.Ua.Extensions;

public static class IdTypeExtensions
{
    public static string GetIdentifierTypeName(this IdType idType) => idType switch
    {
        IdType.Numeric => "Numeric",
        IdType.String => "String",
        IdType.Guid => "Guid",
        IdType.Opaque => "Opaque",
        _ => "Unknown",
    };
}