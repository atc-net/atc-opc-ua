// ReSharper disable SuggestBaseTypeForParameter
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality to interact with an OPC UA server for reading node variables and objects.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    private readonly List<NodeId> excludeNodes = new()
    {
        ObjectIds.Server,
    };

    /// <summary>
    /// Asynchronously reads the variable of a specified node in the OPC UA server.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="includeSampleValue">Indicates whether to include the sample value of the variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    public Task<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue)
    {
        ArgumentNullException.ThrowIfNull(nodeId);

        return InvokeReadNodeVariableAsync(nodeId, includeSampleValue);
    }

    /// <summary>
    /// Asynchronously reads the variables of specified nodes in the OPC UA server.
    /// </summary>
    /// <param name="nodeIds">The identifiers of the nodes.</param>
    /// <param name="includeSampleValues">Indicates whether to include the sample values of the variables.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    public Task<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        return InvokeReadNodeVariablesAsync(nodeIds, includeSampleValues);
    }

    /// <summary>
    /// Asynchronously reads the object of a specified node in the OPC UA server.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="includeObjects">Indicates whether to include objects in the result.</param>
    /// <param name="includeVariables">Indicates whether to include variables in the result.</param>
    /// <param name="includeSampleValues">Indicates whether to include sample values of the variables.</param>
    /// <param name="nodeObjectReadDepth">The depth to read the node object.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    public async Task<(bool Succeeded, NodeObject? NodeObject, string? ErrorMessage)> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth = 1)
    {
        if (string.IsNullOrEmpty(nodeId))
        {
            return (false, null, "Missing nodeId.");
        }

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return (false, null, "Session is not connected.");
        }

        LogSessionReadNodeObjectWithMaxDepth(nodeId, nodeObjectReadDepth);

        var readNode = Session!.ReadNode(nodeId);
        if (readNode is null)
        {
            LogSessionNodeNotFound(nodeId);
            return (false, null, $"Could not find node by nodeId '{nodeId}'.");
        }

        if (readNode.NodeClass != NodeClass.Object)
        {
            LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Object);
            return (false, null, $"Node with nodeId '{nodeId}' has wrong NodeClass '{readNode.NodeClass}', expected '{NodeClass.Object}'.");
        }

        var parentNodeId = GetParentNodeId(nodeId);
        if (parentNodeId is null)
        {
            LogSessionParentNodeNotFound(nodeId);
        }

        var nodeObject = readNode.MapToNodeObject(parentNodeId);

        if (nodeObject is not null &&
            (includeObjects || includeVariables) &&
            nodeObjectReadDepth >= 1)
        {
            await ReadChildNodes(
                nodeObject,
                1,
                includeObjects,
                includeVariables,
                includeSampleValues,
                nodeObjectReadDepth);
        }

        LogSessionReadNodeObjectSucceeded(nodeId);

        return (true, nodeObject, null);
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

    /// <summary>
    /// Asynchronously reads child nodes of a specified node object.
    /// </summary>
    /// <param name="currentNode">The current node object.</param>
    /// <param name="level">The level of node object read depth.</param>
    /// <param name="includeObjects">Indicates whether to include objects in the result.</param>
    /// <param name="includeVariables">Indicates whether to include variables in the result.</param>
    /// <param name="includeSampleValues">Indicates whether to include sample values of the variables.</param>
    /// <param name="nodeObjectReadDepth">The depth to read the node object.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
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

    /// <summary>
    /// Invokes the asynchronous read operation for a specified node variable.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="includeSampleValue">Indicates whether to include the sample value of the variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    private async Task<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)> InvokeReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue)
    {
        if (!IsConnected())
        {
            LogSessionNotConnected();
            return (false, null, "Session is not connected.");
        }

        try
        {
            LogSessionReadNodeObject(nodeId);

            var readNode = Session!.ReadNode(nodeId);
            if (readNode is null)
            {
                LogSessionNodeNotFound(nodeId);
                return (false, null, $"Could not find node by nodeId '{nodeId}'.");
            }

            if (readNode.NodeClass != NodeClass.Variable)
            {
                LogSessionNodeHasWrongClass(nodeId, readNode.NodeClass, NodeClass.Variable);
                return (false, null, $"Node with nodeId '{nodeId}' has wrong NodeClass '{readNode.NodeClass}', expected '{NodeClass.Variable}'.");
            }

            var browseParentResults = BrowseBackwardsByNodeId(nodeId);
            if (browseParentResults.Count != 1)
            {
                LogSessionParentNodeNotFound(nodeId);
            }

            DataValue? sampleValue = null;
            if (includeSampleValue)
            {
                sampleValue = await TryGetDataValueForVariable((VariableNode)readNode);
            }

            NodeVariable? nodeVariable;

            if (browseParentResults.Count == 1)
            {
                var parentObject = browseParentResults[0];
                nodeVariable = readNode.MapToNodeVariableWithValue(parentObject.NodeId.ToString(), sampleValue);
            }
            else
            {
                nodeVariable = readNode.MapToNodeVariableWithValue(parentNodeId: null, sampleValue);
            }

            LogSessionReadNodeVariableSucceeded(nodeId);
            return (true, nodeVariable, null);
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return (false, null, $"Reading node with nodeId '{nodeId}' failed: '{ex.Message}'.");
        }
    }

    /// <summary>
    /// Invokes the asynchronous read operation for specified node variables.
    /// </summary>
    /// <param name="nodeIds">The identifiers of the nodes.</param>
    /// <param name="includeSampleValues">Indicates whether to include the sample values of the variables.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    private async Task<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)> InvokeReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues)
    {
        if (!nodeIds.Any())
        {
            return (false, null, "Missing NodeIds");
        }

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return (false, null, "Session is not connected.");
        }

        var result = new List<NodeVariable>();
        var errors = new List<string>();
        foreach (var nodeId in nodeIds)
        {
            var (succeeded, nodeVariable, errorMessage) = await ReadNodeVariableAsync(nodeId, includeSampleValues);
            if (succeeded)
            {
                result.Add(nodeVariable!);
            }
            else
            {
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    errors.Add(errorMessage);
                }
            }
        }

        return errors.Any()
            ? (true, result, string.Join(',', errors))
            : (true, result, null);
    }

    /// <summary>
    /// Handles the browse results for child nodes.
    /// </summary>
    /// <param name="currentNode">The current node object.</param>
    /// <param name="level">The level of node object read depth.</param>
    /// <param name="includeObjects">Indicates whether to include objects in the result.</param>
    /// <param name="includeVariables">Indicates whether to include variables in the result.</param>
    /// <param name="includeSampleValues">Indicates whether to include sample values of the variables.</param>
    /// <param name="nodeObjectReadDepth">The depth to read the node object.</param>
    /// <param name="result">The browse result for a child node.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
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
                            await ReadChildNodes(
                                childNode,
                                level + 1,
                                includeObjects,
                                includeVariables,
                                includeSampleValues,
                                nodeObjectReadDepth);
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

    /// <summary>
    /// Browses forward by node identifier to find child nodes.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <returns>A collection of reference descriptions for child nodes.</returns>
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

    /// <summary>
    /// Browses backwards by node identifier to find parent nodes.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <returns>A collection of reference descriptions for parent nodes.</returns>
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
            LogSessionReadParentNodeFailure(nodeId, ex.Message);
            return new ReferenceDescriptionCollection();
        }
    }

    /// <summary>
    /// Handles the browse result for a variable node.
    /// </summary>
    /// <param name="node">The current node object.</param>
    /// <param name="referenceDescription">The reference description of the child node.</param>
    /// <param name="includeSampleValue">Indicates whether to include the sample value of the variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
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

    /// <summary>
    /// Tries to get the data value for a specified variable node.
    /// </summary>
    /// <param name="node">The variable node.</param>
    /// <param name="loadComplexTypeSystem">Indicates whether to load the complex type system.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
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