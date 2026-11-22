namespace Atc.Opc.Ua.Tests.Snapshots;

/// <summary>
/// Consumers store scans as JSON and compare the next scan with the stored one, so a scan written by an earlier
/// package must still be read the same way.
/// </summary>
public sealed class ScanFileSnapshotTests
{
    [Fact]
    public void ScanWrittenBy320_ReadByThisVersion_ShouldMatchTheSnapshot()
    {
        // Act
        var scan = SnapshotFormat.ReadScanWrittenBy320();

        // Assert
        var lines = SnapshotFormat.DescribeTree(scan.Result);
        lines.Insert(0, $"{scan.EndpointUrl} from {scan.StartingNodeId}");
        Snapshot.Match(string.Join('\n', lines) + "\n");
    }
}