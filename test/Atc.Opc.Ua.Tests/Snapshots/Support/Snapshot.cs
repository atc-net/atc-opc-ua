namespace Atc.Opc.Ua.Tests.Snapshots.Support;

/// <summary>
/// Compares a test's output with its snapshot, <c>Verified/{TestClass}.{TestMethod}.verified.txt</c> next to the
/// test's source file.
/// </summary>
/// <remarks>
/// On a mismatch, or when there's no snapshot yet, the output is written to <c>.received.txt</c> next to it (git
/// ignores those) and the test fails. Compare the two files; when the new output is right, replace the
/// <c>.verified.txt</c> file with the <c>.received.txt</c> file.
/// </remarks>
internal static class Snapshot
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static void Match(
        string actual,
        [CallerFilePath] string testFilePath = "",
        [CallerMemberName] string testName = "")
    {
        ArgumentNullException.ThrowIfNull(actual);

        var testDirectory = Path.GetDirectoryName(testFilePath) ?? string.Empty;

        if (!Directory.Exists(testDirectory))
        {
            Assert.Fail($"The test's source folder '{testDirectory}' doesn't exist here. The snapshots are found through the source path the compiler records, so don't map source paths (ContinuousIntegrationBuild, PathMap) for test projects.");
        }

        var directory = Directory.CreateDirectory(Path.Combine(testDirectory, "Verified")).FullName;
        var name = $"{Path.GetFileNameWithoutExtension(testFilePath)}.{testName}";
        var verifiedPath = Path.Combine(directory, $"{name}.verified.txt");
        var receivedPath = Path.Combine(directory, $"{name}.received.txt");
        var output = Normalize(actual);

        if (!File.Exists(verifiedPath))
        {
            File.WriteAllText(receivedPath, output, Utf8WithoutBom);
            Assert.Fail($"No snapshot yet. Check '{receivedPath}'. {DescribeHowToAccept(receivedPath, verifiedPath)}");
        }

        var expected = Normalize(File.ReadAllText(verifiedPath, Utf8WithoutBom));

        if (string.Equals(expected, output, StringComparison.Ordinal))
        {
            File.Delete(receivedPath);
            return;
        }

        File.WriteAllText(receivedPath, output, Utf8WithoutBom);
        Assert.Fail($"The output differs from the snapshot at {DescribeFirstDifference(expected, output)}. Compare '{verifiedPath}' with '{receivedPath}'. {DescribeHowToAccept(receivedPath, verifiedPath)}");
    }

    private static string Normalize(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string DescribeHowToAccept(
        string receivedPath,
        string verifiedPath)
        => $"If the new output is right: Move-Item -Force '{receivedPath}' '{verifiedPath}'";

    private static string DescribeFirstDifference(
        string expected,
        string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');

        for (var i = 0; i < System.Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            var expectedLine = i < expectedLines.Length ? expectedLines[i] : "(none)";
            var actualLine = i < actualLines.Length ? actualLines[i] : "(none)";

            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                return $"line {i + 1}: expected '{expectedLine}', got '{actualLine}'";
            }
        }

        return "the end of the file";
    }
}