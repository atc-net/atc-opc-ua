namespace Atc.Opc.Ua.Tests.Snapshots;

/// <summary>
/// Node ids are kept as text by consumers, e.g. to recognise a node between two scans, so the text the library reads
/// and writes must not change unnoticed.
/// </summary>
public sealed class NodeIdTextSnapshotTests
{
    private static readonly string[] Inputs =
    [
        "i=85",
        "ns=0;i=85",
        "ns=2;i=6095",
        "ns=4;i=4294967295",
        "i=0",

        // String
        "ns=3;s=StepUp",
        "s=WithoutNamespace",
        "ns=4;s=\"Root.Objects.IoT data\"",
        "ns=4;s=\"Root.Objects.IoT data.instanceConverted\"",
        "ns=4;s=\"DB_Data\".\"Motor 1\".\"Speed\"",
        "ns=3;s=a;b",
        "ns=3;s=a=b",
        "ns=3;s=ns=2;i=5",
        "ns=3;s=Tank æøå ÆØÅ",
        "ns=6;s=Locales_QN日本の",
        "ns=3;s= leading and trailing ",
        "ns=3;s=",

        // Guid
        "ns=3;g=51b74e55-f2e3-4a4d-b79c-bf57c76ea67c",
        "ns=3;g=51B74E55-F2E3-4A4D-B79C-BF57C76EA67C",
        "g=00000000-0000-0000-0000-000000009204",

        // Opaque
        "ns=3;b=YWJj",
        "ns=6;b=CQIABQ==",

        // With a namespace URI
        "nsu=http://microsoft.com/Opc/OpcPlc/;s=StepUp",
        "nsu=http://www.siemens.com/simatic-s7-opcua;s=\"DB_Data\".\"Speed\"",
        "nsu=urn:unknown;i=1",

        // Invalid or unusual
        " i=85",
        "i=85 ",
        "ns=2;i=abc",
        "ns=x;i=1",
        "ns=2;x=1",
        "ns=3;g=not-a-guid",
        "ns=3;b=not base64!",
        "foo",
        string.Empty,
    ];

    [Fact]
    public void NodeIdText_EveryParsePath_ShouldMatchTheSnapshot()
    {
        // Arrange
        var text = new StringBuilder();

        // Act
        foreach (var input in Inputs)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"input {Quote(input)}");
            text.AppendLine(CultureInfo.InvariantCulture, $"  library:    {SnapshotFormat.Try(() => SnapshotStack.Describe(SnapshotStack.ParseAsTheLibraryDoes(input)))}");
            text.AppendLine(CultureInfo.InvariantCulture, $"  Parse:      {SnapshotFormat.Try(() => SnapshotStack.Describe(SnapshotStack.Parse(input)))}");
            text.AppendLine(CultureInfo.InvariantCulture, $"  Expanded:   {SnapshotFormat.Try(() => SnapshotStack.ParseExpandedAndResolve(input))}");
        }

        // Assert
        Snapshot.Match(text.ToString());
    }

    private static string Quote(string text)
        => $"\"{text.Replace("\t", "\\t", StringComparison.Ordinal)}\"";
}