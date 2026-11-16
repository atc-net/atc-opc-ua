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
        ReadDataTypeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ExecuteInternalAsync(settings, cancellationToken);
    }

    private async Task<int> ExecuteInternalAsync(
        ReadDataTypeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        await ConsoleHelper.WriteHeaderAsync(cancellationToken);

        var nodeId = settings.NodeId;

        return await OpcUaCommandRunner.RunAsync(
            opcUaClient,
            settings,
            logger,
            async ct =>
            {
                var enumDataType = await opcUaClient.ReadEnumDataTypeAsync(nodeId, ct)
                                   ?? throw OpcUaCommandRunner.NodeNotFound(nodeId);

                RenderEnumDataType(enumDataType);
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