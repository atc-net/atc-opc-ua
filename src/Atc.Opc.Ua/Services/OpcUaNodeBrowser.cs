// ReSharper disable InvertIf
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides lazy, single-level address space browsing and attribute reading
/// for OPC UA nodes.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Data type names are best effort.")]
public partial class OpcUaNodeBrowser : IOpcUaNodeBrowser
{
    private const string NotConnectedMessage = "The client isn't connected. Call ConnectAsync first.";

    private readonly ILogger<OpcUaNodeBrowser> logger;

    public OpcUaNodeBrowser(ILogger<OpcUaNodeBrowser> logger)
        => this.logger = logger;

    /// <inheritdoc/>
    public Task<IList<NodeBrowseResult>> BrowseChildrenAsync(
        IOpcUaClient client,
        string parentNodeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentNodeId);

        return InvokeBrowseChildrenAsync(GetSession(client), parentNodeId.Trim(), cancellationToken).NormalizeCancellation(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<NodeAttributeSet?> ReadNodeAttributesAsync(
        IOpcUaClient client,
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        return InvokeReadNodeAttributesAsync(GetSession(client), nodeId.Trim(), cancellationToken).NormalizeCancellation(cancellationToken);
    }

    /// <summary>
    /// The client's session, for the calls that need a connection.
    /// </summary>
    /// <param name="client">The client.</param>
    /// <returns>The connected session.</returns>
    /// <exception cref="InvalidOperationException">The client isn't connected.</exception>
    private static ISession GetSession(IOpcUaClient client)
        => client.IsConnected() && client.Session is { } session
            ? session
            : throw new InvalidOperationException(NotConnectedMessage);

    /// <summary>
    /// Browses one level below the node.
    /// </summary>
    /// <param name="session">The connected session.</param>
    /// <param name="parentNodeId">The node whose children are browsed.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The children.</returns>
    private async Task<IList<NodeBrowseResult>> InvokeBrowseChildrenAsync(
        ISession session,
        string parentNodeId,
        CancellationToken cancellationToken)
    {
        LogBrowseChildren(parentNodeId);

        var browser = BrowserFactory.GetForwardBrowser(session);
        var references = await browser.BrowseAsync(NodeIdParser.Parse(parentNodeId, nameof(parentNodeId)), cancellationToken);

        var results = new List<NodeBrowseResult>(references.Count);

        foreach (var reference in references.ToList())
        {
            var nodeClass = MapNodeClass(reference.NodeClass);
            var nodeIdStr = reference.NodeId.ToString();

            var browseResult = new NodeBrowseResult
            {
                NodeId = nodeIdStr,
                DisplayName = reference.DisplayName.Text ?? string.Empty,
                BrowseName = reference.BrowseName.Name ?? string.Empty,
                NodeClass = nodeClass,
                HasChildren = nodeClass is NodeClassType.Object or NodeClassType.Variable,
            };

            if (nodeClass == NodeClassType.Variable)
            {
                browseResult.DataTypeName = await ResolveDataTypeNameAsync(
                    session,
                    nodeIdStr,
                    cancellationToken);
            }

            results.Add(browseResult);
        }

        LogBrowseChildrenSucceeded(parentNodeId, results.Count);
        return results;
    }

    /// <summary>
    /// Reads the node's attributes, and for a variable its value, data type and access level.
    /// </summary>
    /// <param name="session">The connected session.</param>
    /// <param name="nodeId">The node.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The attributes, or <see langword="null"/> when the node doesn't exist.</returns>
    private async Task<NodeAttributeSet?> InvokeReadNodeAttributesAsync(
        ISession session,
        string nodeId,
        CancellationToken cancellationToken)
    {
        LogReadAttributes(nodeId);

        Node? node;
        try
        {
            node = await session.ReadNodeAsync(NodeIdParser.Parse(nodeId, nameof(nodeId)), cancellationToken);
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNodeIdUnknown)
        {
            node = null;
        }

        if (node is null)
        {
            return null;
        }

        var attributes = new NodeAttributeSet
        {
            NodeId = nodeId,
            DisplayName = node.DisplayName.Text ?? string.Empty,
            BrowseName = node.BrowseName.Name ?? string.Empty,
            NodeClass = MapNodeClass(node.NodeClass),
            Description = node.Description.Text,
        };

        if (node is VariableNode variableNode)
        {
            await PopulateVariableAttributesAsync(
                session,
                nodeId,
                variableNode,
                attributes,
                cancellationToken);
        }

        LogReadAttributesSucceeded(nodeId);
        return attributes;
    }

    private static async Task PopulateVariableAttributesAsync(
        ISession session,
        string nodeId,
        VariableNode variableNode,
        NodeAttributeSet attributes,
        CancellationToken cancellationToken)
    {
        // Read value with timestamps
        ReadValueId[] nodesToRead =
        [
            new()
            {
                NodeId = NodeIdParser.Parse(nodeId, nameof(nodeId)),
                AttributeId = Attributes.Value,
            },
        ];

        var readResponse = await session.ReadAsync(
            requestHeader: null,
            0,
            TimestampsToReturn.Both,
            nodesToRead,
            cancellationToken);

        if (readResponse?.Results.Count > 0)
        {
            var dataValue = readResponse.Results[0];
            attributes.Value = dataValue.WrappedValue.ToInvariantString();
            attributes.StatusCode = dataValue.StatusCode.Code;
            attributes.ServerTimestamp = dataValue.ServerTimestamp.ToTimestamp();
        }

        // Resolve data type name
        var dataTypeNode = variableNode.DataType;
        if (!dataTypeNode.IsNull)
        {
            try
            {
                var dtNode = await session.ReadNodeAsync(dataTypeNode, cancellationToken);
                attributes.DataTypeName = dtNode?.DisplayName.Text;
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested && !ex.IsConnectionFailure())
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
            var node = await session.ReadNodeAsync(NodeIdParser.Parse(nodeId, nameof(nodeId)), cancellationToken);
            if (node is VariableNode variableNode && !variableNode.DataType.IsNull)
            {
                var dtNode = await session.ReadNodeAsync(variableNode.DataType, cancellationToken);
                return dtNode?.DisplayName.Text;
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested && !ex.IsConnectionFailure())
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