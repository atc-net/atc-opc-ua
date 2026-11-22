namespace Atc.Opc.Ua.Tests.Snapshots.Support;

/// <summary>
/// Writes the library's output as snapshot text, and reads the snapshot inputs.
/// </summary>
internal static class SnapshotFormat
{
    public static string Describe(OpUaDataType? type)
        => type is null
            ? "opcua (none)"
            : $"opcua {type.Name} '{type.DisplayName}' {type.Kind} {type.IdentifierType}:{type.Identifier} array {type.IsArray}";

    public static string Describe(DotNetTypeDescriptor? type)
    {
        if (type is null)
        {
            return "dotnet (none)";
        }

        var text = $"dotnet {type.Kind} {type.Name} clr '{type.ClrTypeName}'";

        if (type.ArrayElementType is not null)
        {
            text += $" of ({Describe(type.ArrayElementType)})";
        }

        if (type.EnumMembers is { Count: > 0 } members)
        {
            text += " members " + string.Join(", ", members.Select(m => $"{m.Value}={m.Name}"));
        }

        return text;
    }

    public static string Describe(
        NodeBase node,
        string? parentText = null)
    {
        var text = $"{node.NodeId} | parent {parentText ?? node.ParentNodeId} | {node.NodeClass} | '{node.DisplayName}' | identifier '{node.NodeIdentifier}'";
        return node is NodeVariable variable
            ? $"{text} | {Describe(variable.DataTypeOpcUa)} | {Describe(variable.DataTypeDotnet)}"
            : text;
    }

    /// <summary>
    /// Describes every node of a scan tree, one line each, sorted, without values.
    /// </summary>
    /// <param name="root">The tree's root.</param>
    /// <param name="nodeWithUnstableParent">
    /// A node the server lists under several parents in an order that can change: its parent is left out.
    /// </param>
    /// <returns>The lines.</returns>
    public static List<string> DescribeTree(
        NodeBase root,
        string? nodeWithUnstableParent = null)
    {
        var lines = new List<string>();
        var nodes = new Stack<NodeBase>();
        nodes.Push(root);

        while (nodes.Count > 0)
        {
            var node = nodes.Pop();
            lines.Add(string.Equals(node.NodeId, nodeWithUnstableParent, StringComparison.Ordinal)
                ? Describe(node, "(the first one reached)")
                : Describe(node));

            if (node is NodeObject nodeObject)
            {
                foreach (var child in nodeObject.NodeObjects)
                {
                    nodes.Push(child);
                }
            }

            foreach (var child in node.NodeVariables)
            {
                nodes.Push(child);
            }
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    /// <summary>
    /// Reads <c>XUnitTestData/scan_written_by_3.2.0.json</c>: a scan of the OPC PLC simulator, written by the 3.2.0
    /// package as a consumer stores it; see <c>README.md</c> in the Snapshots folder.
    /// </summary>
    /// <returns>The scan.</returns>
    public static Differs.OpcUaScanResult ReadScanWrittenBy320()
        => JsonSerializer.Deserialize<Differs.OpcUaScanResult>(TestResourceProvider.GetTestData("scan_written_by_3.2.0"), CreateScanFileOptions())
           ?? throw new InvalidOperationException("'scan_written_by_3.2.0.json' is empty.");

    /// <summary>
    /// Writes a scan tree as JSON and reads it back, as a consumer that stores its scans does.
    /// </summary>
    /// <param name="root">The tree's root.</param>
    /// <returns>The tree as it reads back.</returns>
    public static NodeBase RoundTrip(NodeBase root)
    {
        var options = CreateScanFileOptions();
        return JsonSerializer.Deserialize<NodeBase>(JsonSerializer.Serialize(root, options), options)
               ?? throw new InvalidOperationException("The scan didn't read back.");
    }

    private static JsonSerializerOptions CreateScanFileOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        options.Converters.Add(new NodeJsonConverter());
        return options;
    }

    /// <summary>
    /// Reads the namespace-0 DataType ids, kept in <c>XUnitTestData/namespace_zero_data_type_ids.txt</c>, so the
    /// snapshots always cover the same ids.
    /// </summary>
    /// <returns>The ids with their names.</returns>
    public static IReadOnlyList<(string Id, string Name)> ReadNamespaceZeroDataTypeIds()
        => TestResourceProvider.GetTestData("namespace_zero_data_type_ids", "txt")
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split('\t'))
            .Select(parts => (parts[0], parts[1]))
            .ToList();

    public static string Try(Func<string> describe)
    {
        try
        {
            return describe();
        }
        catch (Exception ex)
        {
            return $"throws {ex.GetType().Name}";
        }
    }

    public static async Task<string> TryAsync(Func<Task<string>> describe)
    {
        try
        {
            return await describe();
        }
        catch (Exception ex)
        {
            return $"throws {ex.GetType().Name}";
        }
    }
}