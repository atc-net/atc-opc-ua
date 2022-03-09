namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class ObjectNodeSettings : SingleNodeSettings
{
    [CommandOption("--includeObjects")]
    [Description("Indicates if child objects should be included.")]
    [DefaultValue(false)]
    public bool IncludeObjects { get; init; }

    [CommandOption("--includeVariables")]
    [Description("Indicates if child variables should be included.")]
    [DefaultValue(false)]
    public bool IncludeVariables { get; init; }

    [CommandOption("--nodeObjectReadDepth")]
    [Description("Sets the max depth for object hierarchy retrieval. Default set to 1.")]
    [DefaultValue(1)]
    public int NodeObjectReadDepth { get; init; }
}