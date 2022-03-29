namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class MultiNodeCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--node-id <NODE-ID>")]
    [Description("OPC UA NodeIds")]
    public string[] NodeIds { get; init; } = Array.Empty<string>();

    [CommandOption("--include-sample-values")]
    [Description("Indicates if sample values for the nodes should be included.")]
    [DefaultValue(false)]
    public bool IncludeSampleValues { get; init; }

    public override ValidationResult Validate()
    {
        var validationResult = base.Validate();
        if (!validationResult.Successful)
        {
            return validationResult;
        }

        return !NodeIds.Any()
            ? ValidationResult.Error("NodeIds are missing.")
            : ValidationResult.Success();
    }
}