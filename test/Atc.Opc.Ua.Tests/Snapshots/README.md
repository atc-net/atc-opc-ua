# Snapshot tests

These tests record what the library outputs today, so a change to that output can't go unnoticed: node id text, type names, scan trees, values and enum DataTypes. Consumers store and compare that output, so a silent change would break them.

## How a snapshot test works

Every test does three things: it runs the library, turns the output into lines of text, and compares the text with a recorded file. `ScanFileSnapshotTests` is the shortest:

```csharp
[Fact]
public void ScanWrittenBy320_ReadByThisVersion_ShouldMatchTheSnapshot()
{
    // Act: run the library
    var scan = SnapshotFormat.ReadScanWrittenBy320();

    // Assert: turn its output into lines of text, and compare them with the recorded file
    var lines = SnapshotFormat.DescribeTree(scan.Result);
    lines.Insert(0, $"{scan.EndpointUrl} from {scan.StartingNodeId}");
    Snapshot.Match(string.Join('\n', lines) + "\n");
}
```

`Snapshot.Match` compares the text with `Verified/<TestClass>.<TestMethod>.verified.txt`, here `Verified/ScanFileSnapshotTests.ScanWrittenBy320_ReadByThisVersion_ShouldMatchTheSnapshot.verified.txt`. One line per node:

```text
i=85 | parent i=84 | Object | 'Objects' | identifier '85'
ns=3;b=YWJj | parent ns=3;s=Special | Variable | 'Opaque_abc' | identifier 'System.Byte[]' | opcua UInt32 … | dotnet Primitive UInt32 clr 'uint'
```

The same text passes. Different text fails the test, and the difference is something a consumer would see.

## Running them

```powershell
dotnet test --project test/Atc.Opc.Ua.Tests
```

| Test class | Needs |
|---|---|
| `NodeIdTextSnapshotTests` | Nothing |
| `TypeNameSnapshotTests` | Nothing |
| `ScanFileSnapshotTests` | Nothing |
| `LiveSnapshotTests` | Docker |

`LiveSnapshotTests` reads the [OPC PLC simulator](https://github.com/Azure-Samples/iot-edge-opc-plc). `OpcPlcSimulator` starts it in Docker before the first live test and removes it after the last one, with the image and arguments the snapshots were recorded with:

- The first run pulls the image, which takes a while; after that, the simulator starts in a few seconds.
- Port 50000 must be free, because the simulator advertises `opc.tcp://localhost:50000` as its endpoint.
- Without Docker running, the live tests are skipped.

The live tests have the `category=integration` trait. CI leaves them out, and so can you:

```powershell
dotnet test --project test/Atc.Opc.Ua.Tests --filter-query "/[category!=integration]"
```

## When a snapshot doesn't match

The test fails with the first line that differs, and writes its output next to the recorded file as `.received.txt` (git ignores those):

```text
The output differs from the snapshot at line 12: expected '…', got '…'.
Compare '…/Verified/X.verified.txt' with '…/Verified/X.received.txt'.
If the new output is right: Move-Item -Force '…/Verified/X.received.txt' '…/Verified/X.verified.txt'
```

Compare the two files with a diff tool, then:

- **The change is a mistake:** fix the code, and run the test again. When it passes, the `.received.txt` file is deleted.
- **The change is intended:** run the `Move-Item` command from the message, so the new output becomes the recorded one. Commit the `.verified.txt` change together with the code change, so the review shows both.

## Adding a snapshot

1. Write a test that turns the output into text, using `SnapshotFormat` for the descriptions, and ends with `Snapshot.Match(text)`. Put any OPC UA stack call in `SnapshotStack`.
2. Run it. It fails with "No snapshot yet" and writes the `.received.txt` file.
3. Check that file, run the `Move-Item` command from the message, and commit the `.verified.txt` file with the test.

## Files

| File | What it is |
|---|---|
| The four `*SnapshotTests.cs` | The tests. |
| `Verified/*.verified.txt` | The recorded output, one file per test. |
| `Support/Snapshot.cs` | Compares the text with the recorded file. |
| `Support/SnapshotFormat.cs` | Turns the library's output into lines of text, and reads the inputs. |
| `Support/SnapshotStack.cs` | Every OPC UA stack call the tests make. When the stack's API changes, only this file changes, and the snapshots show whether the library's output did. |
| `Support/OpcPlcSimulator.cs` | Starts the simulator in Docker for the live tests. |
| `../XUnitTestData/namespace_zero_data_type_ids.txt` | The input for the type-name snapshots: the namespace-0 DataType ids the OPC UA stack listed when they were recorded. Kept as a file, so the snapshots cover the same ids whatever a later stack lists. |
| `../XUnitTestData/scan_written_by_3.2.0.json` | A scan of the simulator written by the Atc.Opc.Ua 3.2.0 package, as a consumer stores it: the `OpcUaScanResult` shape, `NodeJsonConverter`, indented, nulls left out. Started at `i=85`, object depth 10, variable depth 2, with sample values, leaving out the objects and variables listed in `LiveSnapshotTests` to keep the file small. `ScanFileSnapshotTests` reads it; `LiveSnapshotTests` checks that a scan today finds the same nodes. |

The live value test writes fixed values to the simulator's static nodes before reading them, because the simulator fills those with random values when it starts.