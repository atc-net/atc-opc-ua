namespace Atc.Opc.Ua.Resolvers;

public sealed class DataTypeResolver : IDataTypeResolver
{
    private readonly List<Tuple<Type, string>> dotnetSimpleTypeLookup =
    [
        Tuple.Create(typeof(bool), "bool"),
        Tuple.Create(typeof(bool), "bool"),
        Tuple.Create(typeof(bool), "Boolean"),
        Tuple.Create(typeof(byte), "byte"),
        Tuple.Create(typeof(char), "char"),
        Tuple.Create(typeof(DateTime), "DateTime"),
        Tuple.Create(typeof(DateTimeOffset), "DateTimeOffset"),
        Tuple.Create(typeof(decimal), "decimal"),
        Tuple.Create(typeof(double), "double"),
        Tuple.Create(typeof(float), "float"),
        Tuple.Create(typeof(float), "single"),
        Tuple.Create(typeof(Half), "half"),
        Tuple.Create(typeof(Guid), "Guid"),
        Tuple.Create(typeof(int), "int"),
        Tuple.Create(typeof(short), "Int16"),
        Tuple.Create(typeof(int), "Int32"),
        Tuple.Create(typeof(int), "integer"),
        Tuple.Create(typeof(long), "long"),
        Tuple.Create(typeof(long), "Int64"),
        Tuple.Create(typeof(object), "object"),
        Tuple.Create(typeof(sbyte), "sbyte"),
        Tuple.Create(typeof(short), "short"),
        Tuple.Create(typeof(string), "string"),
        Tuple.Create(typeof(uint), "uint"),
        Tuple.Create(typeof(ushort), "UInt16"),
        Tuple.Create(typeof(uint), "UInt32"),
        Tuple.Create(typeof(ulong), "ulong"),
        Tuple.Create(typeof(ulong), "UInt64"),
        Tuple.Create(typeof(ushort), "ushort"),
        Tuple.Create(typeof(void), "void"),
        Tuple.Create(typeof(bool?), "bool?"),
        Tuple.Create(typeof(bool?), "Boolean?"),
        Tuple.Create(typeof(byte?), "byte?"),
        Tuple.Create(typeof(char?), "char?"),
        Tuple.Create(typeof(DateTime?), "DateTime?"),
        Tuple.Create(typeof(DateTimeOffset?), "DateTimeOffset?"),
        Tuple.Create(typeof(decimal?), "decimal?"),
        Tuple.Create(typeof(double?), "double?"),
        Tuple.Create(typeof(float?), "float?"),
        Tuple.Create(typeof(float?), "single?"),
        Tuple.Create(typeof(Half), "half?"),
        Tuple.Create(typeof(Guid?), "Guid?"),
        Tuple.Create(typeof(int?), "int?"),
        Tuple.Create(typeof(short?), "Int16?"),
        Tuple.Create(typeof(int?), "Int32?"),
        Tuple.Create(typeof(int?), "integer?"),
        Tuple.Create(typeof(long?), "long?"),
        Tuple.Create(typeof(long?), "Int64?"),
        Tuple.Create(typeof(sbyte?), "sbyte?"),
        Tuple.Create(typeof(short?), "short?"),
        Tuple.Create(typeof(uint?), "uint?"),
        Tuple.Create(typeof(ushort?), "UInt16?"),
        Tuple.Create(typeof(uint?), "UInt32?"),
        Tuple.Create(typeof(ulong?), "ulong?"),
        Tuple.Create(typeof(ulong?), "UInt64?"),
        Tuple.Create(typeof(ushort?), "ushort?"),
    ];

    public Type GetTypeByName(
        string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (TryGetTypeByName(value, out var result))
        {
            return result!;
        }

        throw new NotSupportedException($"Value is not supported: {value}");
    }

    public bool TryGetTypeByName(
        string? value,
        out Type? type)
    {
        type = null;

        if (value is null)
        {
            return false;
        }

        if (value.Contains('.', StringComparison.Ordinal))
        {
            value = value.Split('.', StringSplitOptions.RemoveEmptyEntries).Last();
        }

        var item = dotnetSimpleTypeLookup.Find(x => x.Item2.Equals(value, StringComparison.OrdinalIgnoreCase));
        if (item?.Item1 is null)
        {
            return false;
        }

        type = item.Item1;
        return true;
    }
}