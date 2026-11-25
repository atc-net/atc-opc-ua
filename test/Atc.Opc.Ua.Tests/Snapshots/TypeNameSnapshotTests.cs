namespace Atc.Opc.Ua.Tests.Snapshots;

/// <summary>
/// Consumers map a variable's type names, e.g. <c>ClrTypeName</c>, to their own types, so the names the library gives
/// each DataType must not change unnoticed.
/// </summary>
/// <remarks>
/// Covers every namespace-0 DataType id in <c>XUnitTestData/namespace_zero_data_type_ids.txt</c> and a few ids
/// outside namespace 0, scalar and array, through each code path that names a type. The resolver's server lookup for
/// types that aren't built in is covered by <see cref="LiveSnapshotTests"/>; here it gets no answer and falls back to
/// the type's node id.
/// </remarks>
public sealed class TypeNameSnapshotTests
{
    private static readonly string[] IdsOutsideNamespaceZero =
    [
        "ns=2;i=3001",
        "ns=4;s=\"UDT_Motor\"",
        "ns=3;g=51b74e55-f2e3-4a4d-b79c-bf57c76ea67c",
    ];

    [Fact]
    public async Task TypeNames_EveryDataTypeAndCodePath_ShouldMatchTheSnapshot()
    {
        // Arrange
        var resolver = new DataTypeInfoResolver();
        using var session = SnapshotStack.CreateSessionWithoutServer();
        var inputs = SnapshotFormat.ReadNamespaceZeroDataTypeIds()
            .Concat(IdsOutsideNamespaceZero.Select(id => (Id: id, Name: "(outside namespace 0)")));

        var text = new StringBuilder();

        // Act
        foreach (var (id, name) in inputs)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"{id} {name}");

            foreach (var isArray in new[] { false, true })
            {
                var dataType = SnapshotStack.Parse(id);
                var variableNode = SnapshotStack.CreateVariableNode(dataType, isArray);
                var shape = isArray ? "array " : "scalar";
                var mapper = SnapshotFormat.Try(() => OpcUaToDotNetDataTypeMapper.GetSystemTypeAsString(dataType, variableNode.ArrayDimensions.ToArray() ?? []));
                var basic = SnapshotFormat.Try(() => $"{SnapshotFormat.Describe(variableNode.CreateBasicOpcUaDataType())} / {SnapshotFormat.Describe(variableNode.CreateBasicDotNetTypeDescriptor())}");
                var resolved = await SnapshotFormat.TryAsync(async () =>
                {
                    var info = await resolver.ResolveAsync(variableNode, session, TestContext.Current.CancellationToken);
                    return $"{SnapshotFormat.Describe(info.OpcUaDataType)} / {SnapshotFormat.Describe(info.DotNetType)}";
                });

                text.AppendLine(CultureInfo.InvariantCulture, $"  {shape} mapper:   {mapper}");
                text.AppendLine(CultureInfo.InvariantCulture, $"  {shape} basic:    {basic}");
                text.AppendLine(CultureInfo.InvariantCulture, $"  {shape} resolver: {resolved}");
            }
        }

        // Assert
        Snapshot.Match(text.ToString());
    }
}