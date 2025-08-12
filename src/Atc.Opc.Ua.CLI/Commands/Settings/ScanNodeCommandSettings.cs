namespace Atc.Opc.Ua.CLI.Commands.Settings;

public sealed class ScanNodeCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("--starting-node-id <STARTING-NODE-ID>")]
    [Description("OPC UA starting NodeId (defaults to ObjectsFolder).")]
    public string? StartingNodeId { get; init; }

    [CommandOption("--object-depth")]
    [Description("Maximum depth for object hierarchy (default 1). 0 means only starting object.")]
    [DefaultValue(1)]
    public int ObjectDepth { get; init; } = 1;

    [CommandOption("--variable-depth")]
    [Description("Maximum depth for variable hierarchy (default 0). 0 means only direct variables of each object.")]
    [DefaultValue(0)]
    public int VariableDepth { get; init; } = 0;

    [CommandOption("--include-sample-values")]
    [Description("Indicates if sample values for variables should be included.")]
    [DefaultValue(false)]
    public bool IncludeSampleValues { get; init; }

    [CommandOption("--include-object-node-id <NODE-ID>")]
    [Description("Object NodeId to include. May be specified multiple times.")]
    public string[] IncludeObjectNodeIds { get; init; } = [];

    [CommandOption("--exclude-object-node-id <NODE-ID>")]
    [Description("Object NodeId to exclude. May be specified multiple times.")]
    public string[] ExcludeObjectNodeIds { get; init; } = [];

    [CommandOption("--include-variable-node-id <NODE-ID>")]
    [Description("Variable NodeId to include. May be specified multiple times.")]
    public string[] IncludeVariableNodeIds { get; init; } = [];

    [CommandOption("--exclude-variable-node-id <NODE-ID>")]
    [Description("Variable NodeId to exclude. May be specified multiple times.")]
    public string[] ExcludeVariableNodeIds { get; init; } = [];
}
