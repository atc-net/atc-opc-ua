namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class SingleNodeSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--nodeId <NODEID>")]
    [Description("OPC UA NodeId")]
    public string NodeId { get; init; } = string.Empty;

    [CommandOption("--includeSampleValue")]
    [Description("Indicates if sample value for the node should be included.")]
    [DefaultValue(false)]
    public bool IncludeSampleValue { get; init; }

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