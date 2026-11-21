namespace Atc.Opc.Ua.CLI.Commands;

internal sealed class NodeReadDataTypeMultiCommand : AsyncCommand<ReadDataTypesCommandSettings>
{
    private readonly OpcUaClientFactory clientFactory;
    private readonly ILogger<NodeReadDataTypeMultiCommand> logger;

    public NodeReadDataTypeMultiCommand(
        OpcUaClientFactory clientFactory,
        ILogger<NodeReadDataTypeMultiCommand> logger)
    {
        this.clientFactory = clientFactory ?? throw new ArgumentNullException(nameof(clientFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override Task<int> ExecuteAsync(
        CommandContext context,
        ReadDataTypesCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ReadDataTypesCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        var nodeIds = settings.NodeIds;

        return await OpcUaCommandRunner.RunAsync(
            clientFactory,
            settings,
            logger,
            async (client, ct) =>
            {
                var enumDataTypes = await client.ReadEnumDataTypesAsync(nodeIds, ct);
                foreach (var enumDataType in enumDataTypes)
                {
                    RenderEnumDataType(enumDataType);
                    AnsiConsole.WriteLine();
                }
            },
            cancellationToken);
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