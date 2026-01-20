namespace Atc.Opc.Ua.CLI.Commands.Settings;

public sealed class ReadDataTypesCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--node-id <NODE-ID>")]
    [Description("OPC UA DataType NodeIds")]
    public string[] NodeIds { get; init; } = [];

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