namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class SingleNodeSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--nodeId <NODEID>")]
    [Description("OPC UA NodeId")]
    public string NodeId { get; init; } = string.Empty;

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