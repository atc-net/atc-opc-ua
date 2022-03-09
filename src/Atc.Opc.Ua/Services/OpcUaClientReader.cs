namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient Reader.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    private readonly List<NodeId> excludeNodes = new()
    {
        ObjectIds.Server,
    };

    public Task<NodeVariable?> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue)
    {
        ArgumentNullException.ThrowIfNull(nodeId);

        return InvokeReadNodeVariableAsync(nodeId, includeSampleValue);
    }

    public Task<IList<NodeVariable>?> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        return InvokeReadNodeVariablesAsync(nodeIds, includeSampleValues);
    }

    public async Task<NodeObject?> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth = 1)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return null;
        }

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return null;
        }

        LogSessionReadNodeObjectWithMaxDepth(nodeId, nodeObjectReadDepth);

        var readNode = Session!.ReadNode(nodeId);
        if (readNode is null)
        {
            LogSessionNodeNotFound(nodeId);
            return null;
        }

        if (readNode.NodeClass != NodeClass.Object)
        {
            LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Object);
            return null;
        }

        var parentNodeId = GetParentNodeId(nodeId);
        if (parentNodeId is null)
        {
            LogSessionParentNodeNotFound(nodeId);
            return null;
        }

        var nodeObject = readNode.MapToNodeObject(parentNodeId);

        if (nodeObject is not null &&
            (includeObjects || includeVariables) &&
            nodeObjectReadDepth >= 1)
        {
            await ReadChildNodes(nodeObject, 1, includeObjects, includeVariables, includeSampleValues, nodeObjectReadDepth);
        }

        LogSessionReadNodeObjectSucceeded(nodeId);
        return nodeObject;
    }

    private string? GetParentNodeId(
        string nodeId)
    {
        if (nodeId.Equals(ObjectIds.RootFolder.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var browseParentResults = BrowseBackwardsByNodeId(nodeId);
        if (browseParentResults.Count != 1)
        {
            return null;
        }

        var parentObject = browseParentResults[0];
        return parentObject.NodeId.ToString();
    }

    private async Task ReadChildNodes(
        NodeObject currentNode,
        int level,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth)
    {
        ArgumentNullException.ThrowIfNull(currentNode);

        if (excludeNodes.Find(x => x.ToString().Equals(currentNode.NodeId, StringComparison.Ordinal)) != null)
        {
            return;
        }

        LogSessionReadNodeObject(currentNode.NodeId);

        var browseChildResults = BrowseForwardByNodeId(currentNode.NodeId);
        if (!browseChildResults.Any())
        {
            return;
        }

        foreach (var result in browseChildResults)
        {
            await HandleChildBrowseResults(currentNode, level, includeObjects, includeVariables, includeSampleValues, nodeObjectReadDepth, result);
        }
    }

    private async Task<NodeVariable?> InvokeReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue)
    {
        if (!IsConnected())
        {
            LogSessionNotConnected();
            return null;
        }

        try
        {
            LogSessionReadNodeObject(nodeId);

            var readNode = Session!.ReadNode(nodeId);
            if (readNode is null)
            {
                LogSessionNodeNotFound(nodeId);
                return null;
            }

            if (readNode.NodeClass != NodeClass.Variable)
            {
                LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Variable);
                return null;
            }

            var browserParent = BrowserFactory.GetBackwardsBrowser(Session!);
            var browseParentResults = browserParent.Browse(new NodeId(nodeId));
            if (browseParentResults is null || browseParentResults.Count != 1)
            {
                LogSessionParentNodeNotFound(nodeId);
                return null;
            }

            DataValue? sampleValue = null;
            if (includeSampleValue)
            {
                sampleValue = await TryGetDataValueForVariable((VariableNode)readNode);
            }

            var parentObject = browseParentResults[0];
            var nodeVariable = readNode.MapToNodeVariableWithValue(parentObject.NodeId.ToString(), sampleValue);

            LogSessionReadNodeVariableSucceeded(nodeId);
            return nodeVariable;
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return null;
        }
    }

    private async Task<IList<NodeVariable>?> InvokeReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues)
    {
        if (!nodeIds.Any())
        {
            return null;
        }

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return null;
        }

        var result = new List<NodeVariable>();
        foreach (var nodeId in nodeIds)
        {
            var nodeVariable = await ReadNodeVariableAsync(nodeId, includeSampleValues);
            if (nodeVariable is not null)
            {
                result.Add(nodeVariable);
            }
        }

        return result;
    }

    private async Task HandleChildBrowseResults(
        NodeObject currentNode,
        int level,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth,
        ReferenceDescription result)
    {
        switch (result.NodeClass)
        {
            case NodeClass.Object:
                if (includeObjects)
                {
                    var childNode = result.MapToNodeObject(currentNode.NodeId);
                    if (childNode is not null)
                    {
                        if (nodeObjectReadDepth > level)
                        {
                            await ReadChildNodes(childNode, level + 1, includeObjects, includeVariables, includeSampleValues, nodeObjectReadDepth);
                        }

                        currentNode.NodeObjects.Add(childNode);
                    }
                }

                break;
            case NodeClass.Variable:
                if (includeVariables)
                {
                    await HandleBrowseResultVariableNode(currentNode, result, includeSampleValues);
                }

                break;
            default:
                LogSessionReadNodeNotSupportedNodeClass(currentNode.NodeId, result.NodeClass);
                return;
        }
    }

    private ReferenceDescriptionCollection BrowseForwardByNodeId(
        string nodeId)
    {
        try
        {
            var browser = BrowserFactory.GetForwardBrowser(Session!);
            return browser.Browse(new NodeId(nodeId));
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return new ReferenceDescriptionCollection();
        }
    }

    private ReferenceDescriptionCollection BrowseBackwardsByNodeId(
        string nodeId)
    {
        try
        {
            var browser = BrowserFactory.GetBackwardsBrowser(Session!);
            return browser.Browse(new NodeId(nodeId));
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return new ReferenceDescriptionCollection();
        }
    }

    private async Task HandleBrowseResultVariableNode(
        NodeObject node,
        ReferenceDescription referenceDescription,
        bool includeSampleValue)
    {
        var childNodeId = referenceDescription.NodeId.ToString();
        var childNode = Session!.ReadNode(childNodeId);

        DataValue? sampleValue = null;
        if (includeSampleValue)
        {
            sampleValue = await TryGetDataValueForVariable((VariableNode)childNode);
        }

        var nodeVariable = childNode.MapToNodeVariableWithValue(node.NodeId, sampleValue);
        if (nodeVariable is not null)
        {
            node.NodeVariables.Add(nodeVariable);
        }
    }

    private async Task<DataValue?> TryGetDataValueForVariable(
        VariableNode node,
        bool loadComplexTypeSystem = false)
    {
        DataValue? sampleValue = null;

        var nodesToRead = new ReadValueIdCollection
        {
            new()
            {
                NodeId = node.NodeId,
                AttributeId = Attributes.Value,
            },
        };

        if (loadComplexTypeSystem)
        {
            var complexTypeSystem = new ComplexTypeSystem(Session!);
            var nodeDataType = node.DataType;
            LogLoadingComplexTypeSystem(node.NodeId.ToString(), nodeDataType.ToString());
            await complexTypeSystem.LoadType(nodeDataType);
        }

        Session!.Read(
            requestHeader: null,
            0,
            TimestampsToReturn.Both,
            nodesToRead,
            out DataValueCollection resultValues,
            out DiagnosticInfoCollection _);

        if (resultValues is not null &&
            resultValues.Any() &&
            resultValues.Count == 1)
        {
            if (resultValues[0].Value is null)
            {
                var statusCode = resultValues[0].StatusCode;

                if (!loadComplexTypeSystem &&
                    statusCode.Code == StatusCodes.BadDataTypeIdUnknown)
                {
                    return await TryGetDataValueForVariable(node, loadComplexTypeSystem: true);
                }

                LogSessionReadNodeVariableValueFailure(node.NodeId.ToString(), statusCode.ToString());
                return sampleValue;
            }

            sampleValue = resultValues[0];
        }

        return sampleValue;
    }
}