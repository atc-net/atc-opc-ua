namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class ObjectNodeSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--nodeId <NODEID>")]
    [Description("OPC UA NodeId")]
    public string NodeId { get; init; } = string.Empty;

    [CommandOption("--includeObjects")]
    [Description("Indicates if child objects should be included.")]
    [DefaultValue(false)]
    public bool IncludeObjects { get; init; }

    [CommandOption("--includeVariables")]
    [Description("Indicates if child variables should be included.")]
    [DefaultValue(false)]
    public bool IncludeVariables { get; init; }

    [CommandOption("--includeSampleValues")]
    [Description("Indicates if sample values for the node(s) should be included. Only Relevant if IncludeVariables is set.")]
    [DefaultValue(false)]
    public bool IncludeSampleValues { get; init; }

    [CommandOption("--nodeObjectReadDepth")]
    [Description("Sets the max depth for object hierarchy retrieval. Default set to 1.")]
    [DefaultValue(1)]
    public int NodeObjectReadDepth { get; init; }

    public override ValidationResult Validate()
    {
        var validationResult = base.Validate();
        return !validationResult.Successful
            ? validationResult
            : string.IsNullOrWhiteSpace(NodeId)
                ? ValidationResult.Error("NodeId is missing.")
                : ValidationResult.Success();
    }
}