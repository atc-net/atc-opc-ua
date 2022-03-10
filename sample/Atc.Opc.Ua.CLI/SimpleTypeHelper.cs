// ReSharper disable InvertIf
namespace Atc.Opc.Ua.CLI;

public static class SimpleTypeHelper
{
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "OK.")]
    [SuppressMessage("Minor Code Smell", "S2386:Mutable fields should not be \"public static\"", Justification = "OK.")]
    [SuppressMessage("Minor Bug", "S3887:Mutable, non-private fields should not be \"readonly\"", Justification = "OK.")]
    [SuppressMessage("Design", "MA0016:Prefer returning collection abstraction instead of implementation", Justification = "OK.")]
    [SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "OK.")]
    public static readonly List<Tuple<Type, string>> DotnetSimpleTypeLookup = new List<Tuple<Type, string>>
    {
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
        Tuple.Create(typeof(int), "Int16"),
        Tuple.Create(typeof(int), "Int32"),
        Tuple.Create(typeof(int), "integer"),
        Tuple.Create(typeof(long), "long"),
        Tuple.Create(typeof(long), "Int64"),
        Tuple.Create(typeof(object), "object"),
        Tuple.Create(typeof(sbyte), "sbyte"),
        Tuple.Create(typeof(short), "short"),
        Tuple.Create(typeof(string), "string"),
        Tuple.Create(typeof(uint), "uint"),
        Tuple.Create(typeof(uint), "UInt16"),
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
        Tuple.Create(typeof(int?), "Int16?"),
        Tuple.Create(typeof(int?), "Int32?"),
        Tuple.Create(typeof(int?), "integer?"),
        Tuple.Create(typeof(long?), "long?"),
        Tuple.Create(typeof(long?), "Int64?"),
        Tuple.Create(typeof(sbyte?), "sbyte?"),
        Tuple.Create(typeof(short?), "short?"),
        Tuple.Create(typeof(uint?), "uint?"),
        Tuple.Create(typeof(uint?), "UInt16?"),
        Tuple.Create(typeof(uint?), "UInt32?"),
        Tuple.Create(typeof(ulong?), "ulong?"),
        Tuple.Create(typeof(ulong?), "Uint64?"),
        Tuple.Create(typeof(ushort?), "ushort?"),
    };

    public static bool TryGetTypeByName(
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

        var item = DotnetSimpleTypeLookup.Find(x => x.Item2.Equals(value, StringComparison.OrdinalIgnoreCase));
        if (item?.Item1 is null)
        {
            return false;
        }

        type = item.Item1;
        return true;
    }

    public static Type GetTypeByName(
        string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (TryGetTypeByName(value, out var result))
        {
            return result!;
        }

        throw new NotSupportedException($"Value is not supported: {value}");
    }
}