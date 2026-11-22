namespace Atc.Opc.Ua.Tests.Snapshots;

/// <summary>
/// Snapshots of what the library reads from the OPC PLC simulator: the scan tree, values of every type, enum DataTypes,
/// and the type names the server gives. <see cref="OpcPlcSimulator"/> starts the simulator in Docker; see
/// <c>README.md</c> in this folder.
/// </summary>
[Trait("category", "integration")]
public sealed class LiveSnapshotTests(OpcPlcSimulator simulator) : IClassFixture<OpcPlcSimulator>
{
    /// <summary>
    /// The simulator's Server object: its session diagnostics have node ids that differ for every session.
    /// </summary>
    private const string ServerObjectNodeId = "i=2253";

    /// <summary>
    /// 'Boiler #2' is under both Boilers and DeviceSet. The scan keeps it once, under the parent it reaches first, and
    /// the simulator's browse order between the two changes when it restarts.
    /// </summary>
    private const string NodeWithTwoParents = "ns=4;i=5017";

    /// <summary>
    /// The objects left out of <c>XUnitTestData/scan_written_by_3.2.0.json</c> to keep it small; see <c>README.md</c>
    /// in this folder.
    /// </summary>
    private static readonly string[] ObjectsLeftOutOfTheScanWrittenBy320 =
    [
        "i=2253",
        "i=23470",
        "i=31915",
        "ns=2;i=5001",
        "ns=2;i=6078",
        "ns=2;i=6094",
        "ns=6;s=AccessRights",
        "ns=6;s=Attributes",
        "ns=6;s=DataAccess",
        "ns=6;s=Methods",
        "ns=6;s=MyCompany",
        "ns=6;s=References",
        "ns=6;s=Scalar",
        "ns=6;s=Views",
    ];

    /// <summary>
    /// The variables left out of <c>XUnitTestData/scan_written_by_3.2.0.json</c>: test strings of 10 to 200 kB.
    /// </summary>
    private static readonly string[] VariablesLeftOutOfTheScanWrittenBy320 =
    [
        "ns=3;s=LongString10kB",
        "ns=3;s=LongString50kB",
        "ns=3;s=LongString100kB",
        "ns=3;s=LongString200kB",
    ];

    /// <summary>
    /// Variables the value snapshot reads without writing them first: 'DeviceHealth' of 'Boiler #2', an enum that
    /// stays 0, and a boiler's 'BoilerStatus', a structure. The structure's temperatures change while the simulator
    /// runs, but its value is read as empty text, so the snapshot stays the same.
    /// </summary>
    private static readonly string[] VariablesReadOnly =
    [
        "ns=4;i=6198",
        "ns=4;i=15013",
    ];

    [Fact]
    public async Task Scan_WholeServer_ShouldMatchTheSnapshot()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await simulator.ConnectAsync(cancellationToken);
        var scanner = new OpcUaScanner(NullLogger<OpcUaScanner>.Instance);
        var options = new OpcUaScannerOptions
        {
            ObjectDepth = 10,
            VariableDepth = 2,
            IncludeSampleValues = true,
        };

        options.ExcludeObjectNodeIds.Add(ServerObjectNodeId);

        // Act
        var result = await scanner.ScanAsync(client, options, cancellationToken);
        await client.DisconnectAsync(cancellationToken);

