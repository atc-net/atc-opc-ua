namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class ExecuteMethodCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("--parent-node-id <PARENT-NODE-ID>")]
    [Description("OPC UA ParentNodeId")]
    public string ParentNodeId { get; init; } = string.Empty;

    [CommandOption("--method-node-id <METHOD-NODE-ID>")]
    [Description("OPC UA NodeId")]
    public string MethodNodeId { get; init; } = string.Empty;

    [CommandOption("--data-types <DATA-TYPES>")]
    [Description("OPC UA Argument Data Types")]
    public string[] DataTypes { get; init; } = Array.Empty<string>();

    [CommandOption("--data-values <DATA-VALUES>")]
    [Description("OPC UA Argument Data Values")]
    public string[] DataValues { get; init; } = Array.Empty<string>();

    public override ValidationResult Validate()
    {
        var validationResult = base.Validate();
        if (!validationResult.Successful)
        {
            return validationResult;
        }

        if (string.IsNullOrEmpty(ParentNodeId))
        {
            return ValidationResult.Error("--parent-node-id is not set.");
        }

        if (string.IsNullOrEmpty(MethodNodeId))
        {
            return ValidationResult.Error("--method-node-id is not set.");
        }

        var validationError = OpcUaValidationHelper.GetErrorForArgumentData(
            "data-types",
            "data-values",
            DataTypes,
            DataValues);

        return validationError is not null
            ? ValidationResult.Error(validationError)
            : ValidationResult.Success();
    }
}