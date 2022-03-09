namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class MultiNodeSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--nodeId <NODEID>")]
    [Description("OPC UA NodeIds")]
    public string[] NodeIds { get; init; } = Array.Empty<string>();

    public override ValidationResult Validate()
    {
        var validationResult = base.Validate();
        return !validationResult.Successful
            ? validationResult
            : !NodeIds.Any()
                ? ValidationResult.Error("NodeIds are missing.")
                : ValidationResult.Success();
    }
}