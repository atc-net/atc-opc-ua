namespace Atc.Opc.Ua.CLI.Commands.Settings;

public class OpcUaBaseCommandSettings : BaseCommandSettings
{
    [CommandOption("-s|--serverUrl <SERVERURL>")]
    [Description("OPC UA Server Url")]
    public string? ServerUrl { get; init; }

    [CommandOption("-u|--userName [USERNAME]")]
    [Description("OPC UA UserName")]
    public FlagValue<string>? UserName { get; init; }

    [CommandOption("-p|--password [PASSWORD]")]
    [Description("OPC UA Password")]
    public FlagValue<string>? Password { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl) ||
            !ServerUrl.StartsWith("opc.tcp://", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Error("ServerUrl must be present and begin with opc.tcp://.");
        }

        if ((UserName is not null && UserName.IsSet && Password is not null && !Password.IsSet) ||
            (UserName is not null && !UserName.IsSet && Password is not null && Password.IsSet))
        {
            return ValidationResult.Error("Both UserName and Password must be set.");
        }

        return ValidationResult.Success();
    }
}