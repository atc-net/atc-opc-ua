namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadDataTypeSingleCommand : AsyncCommand<ReadDataTypeCommandSettings>
{
    private readonly IOpcUaClient opcUaClient;
    private readonly ILogger<NodeReadDataTypeSingleCommand> logger;

    public NodeReadDataTypeSingleCommand(
        IOpcUaClient opcUaClient,
        ILogger<NodeReadDataTypeSingleCommand> logger)
    {
        this.opcUaClient = opcUaClient ?? throw new ArgumentNullException(nameof(opcUaClient));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ReadDataTypeCommandSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings);
    }

    private async Task<int> ExecuteInternalAsync(ReadDataTypeCommandSettings settings)
    {
        ConsoleHelper.WriteHeader();

        var serverUrl = new Uri(settings.ServerUrl!);
        var userName = settings.UserName;
        var password = settings.Password;
        var nodeId = settings.NodeId;

        var sw = Stopwatch.StartNew();

        var (succeeded, _) = userName is not null && userName.IsSet
            ? await opcUaClient.ConnectAsync(serverUrl, userName.Value, password!.Value, CancellationToken.None)
            : await opcUaClient.ConnectAsync(serverUrl, CancellationToken.None);

        if (succeeded)
        {
            var (succeededReading, enumDataType, errorMessage) = await opcUaClient.ReadEnumDataTypeAsync(
                nodeId,
                CancellationToken.None);

            if (succeededReading && enumDataType is not null)
            {
                RenderEnumDataType(enumDataType);
            }
            else
            {
                logger.LogError("Failed to read enum DataType: {ErrorMessage}", errorMessage);
            }

            await opcUaClient.DisconnectAsync(CancellationToken.None);
        }

        sw.Stop();
        logger.LogDebug($"Time for operation: {sw.Elapsed.GetPrettyTime()}");

        return succeeded
            ? ConsoleExitStatusCodes.Success
            : ConsoleExitStatusCodes.Failure;
    }

    private void RenderEnumDataType(OpcUaEnumDataType enumDataType)
    {
        logger.LogInformation("Enum DataType: {Name} ({NodeId})", enumDataType.Name, enumDataType.NodeId);
        logger.LogInformation("HasEnumValues: {HasEnumValues}", enumDataType.HasEnumValues);

        if (!string.IsNullOrEmpty(enumDataType.Description))
        {
            logger.LogInformation("Description: {Description}", enumDataType.Description);
        }

        logger.LogInformation("Members ({Count}):", enumDataType.Members.Count);

        var table = new Table();
        table.AddColumn("Value");
        table.AddColumn("Name");
        table.AddColumn("DisplayName");
        table.AddColumn("Description");

        foreach (var member in enumDataType.Members.OrderBy(x => x.Value))
        {
            table.AddRow(
                member.Value.ToString(CultureInfo.InvariantCulture),
                member.Name,
                member.DisplayName,
                member.Description);
        }

        AnsiConsole.Write(table);
    }
}