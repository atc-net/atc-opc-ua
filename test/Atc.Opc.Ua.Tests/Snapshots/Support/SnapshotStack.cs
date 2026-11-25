namespace Atc.Opc.Ua.Tests.Snapshots.Support;

/// <summary>
/// Every OPC UA stack call the snapshot tests make, in one place: when the stack's API changes, only this file changes,
/// and the snapshots show whether the library's output did.
/// </summary>
internal static class SnapshotStack
{
    /// <summary>
    /// The namespace table used to resolve <c>nsu=</c> ids, as a server could send it.
    /// </summary>
    public static readonly string[] NamespaceUris =
    [
        "http://opcfoundation.org/UA/",
        "urn:localhost:OPCFoundation:CoreSampleServer",
        "http://opcfoundation.org/UA/Diagnostics",
        "http://microsoft.com/Opc/OpcPlc/",
        "http://www.siemens.com/simatic-s7-opcua",
    ];

    public static NodeId ParseAsTheLibraryDoes(string text)
        => NodeIdParser.Parse(text, nameof(text));

    public static NodeId Parse(string text)
        => NodeId.Parse(text);

    public static string ParseExpandedAndResolve(string text)
    {
        var expandedNodeId = ExpandedNodeId.Parse(text);
        var namespaceTable = new NamespaceTable(NamespaceUris);
        var nodeId = ExpandedNodeId.ToNodeId(expandedNodeId, namespaceTable);
        return $"{expandedNodeId} -> {(nodeId.IsNull ? "null" : Describe(nodeId))}";
    }

    /// <summary>
    /// What the library takes from a NodeId: its text, the identifier as <c>NodeIdentifier</c> gets it, and the
    /// identifier as the type descriptors get it.
    /// </summary>
    /// <param name="nodeId">The node id.</param>
    /// <returns>One line.</returns>
    public static string Describe(NodeId nodeId)
        => $"{nodeId} | ns {nodeId.NamespaceIndex} | {nodeId.IdType.GetIdentifierTypeName()} | identifier '{nodeId.GetIdentifierText()}' | as string '{nodeId.GetIdentifierAsString()}'";

    public static VariableNode CreateVariableNode(
        NodeId dataType,
        bool isArray)
    {
        var variableNode = new VariableNode
        {
            NodeId = new NodeId("Snapshot", 2),
            DataType = dataType,
            ValueRank = isArray ? ValueRanks.OneDimension : ValueRanks.Scalar,
        };

        if (isArray)
        {
            variableNode.ArrayDimensions = [0];
        }

        return variableNode;
    }

    public static ISession CreateSessionWithoutServer()
        => Substitute.For<ISession>();

    /// <summary>
    /// A fixed value for a writable static node of each type in the simulator's reference test set, scalar and array.
    /// The simulator fills those nodes with random values when it starts, so the live snapshot writes these first.
    /// </summary>
    /// <returns>The node ids and the values to write.</returns>
    public static IReadOnlyList<(string NodeId, object Value)> CreateValuesToWrite()
    {
        var dateTime = new DateTime(2026, 11, 20, 12, 34, 56, 789, DateTimeKind.Utc);
        var guid = new Guid("51b74e55-f2e3-4a4d-b79c-bf57c76ea67c");
        var xml = new System.Xml.XmlDocument();
        xml.LoadXml("<Motor Speed=\"1500\">Running</Motor>");

        return
        [
            ("ns=6;s=Scalar_Static_Boolean", true),
            ("ns=6;s=Scalar_Static_SByte", (sbyte)-5),
            ("ns=6;s=Scalar_Static_Byte", (byte)200),
            ("ns=6;s=Scalar_Static_Int16", (short)-300),
            ("ns=6;s=Scalar_Static_UInt16", (ushort)60000),
            ("ns=6;s=Scalar_Static_Int32", -70000),
            ("ns=6;s=Scalar_Static_UInt32", 4000000000u),
            ("ns=6;s=Scalar_Static_Int64", -5000000000L),
            ("ns=6;s=Scalar_Static_UInt64", 10000000000000000000UL),
            ("ns=6;s=Scalar_Static_Float", 0.1f),
            ("ns=6;s=Scalar_Static_Double", 0.1d),
            ("ns=6;s=Scalar_Static_String", "Tank æøå; \"quoted\""),
            ("ns=6;s=Scalar_Static_DateTime", dateTime),
            ("ns=6;s=Scalar_Static_Guid", guid),
            ("ns=6;s=Scalar_Static_ByteString", new byte[] { 0x01, 0xAB, 0xFF }),
            ("ns=6;s=Scalar_Static_XmlElement", xml.DocumentElement),
            ("ns=6;s=Scalar_Static_NodeId", new NodeId("Motor 1", 2)),
            ("ns=6;s=Scalar_Static_QualifiedName", new QualifiedName("Speed", 2)),
            ("ns=6;s=Scalar_Static_LocalizedText", new LocalizedText("en-US", "Tank level")),
            ("ns=6;s=Scalar_Static_Variant", 42),
            ("ns=6;s=Scalar_Static_Arrays_Boolean", new[] { true, false }),
            ("ns=6;s=Scalar_Static_Arrays_SByte", new sbyte[] { -5, 7 }),
            ("ns=6;s=Scalar_Static_Arrays_Byte", new byte[] { 1, 255 }),
            ("ns=6;s=Scalar_Static_Arrays_Int16", new short[] { -300, 300 }),
            ("ns=6;s=Scalar_Static_Arrays_UInt16", new ushort[] { 0, 60000 }),
            ("ns=6;s=Scalar_Static_Arrays_Int32", new[] { -70000, 70000 }),
            ("ns=6;s=Scalar_Static_Arrays_UInt32", new[] { 0u, 4000000000u }),
            ("ns=6;s=Scalar_Static_Arrays_Int64", new[] { -5000000000L, 5000000000L }),
            ("ns=6;s=Scalar_Static_Arrays_UInt64", new[] { 0UL, 10000000000000000000UL }),
            ("ns=6;s=Scalar_Static_Arrays_Float", new[] { 0.1f, -1.5f }),
            ("ns=6;s=Scalar_Static_Arrays_Double", new[] { 0.1d, 1e-20d }),
            ("ns=6;s=Scalar_Static_Arrays_String", new[] { "a", "b;c", string.Empty }),
            ("ns=6;s=Scalar_Static_Arrays_DateTime", new[] { dateTime, dateTime.AddDays(1) }),
            ("ns=6;s=Scalar_Static_Arrays_Guid", new[] { guid, Guid.Empty }),
            ("ns=6;s=Scalar_Static_Arrays_ByteString", new[] { new byte[] { 0x01, 0x02 }, Array.Empty<byte>() }),
            ("ns=6;s=Scalar_Static_Arrays_NodeId", new[] { new NodeId(85u), new NodeId("Motor 1", 2) }),
            ("ns=6;s=Scalar_Static_Arrays_QualifiedName", new[] { new QualifiedName("Speed", 2), new QualifiedName("Level", 0) }),
            ("ns=6;s=Scalar_Static_Arrays_LocalizedText", new[] { new LocalizedText("en-US", "Tank"), new LocalizedText("da-DK", "Tank æøå") }),
        ];
    }
}