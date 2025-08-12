namespace Atc.Opc.Ua.Options;

public sealed class OpcUaScannerOptions
{
    public string StartingNodeId { get; set; } = ObjectIds.ObjectsFolder.ToString();

    public int ObjectDepth { get; set; } = 100;

    public int VariableDepth { get; set; } = 100;

    public bool IncludeSampleValues { get; set; }

    public IList<string> IncludeObjectNodeIds { get; } = new List<string>();

    public IList<string> ExcludeObjectNodeIds { get; } = new List<string>();

    public IList<string> IncludeVariableNodeIds { get; } = new List<string>();

    public IList<string> ExcludeVariableNodeIds { get; } = new List<string>();
}
