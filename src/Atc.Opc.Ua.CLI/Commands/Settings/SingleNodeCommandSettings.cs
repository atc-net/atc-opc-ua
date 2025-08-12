namespace Atc.Opc.Ua.CLI.Commands.Settings;

public sealed class SingleNodeCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--node-id <NODE-ID>")]
    [Description("OPC UA NodeId")]
    public string NodeId { get; init; } = string.Empty;

    [CommandOption("--include-sample-value")]
    [Description("Indicates if sample value for the node should be included.")]
    [DefaultValue(false)]
    public bool IncludeSampleValue { get; init; }

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