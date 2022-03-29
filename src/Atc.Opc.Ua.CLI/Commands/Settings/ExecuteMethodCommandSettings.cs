namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class ExecuteMethodCommandSettings : OpcUaBaseCommandSettings
{
    [CommandOption("--method-node-id <METHOD-NODE-ID>")]
    [Description("OPC UA NodeId")]
    public string MethodNodeId { get; init; } = string.Empty;

    [CommandOption("--parent-node-id <PARENT-NODE-ID>")]
    [Description("OPC UA ParentNodeId")]
    public string ParentNodeId { get; init; } = string.Empty;

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

        //// TODO: Expand

        return ValidationResult.Success();
    }
}