        // Assert: the tree without values, which change while the simulator runs
        var lines = SnapshotFormat.DescribeTree(result.Root, NodeWithTwoParents);
        lines.AddRange(result.SkippedNodeIds.Order(StringComparer.Ordinal).Select(id => $"skipped {id}"));
        Snapshot.Match(string.Join('\n', lines) + "\n");
    }

    [Fact]
    public async Task Scan_SameOptionsAsTheScanWrittenBy320_ShouldFindTheSameNodes()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var previous = SnapshotFormat.ReadScanWrittenBy320();
        using var client = await simulator.ConnectAsync(cancellationToken);
        var scanner = new OpcUaScanner(NullLogger<OpcUaScanner>.Instance);
        var options = new OpcUaScannerOptions
        {
            StartingNodeId = previous.StartingNodeId,
            ObjectDepth = 10,
            VariableDepth = 2,
            IncludeSampleValues = true,
        };

        foreach (var nodeId in ObjectsLeftOutOfTheScanWrittenBy320)
        {
            options.ExcludeObjectNodeIds.Add(nodeId);
        }

        foreach (var nodeId in VariablesLeftOutOfTheScanWrittenBy320)
        {
            options.ExcludeVariableNodeIds.Add(nodeId);
        }

        // Act
        var result = await scanner.ScanAsync(client, options, cancellationToken);
        await client.DisconnectAsync(cancellationToken);
        var comparison = NodeTreeDiffer.Diff(result.Root, previous.Result);

        // Assert: a consumer comparing its stored scan with a new one, stored the same way, sees no change
        comparison.AddedNodes.Select(node => node.NodeId).Should().BeEmpty();
        comparison.DeletedNodes.Select(node => node.NodeId).Should().BeEmpty();
        SnapshotFormat.DescribeTree(SnapshotFormat.RoundTrip(result.Root)).Should().Equal(SnapshotFormat.DescribeTree(previous.Result));
    }

    [Fact]
    public async Task Values_WrittenThenReadBack_ShouldMatchTheSnapshot()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await simulator.ConnectAsync(cancellationToken);
        var text = new StringBuilder();

        // Act
        foreach (var (nodeId, value) in SnapshotStack.CreateValuesToWrite())
        {
            var written = await SnapshotFormat.TryAsync(async () =>
            {
                await client.WriteNodeAsync(nodeId, value, cancellationToken);
                return "written";
            });

            var read = await ReadAsync(nodeId);
            text.AppendLine(CultureInfo.InvariantCulture, $"{nodeId} {written}: {read}");
        }

        foreach (var nodeId in VariablesReadOnly)
        {
            var read = await ReadAsync(nodeId);
            text.AppendLine(CultureInfo.InvariantCulture, $"{nodeId} read only: {read}");
        }

        await client.DisconnectAsync(cancellationToken);

        // Assert
        Snapshot.Match(text.ToString());

        Task<string> ReadAsync(string nodeId)
            => SnapshotFormat.TryAsync(async () =>
            {
                var variable = await client.ReadNodeVariableAsync(nodeId, includeSampleValue: true, cancellationToken: cancellationToken);
                return variable is null
                    ? "not found"
                    : $"value '{variable.SampleValue}' | {SnapshotFormat.Describe(variable.DataTypeOpcUa)} | {SnapshotFormat.Describe(variable.DataTypeDotnet)}";
            });
    }

    [Fact]
    public async Task EnumDataTypes_ReadFromTheServer_ShouldMatchTheSnapshot()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await simulator.ConnectAsync(cancellationToken);
        string[] dataTypeIds = ["i=852", "i=11939", "i=12077", "i=29", "i=1", "i=99999"];
        var text = new StringBuilder();

        // Act
        foreach (var dataTypeId in dataTypeIds)
        {
            var read = await SnapshotFormat.TryAsync(async () =>
            {
                var enumDataType = await client.ReadEnumDataTypeAsync(dataTypeId, cancellationToken);
                return enumDataType is null
                    ? "null"
                    : $"{enumDataType.NodeId} {enumDataType.Name} '{enumDataType.DisplayName}' enum values {enumDataType.HasEnumValues} '{enumDataType.Description}' members "
                      + string.Join(", ", enumDataType.Members.Select(m => $"{m.Value}={m.Name} '{m.DisplayName}' '{m.Description}'"));
            });

            text.AppendLine(CultureInfo.InvariantCulture, $"{dataTypeId}: {read}");
        }

        await client.DisconnectAsync(cancellationToken);

        // Assert
        Snapshot.Match(text.ToString());
    }

    [Fact]
    public async Task TypeNames_ResolvedByTheServer_ShouldMatchTheSnapshot()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        using var client = await simulator.ConnectAsync(cancellationToken);
        var resolver = new DataTypeInfoResolver();
        var text = new StringBuilder();

        // Act
        foreach (var (id, name) in SnapshotFormat.ReadNamespaceZeroDataTypeIds())
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"{id} {name}");

            foreach (var isArray in new[] { false, true })
            {
                var variableNode = SnapshotStack.CreateVariableNode(SnapshotStack.Parse(id), isArray);
                var resolved = await SnapshotFormat.TryAsync(async () =>
                {
                    var info = await resolver.ResolveAsync(variableNode, client.Session, cancellationToken);
                    return $"{SnapshotFormat.Describe(info.OpcUaDataType)} / {SnapshotFormat.Describe(info.DotNetType)}";
                });

                text.AppendLine(CultureInfo.InvariantCulture, $"  {(isArray ? "array " : "scalar")} {resolved}");
            }
        }

        await client.DisconnectAsync(cancellationToken);

        // Assert
        Snapshot.Match(text.ToString());
    }
}