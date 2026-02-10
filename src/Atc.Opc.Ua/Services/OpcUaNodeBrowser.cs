// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides lazy, single-level address space browsing and attribute reading
/// for OPC UA nodes.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "OPC UA communication can fail with various exception types.")]
public partial class OpcUaNodeBrowser : IOpcUaNodeBrowser
{
    private readonly ILogger<OpcUaNodeBrowser> logger;

    public OpcUaNodeBrowser(ILogger<OpcUaNodeBrowser> logger)
        => this.logger = logger;

    /// <inheritdoc/>
    public async Task<(bool Succeeded, IList<NodeBrowseResult>? Children, string? ErrorMessage)> BrowseChildrenAsync(
        IOpcUaClient client,
        string parentNodeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (string.IsNullOrWhiteSpace(parentNodeId))
        {
            return (false, null, "Missing parentNodeId.");
        }

        if (!client.IsConnected() || client.Session is null)
        {
            return (false, null, "Client is not connected.");
        }

        try
        {
            parentNodeId = parentNodeId.Trim();
            LogBrowseChildren(parentNodeId);

            var browser = BrowserFactory.GetForwardBrowser(client.Session);
            var references = await browser.BrowseAsync(new NodeId(parentNodeId), cancellationToken);

            var results = new List<NodeBrowseResult>(references.Count);

            foreach (var reference in references)
            {
                var nodeClass = MapNodeClass(reference.NodeClass);
                var nodeIdStr = reference.NodeId.ToString();

                var browseResult = new NodeBrowseResult
                {
                    NodeId = nodeIdStr,
                    DisplayName = reference.DisplayName?.Text ?? string.Empty,
                    BrowseName = reference.BrowseName?.Name ?? string.Empty,
                    NodeClass = nodeClass,
                    HasChildren = nodeClass is NodeClassType.Object or NodeClassType.Variable,
                };

                if (nodeClass == NodeClassType.Variable)
                {
                    browseResult.DataTypeName = await ResolveDataTypeNameAsync(
                        client.Session,
                        nodeIdStr,
                        cancellationToken);
                }

                results.Add(browseResult);
            }

            LogBrowseChildrenSucceeded(parentNodeId, results.Count);
            return (true, results, null);
        }
        catch (Exception ex)
        {
            LogBrowseChildrenFailure(parentNodeId, ex.Message);
            return (false, null, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<(bool Succeeded, NodeAttributeSet? Attributes, string? ErrorMessage)> ReadNodeAttributesAsync(
        IOpcUaClient client,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return (false, null, "Missing nodeId.");
        }

        if (!client.IsConnected() || client.Session is null)
        {
            return (false, null, "Client is not connected.");
        }

        try
        {
            nodeId = nodeId.Trim();
            LogReadAttributes(nodeId);

            var node = await client.Session.ReadNodeAsync(nodeId, cancellationToken);
            if (node is null)
            {
                return (false, null, $"Node '{nodeId}' not found.");
            }

            var attributes = new NodeAttributeSet
            {
                NodeId = nodeId,
                DisplayName = node.DisplayName?.Text ?? string.Empty,
                BrowseName = node.BrowseName?.Name ?? string.Empty,
                NodeClass = MapNodeClass(node.NodeClass),
                Description = node.Description?.Text,
            };

            if (node is VariableNode variableNode)
            {
                await PopulateVariableAttributesAsync(
                    client.Session,
                    nodeId,
                    variableNode,
                    attributes,
                    cancellationToken);
            }

            LogReadAttributesSucceeded(nodeId);
            return (true, attributes, null);
        }
        catch (Exception ex)
        {
            LogReadAttributesFailure(nodeId, ex.Message);
            return (false, null, ex.Message);
        }
    }

    private static async Task PopulateVariableAttributesAsync(
        ISession session,
        string nodeId,
        VariableNode variableNode,
        NodeAttributeSet attributes,
        CancellationToken cancellationToken)
    {
        // Read value with timestamps
        var nodesToRead = new ReadValueIdCollection
        {
            new()
            {
                NodeId = new NodeId(nodeId),
                AttributeId = Attributes.Value,
            },
        };

        var readResponse = await session.ReadAsync(
            requestHeader: null,
            0,
            TimestampsToReturn.Both,
            nodesToRead,
            cancellationToken);

        if (readResponse?.Results?.Count > 0)
        {
            var dataValue = readResponse.Results[0];
            attributes.Value = dataValue.WrappedValue.ToString();
            attributes.StatusCode = dataValue.StatusCode.Code;
            attributes.ServerTimestamp = dataValue.ServerTimestamp;
        }

        // Resolve data type name
        var dataTypeNode = variableNode.DataType;
        if (dataTypeNode is not null)
        {
            try
            {
                var dtNode = await session.ReadNodeAsync(dataTypeNode, cancellationToken);
                attributes.DataTypeName = dtNode?.DisplayName?.Text;
            }
            catch
            {
                // Best-effort: DataType name resolution is not critical
            }
        }

        // Access level
        attributes.AccessLevel = variableNode.AccessLevel;
        attributes.UserAccessLevel = variableNode.UserAccessLevel;
        attributes.IsWritable = (variableNode.AccessLevel & AccessLevels.CurrentWrite) != 0;
    }

    private static async Task<string?> ResolveDataTypeNameAsync(
        ISession session,
        string nodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var node = await session.ReadNodeAsync(nodeId, cancellationToken);
            if (node is VariableNode variableNode && variableNode.DataType is not null)
            {
                var dtNode = await session.ReadNodeAsync(variableNode.DataType, cancellationToken);
                return dtNode?.DisplayName?.Text;
            }
        }
        catch
        {
            // Best-effort: data type resolution is not critical for browsing
        }

        return null;
    }

    private static NodeClassType MapNodeClass(NodeClass nodeClass)
        => nodeClass switch
        {
            NodeClass.Object => NodeClassType.Object,
            NodeClass.Variable => NodeClassType.Variable,
            NodeClass.Method => NodeClassType.Method,
            NodeClass.ObjectType => NodeClassType.ObjectType,
            NodeClass.VariableType => NodeClassType.VariableType,
            NodeClass.ReferenceType => NodeClassType.ReferenceType,
            NodeClass.DataType => NodeClassType.DataType,
            NodeClass.View => NodeClassType.View,
            _ => NodeClassType.Unspecified,
        };
}
