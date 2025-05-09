// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable SwitchStatementHandlesSomeKnownEnumValuesWithDefault
namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality to interact with an OPC UA server for reading node variables and objects.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    private readonly List<NodeId> excludeNodes = [ObjectIds.Server];

    /// <summary>
    /// Asynchronously reads the variable of a specified node in the OPC UA server.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="includeSampleValue">Indicates whether to include the sample value of the variable.</param>
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    public Task<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        int nodeVariableReadDepth = 0)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return Task.FromResult<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)>((false, null, "Missing nodeId."));
        }

        nodeId = nodeId.Trim();

        return InvokeReadNodeVariableAsync(nodeId, includeSampleValue, nodeVariableReadDepth);
    }

    /// <summary>
    /// Asynchronously reads the variables of specified nodes in the OPC UA server.
    /// </summary>
    /// <param name="nodeIds">The identifiers of the nodes.</param>
    /// <param name="includeSampleValues">Indicates whether to include the sample values of the variables.</param>
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    public Task<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        int nodeVariableReadDepth = 0)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        if (nodeIds.Length == 0)
        {
            return Task.FromResult<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)>((false, null, "Missing nodeIds."));
        }

        nodeIds = nodeIds.Select(id => id.Trim()).ToArray();

        return nodeIds.Any(string.IsNullOrWhiteSpace)
            ? Task.FromResult<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)>((false, null, "One or more nodeIds are invalid."))
            : InvokeReadNodeVariablesAsync(nodeIds, includeSampleValues, nodeVariableReadDepth);
    }

    /// <summary>
    /// Asynchronously reads the object of a specified node in the OPC UA server.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="includeObjects">Indicates whether to include objects in the result.</param>
    /// <param name="includeVariables">Indicates whether to include variables in the result.</param>
    /// <param name="includeSampleValues">Indicates whether to include sample values of the variables.</param>
    /// <param name="nodeObjectReadDepth">The depth to read the node object.</param>
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    public async Task<(bool Succeeded, NodeObject? NodeObject, string? ErrorMessage)> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth = 1,
        int nodeVariableReadDepth = 0)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            return (false, null, "Missing nodeId.");
        }

        nodeId = nodeId.Trim();

        if (!IsConnected())
        {
            LogSessionNotConnected();
            return (false, null, "Session is not connected.");
        }

        LogSessionReadNodeObjectWithMaxDepth(nodeId, nodeObjectReadDepth);

        var readNode = await Session!.ReadNodeAsync(nodeId);
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
            await ReadChildNodesFromNodeObject(
                nodeObject,
                1,
                includeObjects,
                includeVariables,
                includeSampleValues,
                nodeObjectReadDepth,
                nodeVariableReadDepth);
        }

        LogSessionReadNodeObjectSucceeded(nodeId);

        return (true, nodeObject, null);
    }

    /// <summary>
    /// Gets the parent node identifier of a specified node.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <returns>The identifier of the parent node.</returns>
    private string? GetParentNodeId(string nodeId)
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
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    private async Task ReadChildNodesFromNodeObject(
        NodeObject currentNode,
        int level,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth,
        int nodeVariableReadDepth)
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
            await HandleChildBrowseResultsFromNodeObject(
                currentNode,
                level,
                includeObjects,
                includeVariables,
                includeSampleValues,
                nodeObjectReadDepth,
                nodeVariableReadDepth,
                result);
        }
    }

    /// <summary>
    /// Recursively browses the children of a <see cref="NodeVariable"/> and
    /// materialises them as <see cref="NodeVariable"/> instances in the in‑memory model.
    /// Skips the call if the node appears in the <c>excludeNodes</c> list, or if
    /// the server reports no forward references.
    /// </summary>
    /// <param name="currentNode">
    /// The <see cref="NodeVariable"/> whose forward references are to be browsed.
    /// Must not be <see langword="null"/>.
    /// </param>
    /// <param name="level">
    /// Current recursion depth (0 = root variable). Used to decide whether the
    /// maximum depth (<paramref name="nodeVariableReadDepth"/>) has been reached
    /// when processing grandchildren further down the call chain.
    /// </param>
    /// <param name="includeSampleValues">
    /// <see langword="true"/> to attempt reading a sample <see cref="DataValue"/>
    /// for each discovered child variable; otherwise the value is omitted.
    /// </param>
    /// <param name="nodeVariableReadDepth">
    /// Maximum recursion depth allowed for variable nodes. When
    /// <paramref name="level"/> equals or exceeds this value, no further browsing
    /// is performed.
    /// </param>
    /// <returns>A task that completes once all child variables (up to the
    /// specified depth) have been processed.</returns>
    private async Task ReadChildNodesFromNodeVariable(
        NodeVariable currentNode,
        int level,
        bool includeSampleValues,
        int nodeVariableReadDepth)
    {
        ArgumentNullException.ThrowIfNull(currentNode);

        if (excludeNodes.Find(x => x.ToString().Equals(currentNode.NodeId, StringComparison.Ordinal)) != null)
        {
            return;
        }

        LogSessionReadNodeVariable(currentNode.NodeId);

        var browseChildResults = BrowseForwardByNodeId(currentNode.NodeId);
        if (!browseChildResults.Any())
        {
            return;
        }

        foreach (var result in browseChildResults)
        {
            await HandleChildBrowseResultsFromNodeVariable(currentNode, level, includeSampleValues, nodeVariableReadDepth, result);
        }
    }

    /// <summary>
    /// Invokes the asynchronous read operation for a specified node variable.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="includeSampleValue">Indicates whether to include the sample value of the variable.</param>
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK")]
    private async Task<(bool Succeeded, NodeVariable? NodeVariable, string? ErrorMessage)> InvokeReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        int nodeVariableReadDepth)
    {
        if (!IsConnected())
        {
            LogSessionNotConnected();
            return (false, null, "Session is not connected.");
        }

        try
        {
            LogSessionReadNodeObject(nodeId);

            var readNode = await Session!.ReadNodeAsync(nodeId);
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

            if (nodeVariable is not null &&
                nodeVariableReadDepth >= 1)
            {
                await ReadChildNodesFromNodeVariable(
                    nodeVariable,
                    1,
                    includeSampleValue,
                    nodeVariableReadDepth);
            }

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
    /// <returns>
    /// A tuple containing:
    /// - `Succeeded`: True if all node variables were successfully read; false if any read operation failed.
    /// - `NodeVariables`: A list of successfully read node variables. If no variables were successfully read, this will be null.
    /// - `ErrorMessage`: A string containing error messages if any operation failed; otherwise, null.
    /// </returns>
    private async Task<(bool Succeeded, IList<NodeVariable>? NodeVariables, string? ErrorMessage)> InvokeReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        int nodeVariableReadDepth)
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
            var (succeeded, nodeVariable, errorMessage) = await ReadNodeVariableAsync(nodeId, includeSampleValues, nodeVariableReadDepth);
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
            ? (false, result, string.Join(',', errors))
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
    private async Task HandleChildBrowseResultsFromNodeObject(
        NodeObject currentNode,
        int level,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth,
        int nodeVariableReadDepth,
        ReferenceDescription result)
    {
        switch (result.NodeClass)
        {
            case NodeClass.Object when includeObjects:
                var childNode = result.MapToNodeObject(currentNode.NodeId);
                if (childNode is not null)
                {
                    if (nodeObjectReadDepth > level)
                    {
                        await ReadChildNodesFromNodeObject(
                            childNode,
                            level + 1,
                            includeObjects,
                            includeVariables,
                            includeSampleValues,
                            nodeObjectReadDepth,
                            nodeVariableReadDepth);
                    }

                    currentNode.NodeObjects.Add(childNode);
                }

                break;
            case NodeClass.Variable when includeVariables:
                await HandleVariableChild(
                        currentNode,
                        level,
                        includeSampleValues,
                        nodeVariableReadDepth,
                        result.NodeId.ToString());

                break;
            default:
                LogSessionReadNodeNotSupportedNodeClass(currentNode.NodeId, result.NodeClass);
                return;
        }
    }

    /// <summary>
    /// Handles a browse result returned while exploring the children of a
    /// <see cref="NodeVariable"/>. Only <see cref="NodeClass.Variable"/> references
    /// are valid; any other class is logged and ignored.
    /// </summary>
    /// <param name="currentNode">The parent <see cref="NodeVariable"/>.</param>
    /// <param name="level">Current recursion level (0 = root variable).</param>
    /// <param name="includeSampleValues">
    /// <see langword="true"/> to attempt reading a sample value for each variable;
    /// otherwise no value is read.
    /// </param>
    /// <param name="nodeVariableReadDepth">Maximum recursion depth for variable nodes.</param>
    /// <param name="result">The browse result that describes the child variable.</param>
    /// <returns>A task that completes when the child has been processed.</returns>
    private async Task HandleChildBrowseResultsFromNodeVariable(
        NodeVariable currentNode,
        int level,
        bool includeSampleValues,
        int nodeVariableReadDepth,
        ReferenceDescription result)
    {
        if (result.NodeClass != NodeClass.Variable)
        {
            LogSessionReadNodeNotSupportedNodeClass(currentNode.NodeId, result.NodeClass);
            return;
        }

        await HandleVariableChild(
                currentNode,
                level,
                includeSampleValues,
                nodeVariableReadDepth,
                result.NodeId.ToString());
    }

    /// <summary>
    /// Shared helper that turns a server‑side <see cref="VariableNode"/> into a
    /// client‑side <see cref="NodeVariable"/>, attaches it to
    /// <paramref name="parentNode"/>, and—if permitted by
    /// <paramref name="maxReadDepth"/>—recursively processes its own children.
    /// </summary>
    /// <param name="parentNode">
    /// The node (either <see cref="NodeObject"/> or <see cref="NodeVariable"/>) that becomes
    /// the parent of the new variable.
    /// </param>
    /// <param name="level">Current recursion depth.</param>
    /// <param name="includeSampleValues">
    /// <see langword="true"/> to read a sample value for the variable;
    /// otherwise the value is omitted.
    /// </param>
    /// <param name="maxReadDepth">Maximum recursion depth allowed for variable nodes.</param>
    /// <param name="childNodeId">The OPC UA node‑id of the child variable.</param>
    /// <returns>A task that completes when the variable (and optionally its
    /// descendants) have been processed.</returns>
    private async Task HandleVariableChild(
        NodeBase parentNode,
        int level,
        bool includeSampleValues,
        int maxReadDepth,
        string childNodeId)
    {
        var childNode = await Session!.ReadNodeAsync(childNodeId);

        DataValue? sampleValue = null;
        if (includeSampleValues)
        {
            sampleValue = await TryGetDataValueForVariable((VariableNode)childNode);
        }

        var nodeVariable = childNode.MapToNodeVariableWithValue(parentNode.NodeId, sampleValue);
        if (nodeVariable is null)
        {
            return;
        }

        parentNode.NodeVariables.Add(nodeVariable);

        if (level < maxReadDepth)
        {
            await ReadChildNodesFromNodeVariable(
                    nodeVariable,
                    level + 1,
                    includeSampleValues,
                    maxReadDepth);
        }
    }

    /// <summary>
    /// Browses forward by node identifier to find child nodes.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <returns>A collection of reference descriptions for child nodes.</returns>
    private ReferenceDescriptionCollection BrowseForwardByNodeId(string nodeId)
    {
        try
        {
            var browser = BrowserFactory.GetForwardBrowser(Session!);
            return browser.Browse(new NodeId(nodeId));
        }
        catch (Exception ex)
        {
            LogSessionReadNodeFailure(nodeId, ex.Message);
            return [];
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
            return [];
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
            new ReadValueId
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
            var resultValue = resultValues[0];
            if (resultValue.Value is null)
            {
                var statusCode = resultValue.StatusCode;

                if (!loadComplexTypeSystem &&
                    statusCode.Code == StatusCodes.BadDataTypeIdUnknown)
                {
                    return await TryGetDataValueForVariable(node, loadComplexTypeSystem: true);
                }

                LogSessionReadNodeVariableValueFailure(node.NodeId.ToString(), statusCode.ToString());
                return sampleValue;
            }

            if (resultValue.Value is ExtensionObject or ExtensionObject[])
            {
                return sampleValue;
            }

            sampleValue = resultValue;
        }

        return sampleValue;
    }
}