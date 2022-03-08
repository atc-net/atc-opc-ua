namespace Atc.Opc.Ua.Contracts;

public class OpUaDataType
{
    public string Name { get; set; } = string.Empty;

    public bool IsArray { get; set; }

    public override string ToString()
        => $"{nameof(Name)}: {Name}, {nameof(IsArray)}: {IsArray}";
}