namespace Atc.Opc.Ua.CLI.Commands.Settings;

public sealed class ReadDataTypeCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--node-id <NODE-ID>")]
    [Description("OPC UA DataType NodeId")]
    public string NodeId { get; init; } = string.Empty;

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