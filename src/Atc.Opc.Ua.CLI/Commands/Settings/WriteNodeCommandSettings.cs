// ReSharper disable ConvertIfStatementToReturnStatement
namespace Atc.Opc.Ua.CLI.Commands.Settings;

public sealed class WriteNodeCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("-n|--node-id <NODE-ID>")]
    [Description("OPC UA NodeId")]
    public string NodeId { get; init; } = string.Empty;

    [CommandOption("--data-type <DATA-TYPE>")]
    [Description("OPC UA DataType")]
    public string DataType { get; set; } = string.Empty;

    [CommandOption("--data-value <DATA-VALUE>")]
    [Description("OPC UA DataValue")]
    public string Value { get; init; } = string.Empty;

    public override ValidationResult Validate()
    {
        var validationResult = base.Validate();
        if (!validationResult.Successful)
        {
            return validationResult;
        }

        if (string.IsNullOrWhiteSpace(NodeId))
        {
            return ValidationResult.Error("NodeId is missing.");
        }

        if (string.IsNullOrWhiteSpace(DataType))
        {
            return ValidationResult.Error("DataType is missing.");
        }

        if (string.IsNullOrWhiteSpace(Value))
        {
            return ValidationResult.Error("DataValue is missing.");
        }

        var dataTypeResolver = new DataTypeResolver();
        if (!dataTypeResolver.TryGetTypeByName(DataType, out _))
        {
            return ValidationResult.Error("DataType is not supported.");
        }

        return ValidationResult.Success();
    }
}