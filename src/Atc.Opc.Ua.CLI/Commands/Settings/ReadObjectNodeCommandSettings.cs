namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class ReadObjectNodeCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--node-id <NODE-ID>")]
    [Description("OPC UA NodeId")]
    public string NodeId { get; init; } = string.Empty;

    [CommandOption("--include-objects")]
    [Description("Indicates if child objects should be included.")]
    [DefaultValue(false)]
    public bool IncludeObjects { get; init; }

    [CommandOption("--include-variables")]
    [Description("Indicates if child variables should be included.")]
    [DefaultValue(false)]
    public bool IncludeVariables { get; init; }

    [CommandOption("--include-sample-values")]
    [Description("Indicates if sample values for the node(s) should be included. Only Relevant if IncludeVariables is set.")]
    [DefaultValue(false)]
    public bool IncludeSampleValues { get; init; }

    [CommandOption("--node-object-read-depth")]
    [Description("Sets the max depth for object hierarchy retrieval. Default set to 1.")]
    [DefaultValue(1)]
    public int NodeObjectReadDepth { get; init; }

    [CommandOption("--node-variable-read-depth")]
    [Description("Sets the max depth for variable hierarchy retrieval. Default set to 0.")]
    [DefaultValue(0)]
    public int NodeVariableReadDepth { get; init; }

    public override ValidationResult Validate()
    {
        var validationResult = base.Validate();
        if (!validationResult.Successful)
        {
            return validationResult;
        }

        return string.IsNullOrWhiteSpace(NodeId)
            ? ValidationResult.Error("NodeId is missing.")
            : ValidationResult.Success();
    }
}