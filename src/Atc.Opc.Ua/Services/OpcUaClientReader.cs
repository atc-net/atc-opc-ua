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
    /// <summary>
    /// Where the read in progress reports the nodes it skips, when its caller asked for them. It flows with the read's
    /// own async calls, so concurrent reads don't mix their lists.
    /// </summary>
    private static readonly AsyncLocal<ICollection<string>?> SkippedNodeIdsOfRead = new();

    private readonly List<NodeId> excludeNodes = [ObjectIds.Server];

    /// <inheritdoc/>
    public Task<NodeVariable?> ReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        int nodeVariableReadDepth = 0,
        ICollection<string>? skippedNodeIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        return ReadReportingSkippedNodesAsync(
                skippedNodeIds,
                () => InvokeReadNodeVariableAsync(nodeId.Trim(), includeSampleValue, nodeVariableReadDepth, cancellationToken))
            .NormalizeCancellation(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IList<NodeVariable>> ReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        int nodeVariableReadDepth = 0,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodeIds);

        if (nodeIds.Length == 0)
        {
            throw new ArgumentException("At least one node id is needed.", nameof(nodeIds));
        }

        if (nodeIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("A node id is missing or blank.", nameof(nodeIds));
        }

        return InvokeReadNodeVariablesAsync([.. nodeIds.Select(x => x.Trim())], includeSampleValues, nodeVariableReadDepth, cancellationToken)
            .NormalizeCancellation(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<NodeObject?> ReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth = 1,
        int nodeVariableReadDepth = 0,
        IReadOnlyCollection<string>? includeObjectNodeIds = null,
        IReadOnlyCollection<string>? excludeObjectNodeIds = null,
        IReadOnlyCollection<string>? includeVariableNodeIds = null,
        IReadOnlyCollection<string>? excludeVariableNodeIds = null,
        ICollection<string>? skippedNodeIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        return ReadReportingSkippedNodesAsync(
                skippedNodeIds,
                () => InvokeReadNodeObjectAsync(
                    nodeId.Trim(),
                    includeObjects,
                    includeVariables,
                    includeSampleValues,
                    nodeObjectReadDepth,
                    nodeVariableReadDepth,
                    includeObjectNodeIds,
                    excludeObjectNodeIds,
                    includeVariableNodeIds,
                    excludeVariableNodeIds,
                    cancellationToken))
            .NormalizeCancellation(cancellationToken);
    }

    /// <summary>
    /// Runs a read that reports the nodes it skips to <paramref name="skippedNodeIds"/>.
    /// </summary>
    /// <typeparam name="T">The type of the read's result.</typeparam>
    /// <param name="skippedNodeIds">Where to report skipped nodes, or <see langword="null"/>.</param>
    /// <param name="read">The read.</param>
    /// <returns>The read's result.</returns>
    [SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage", Justification = "The async method scopes the AsyncLocal value to the read; without it, the value would leak back to the caller.")]
    private static async Task<T> ReadReportingSkippedNodesAsync<T>(
        ICollection<string>? skippedNodeIds,
        Func<Task<T>> read)
    {
        // Set in this async method, so the value only flows into the read and is gone again for the caller.
        SkippedNodeIdsOfRead.Value = skippedNodeIds;
        return await read();
    }

    /// <summary>
    /// Skips a node that can't be read, or whose children can't be browsed, because of the node itself (see
    /// <see cref="OpcUaExceptionExtensions.IsNodeFailure"/>): logs it and reports it to the read's caller.
    /// </summary>
    /// <param name="nodeId">The node.</param>
    /// <param name="reason">Why it's skipped.</param>
    private void SkipNode(
        string nodeId,
        string reason)
    {
        LogSessionNodeSkipped(nodeId, reason);
        SkippedNodeIdsOfRead.Value?.Add(nodeId);
    }

    /// <summary>
    /// Reads an object and, down to the given depths, its child objects and variables. A child that doesn't exist or may
    /// not be read is skipped with a warning; any other failure fails the read.
    /// </summary>
    /// <returns>The object, or <see langword="null"/> when the node doesn't exist.</returns>
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK")]
    private async Task<NodeObject?> InvokeReadNodeObjectAsync(
        string nodeId,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth,
        int nodeVariableReadDepth,
        IReadOnlyCollection<string>? includeObjectNodeIds,
        IReadOnlyCollection<string>? excludeObjectNodeIds,
        IReadOnlyCollection<string>? includeVariableNodeIds,
        IReadOnlyCollection<string>? excludeVariableNodeIds,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        LogSessionReadNodeObjectWithMaxDepth(nodeId, nodeObjectReadDepth);

        var readNode = await ReadNodeOrDefaultAsync(nodeId, cancellationToken);
        if (readNode is null)
        {
            return null;
        }

        if (readNode.NodeClass != NodeClass.Object)
        {
            throw WrongNodeClass(nodeId, readNode.NodeClass, NodeClass.Object, nameof(nodeId));
        }

        var parentNodeId = await GetParentNodeIdAsync(nodeId, cancellationToken);
        if (parentNodeId is null)
        {
            LogSessionParentNodeNotFound(nodeId);
        }

        var nodeObject = readNode.MapToNodeObject(parentNodeId);

        if (nodeObject is not null &&
            (includeObjects || includeVariables) &&
            nodeObjectReadDepth >= 1)
        {
            // Track visited NodeIds to avoid reprocessing the same nodes when multiple references point to them
            var visited = new HashSet<string>(StringComparer.Ordinal)
            {
                nodeObject.NodeId,
            };

            await ReadChildNodesFromNodeObjectAsync(
                nodeObject,
                objectLevel: 1,
                includeObjects,
                includeVariables,
                includeSampleValues,
                nodeObjectReadDepth,
                nodeVariableReadDepth,
                includeObjectNodeIds,
                excludeObjectNodeIds,
                includeVariableNodeIds,
                excludeVariableNodeIds,
                visited,
                cancellationToken);
        }

        LogSessionReadNodeObjectSucceeded(nodeId);

        return nodeObject;
    }

    /// <summary>
    /// Reads a node, or returns <see langword="null"/> when it doesn't exist.
    /// </summary>
    /// <param name="nodeId">The node, e.g. <c>ns=2;s=Demo.Dynamic.Scalar.Float</c>.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The node, or <see langword="null"/> when the server doesn't know it.</returns>
    /// <exception cref="ArgumentException"><paramref name="nodeId"/> isn't a valid node id.</exception>
    private async Task<Node?> ReadNodeOrDefaultAsync(
        string nodeId,
        CancellationToken cancellationToken)
    {
        try
        {
            var node = await CurrentSession.ReadNodeAsync(NodeIdParser.Parse(nodeId, nameof(nodeId)), cancellationToken);
            if (node is null)
            {
                LogSessionNodeNotFound(nodeId);
            }

            return node;
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNodeIdUnknown)
        {
            LogSessionNodeNotFound(nodeId);
            return null;
        }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNodeIdInvalid)
        {
            throw new ArgumentException($"'{nodeId}' isn't a valid node id.", nameof(nodeId), ex);
        }
    }

    /// <summary>
    /// The exception for a node of another class than the call reads, e.g. an Object read as a Variable.
    /// </summary>
    /// <param name="nodeId">The node.</param>
    /// <param name="actual">The node's class.</param>
    /// <param name="expected">The class the call reads.</param>
    /// <param name="paramName">The caller's parameter with the node id.</param>
    /// <returns>The exception to throw.</returns>
    private static ArgumentException WrongNodeClass(
        string nodeId,
        NodeClass actual,
        NodeClass expected,
        string paramName)
        => new($"Node '{nodeId}' has the NodeClass {actual}, not {expected}.", paramName);

    /// <summary>
    /// Gets the parent node identifier of a specified node.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The identifier of the parent node.</returns>
    private async Task<string?> GetParentNodeIdAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        if (nodeId.Equals(ObjectIds.RootFolder.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var browseParentResults = await BrowseBackwardsByNodeIdAsync(nodeId, cancellationToken);
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
    /// <param name="objectLevel">The level of node object read depth.</param>
    /// <param name="includeObjects">Indicates whether to include objects in the result.</param>
    /// <param name="includeVariables">Indicates whether to include variables in the result.</param>
    /// <param name="includeSampleValues">Indicates whether to include sample values of the variables.</param>
    /// <param name="nodeObjectReadDepth">The depth to read the node object.</param>
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <param name="includeObjectNodeIds">The object node IDs to include.</param>
    /// <param name="excludeObjectNodeIds">The object node IDs to exclude.</param>
    /// <param name="includeVariableNodeIds">The variable node IDs to include.</param>
    /// <param name="excludeVariableNodeIds">The variable node IDs to exclude.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    private async Task ReadChildNodesFromNodeObjectAsync(
        NodeObject currentNode,
        int objectLevel,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth,
        int nodeVariableReadDepth,
        IReadOnlyCollection<string>? includeObjectNodeIds,
        IReadOnlyCollection<string>? excludeObjectNodeIds,
        IReadOnlyCollection<string>? includeVariableNodeIds,
        IReadOnlyCollection<string>? excludeVariableNodeIds,
        ISet<string> visited,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentNode);
        ArgumentNullException.ThrowIfNull(visited);

        if (!excludeNodes.Find(x => x.ToString().Equals(currentNode.NodeId, StringComparison.Ordinal)).IsNull)
        {
            return;
        }

        LogSessionReadNodeObject(currentNode.NodeId);

        var browseChildResults = await BrowseForwardByNodeIdAsync(currentNode.NodeId, cancellationToken);
        if (!browseChildResults.Any())
        {
            return;
        }

        foreach (var result in browseChildResults)
        {
            LogSessionHandlingNode(result.NodeId.ToString());

            await HandleChildBrowseResultsFromNodeObjectAsync(
                currentNode,
                objectLevel,
                includeObjects,
                includeVariables,
                includeSampleValues,
                nodeObjectReadDepth,
                nodeVariableReadDepth,
                result,
                includeObjectNodeIds,
                excludeObjectNodeIds,
                includeVariableNodeIds,
                excludeVariableNodeIds,
                visited,
                cancellationToken);
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
    /// <param name="variableLevel">
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
    /// <paramref name="variableLevel"/> equals or exceeds this value, no further browsing
    /// is performed.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes once all child variables (up to the
    /// specified depth) have been processed.</returns>
    private async Task ReadChildNodesFromNodeVariableAsync(
        NodeVariable currentNode,
        int variableLevel,
        bool includeSampleValues,
        int nodeVariableReadDepth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currentNode);

        if (!excludeNodes.Find(x => x.ToString().Equals(currentNode.NodeId, StringComparison.Ordinal)).IsNull)
        {
            return;
        }

        var browseChildResults = await BrowseForwardByNodeIdAsync(currentNode.NodeId, cancellationToken);
        if (!browseChildResults.Any())
        {
            return;
        }

        foreach (var result in browseChildResults)
        {
            await HandleChildBrowseResultsFromNodeVariableAsync(
                currentNode,
                variableLevel + 1,
                includeSampleValues,
                nodeVariableReadDepth,
                result,
                cancellationToken);
        }
    }

    /// <summary>
    /// Invokes the asynchronous read operation for a specified node variable.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="includeSampleValue">Indicates whether to include the sample value of the variable.</param>
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>The variable, or <see langword="null"/> when the node doesn't exist.</returns>
    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK")]
    private async Task<NodeVariable?> InvokeReadNodeVariableAsync(
        string nodeId,
        bool includeSampleValue,
        int nodeVariableReadDepth,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        LogSessionReadNodeVariable(nodeId);

        var readNode = await ReadNodeOrDefaultAsync(nodeId, cancellationToken);
        if (readNode is null)
        {
            return null;
        }

        if (readNode.NodeClass != NodeClass.Variable)
        {
            throw WrongNodeClass(nodeId, readNode.NodeClass, NodeClass.Variable, nameof(nodeId));
        }

        var browseParentResults = await BrowseBackwardsByNodeIdAsync(nodeId, cancellationToken);
        if (browseParentResults.Count != 1)
        {
            LogSessionParentNodeNotFound(nodeId);
        }

        var variableNode = (VariableNode)readNode;

        DataValue? sampleValue = null;
        if (includeSampleValue)
        {
            sampleValue = await TryGetDataValueForVariableAsync(variableNode, cancellationToken: cancellationToken);
        }

        // Resolve rich DataType information
        var resolvedTypeInfo = await dataTypeInfoResolver.ResolveAsync(variableNode, CurrentSession, cancellationToken);

        NodeVariable? nodeVariable;

        if (browseParentResults.Count == 1)
        {
            var parentObject = browseParentResults[0];
            nodeVariable = readNode.MapToNodeVariableWithValue(parentObject.NodeId.ToString(), sampleValue, resolvedTypeInfo);
        }
        else
        {
            nodeVariable = readNode.MapToNodeVariableWithValue(parentNodeId: null, sampleValue, resolvedTypeInfo);
        }

        LogSessionReadNodeVariableSucceeded(nodeId);

        if (nodeVariable is not null &&
            nodeVariableReadDepth >= 1)
        {
            await ReadChildNodesFromNodeVariableAsync(
                nodeVariable,
                variableLevel: 1,
                includeSampleValue,
                nodeVariableReadDepth,
                cancellationToken);
        }

        return nodeVariable;
    }

    /// <summary>
    /// Invokes the asynchronous read operation for specified node variables.
    /// </summary>
    /// <param name="nodeIds">The identifiers of the nodes.</param>
    /// <param name="includeSampleValues">Indicates whether to include the sample values of the variables.</param>
    /// <param name="nodeVariableReadDepth">The depth to read the node variable.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The variables, in the order of <paramref name="nodeIds"/>; a node that doesn't exist is left out, with a warning.</returns>
    private async Task<IList<NodeVariable>> InvokeReadNodeVariablesAsync(
        string[] nodeIds,
        bool includeSampleValues,
        int nodeVariableReadDepth,
        CancellationToken cancellationToken = default)
    {
        EnsureConnected();

        var result = new List<NodeVariable>(nodeIds.Length);
        foreach (var nodeId in nodeIds)
        {
            var nodeVariable = await InvokeReadNodeVariableAsync(nodeId, includeSampleValues, nodeVariableReadDepth, cancellationToken);
            if (nodeVariable is null)
            {
                LogSessionNodeLeftOut(nodeId);
                continue;
            }

            result.Add(nodeVariable);
        }

        return result;
    }

    /// <summary>
    /// Handles the browse results for child nodes.
    /// </summary>
    /// <param name="currentNode">The current node object.</param>
    /// <param name="objectLevel">The level of node object read depth.</param>
    /// <param name="includeObjects">Indicates whether to include objects in the result.</param>
    /// <param name="includeVariables">Indicates whether to include variables in the result.</param>
    /// <param name="includeSampleValues">Indicates whether to include sample values of the variables.</param>
    /// <param name="nodeObjectReadDepth">The depth to read the node object.</param>
    /// <param name="result">The browse result for a child node.</param>
    /// <param name="includeObjectNodeIds">The object node IDs to include.</param>
    /// <param name="excludeObjectNodeIds">The object node IDs to exclude.</param>
    /// <param name="includeVariableNodeIds">The variable node IDs to include.</param>
    /// <param name="excludeVariableNodeIds">The variable node IDs to exclude.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    private async Task HandleChildBrowseResultsFromNodeObjectAsync(
        NodeObject currentNode,
        int objectLevel,
        bool includeObjects,
        bool includeVariables,
        bool includeSampleValues,
        int nodeObjectReadDepth,
        int nodeVariableReadDepth,
        ReferenceDescription result,
        IReadOnlyCollection<string>? includeObjectNodeIds,
        IReadOnlyCollection<string>? excludeObjectNodeIds,
        IReadOnlyCollection<string>? includeVariableNodeIds,
        IReadOnlyCollection<string>? excludeVariableNodeIds,
        ISet<string> visited,
        CancellationToken cancellationToken = default)
    {
        switch (result.NodeClass)
        {
            case NodeClass.Object when includeObjects:
                var childNode = result.MapToNodeObject(currentNode.NodeId);
                if (childNode is not null)
                {
                    // Skip if already processed elsewhere in the graph to avoid duplicates and cycles
                    if (!visited.Add(childNode.NodeId))
                    {
                        break;
                    }

                    if (ShouldSkipObject(childNode.NodeId, includeObjectNodeIds, excludeObjectNodeIds))
                    {
                        break;
                    }

                    if (nodeObjectReadDepth > objectLevel)
                    {
                        await ReadChildNodesFromNodeObjectAsync(
                            childNode,
                            objectLevel + 1,
                            includeObjects,
                            includeVariables,
                            includeSampleValues,
                            nodeObjectReadDepth,
                            nodeVariableReadDepth,
                            includeObjectNodeIds,
                            excludeObjectNodeIds,
                            includeVariableNodeIds,
                            excludeVariableNodeIds,
                            visited,
                            cancellationToken);
                    }

                    // De-duplicate per parent
                    if (!currentNode.NodeObjects.Any(o => string.Equals(o.NodeId, childNode.NodeId, StringComparison.Ordinal)))
                    {
                        currentNode.NodeObjects.Add(childNode);
                    }
                }

                break;
            case NodeClass.Variable when includeVariables:
                if (ShouldSkipVariable(result.NodeId.ToString(), includeVariableNodeIds, excludeVariableNodeIds))
                {
                    break;
                }

                // Respect variable depth to avoid excessive scanning
                if (nodeVariableReadDepth > 0)
                {
                    await HandleVariableChildAsync(
                        currentNode,
                        variableLevel: 0,
                        includeSampleValues,
                        nodeVariableReadDepth,
                        result.NodeId.ToString(),
                        cancellationToken);
                }

                break;
            default:
                LogSessionReadNodeNotSupportedNodeClass(currentNode.NodeId, result.NodeClass);
                return;
        }
    }

    private static bool ShouldSkipObject(
        string nodeId,
        IReadOnlyCollection<string>? includeList,
        IReadOnlyCollection<string>? excludeList)
        => ShouldSkipNode(nodeId, includeList, excludeList);

    private static bool ShouldSkipVariable(
        string nodeId,
        IReadOnlyCollection<string>? includeList,
        IReadOnlyCollection<string>? excludeList)
        => ShouldSkipNode(nodeId, includeList, excludeList);

    private static bool ShouldSkipNode(
        string nodeId,
        IReadOnlyCollection<string>? includeList,
        IReadOnlyCollection<string>? excludeList)
    {
        if (excludeList is not null && excludeList.Contains(nodeId, StringComparer.Ordinal))
        {
            return true;
        }

        return includeList is not null &&
               includeList.Count > 0 &&
               !includeList.Contains(nodeId, StringComparer.Ordinal);
    }

    /// <summary>
    /// Handles a browse result returned while exploring the children of a
    /// <see cref="NodeVariable"/>. Only <see cref="NodeClass.Variable"/> references
    /// are valid; any other class is logged and ignored.
    /// </summary>
    /// <param name="currentNode">The parent <see cref="NodeVariable"/>.</param>
    /// <param name="variableLevel">Current recursion level (0 = root variable).</param>
    /// <param name="includeSampleValues">
    /// <see langword="true"/> to attempt reading a sample value for each variable;
    /// otherwise no value is read.
    /// </param>
    /// <param name="nodeVariableReadDepth">Maximum recursion depth for variable nodes.</param>
    /// <param name="result">The browse result that describes the child variable.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the child has been processed.</returns>
    private async Task HandleChildBrowseResultsFromNodeVariableAsync(
        NodeVariable currentNode,
        int variableLevel,
        bool includeSampleValues,
        int nodeVariableReadDepth,
        ReferenceDescription result,
        CancellationToken cancellationToken = default)
    {
        if (result.NodeClass != NodeClass.Variable)
        {
            LogSessionReadNodeNotSupportedNodeClass(currentNode.NodeId, result.NodeClass);
            return;
        }

        await HandleVariableChildAsync(
                currentNode,
                variableLevel: variableLevel,
                includeSampleValues,
                nodeVariableReadDepth,
                result.NodeId.ToString(),
                cancellationToken);
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
    /// <param name="variableLevel">Current recursion depth.</param>
    /// <param name="includeSampleValues">
    /// <see langword="true"/> to read a sample value for the variable;
    /// otherwise the value is omitted.
    /// </param>
    /// <param name="maxReadDepth">Maximum recursion depth allowed for variable nodes.</param>
    /// <param name="childNodeId">The OPC UA node‑id of the child variable.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A task that completes when the variable (and optionally its
    /// descendants) have been processed.</returns>
    private async Task HandleVariableChildAsync(
        NodeBase parentNode,
        int variableLevel,
        bool includeSampleValues,
        int maxReadDepth,
        string childNodeId,
        CancellationToken cancellationToken = default)
    {
        LogSessionReadNodeVariable(childNodeId);

        Node childNode;

        try
        {
            childNode = await CurrentSession.ReadNodeAsync(NodeIdParser.Parse(childNodeId, nameof(childNodeId)), cancellationToken);
        }
        catch (ServiceResultException ex) when (ex.IsNodeFailure())
        {
            SkipNode(childNodeId, ex.Message);
            return;
        }

        if (childNode is not VariableNode variableNode)
        {
            SkipNode(childNodeId, $"it has the NodeClass {childNode?.NodeClass}, not Variable");
            return;
        }

        DataValue? sampleValue = null;
        if (includeSampleValues)
        {
            sampleValue = await TryGetDataValueForVariableAsync(variableNode, cancellationToken: cancellationToken);
        }

        // Resolve rich DataType information
        var resolvedTypeInfo = await dataTypeInfoResolver.ResolveAsync(variableNode, CurrentSession, cancellationToken);

        var nodeVariable = childNode.MapToNodeVariableWithValue(parentNode.NodeId, sampleValue, resolvedTypeInfo);
        if (nodeVariable is null)
        {
            return;
        }

        // De-duplicate per parent
        if (!parentNode.NodeVariables.Any(v => string.Equals(v.NodeId, nodeVariable.NodeId, StringComparison.Ordinal)))
        {
            parentNode.NodeVariables.Add(nodeVariable);
        }

        if (variableLevel <= maxReadDepth)
        {
            await ReadChildNodesFromNodeVariableAsync(
                nodeVariable,
                variableLevel: variableLevel + 1,
                includeSampleValues,
                maxReadDepth,
                cancellationToken);
        }
    }

    /// <summary>
    /// Browses forward by node identifier to find child nodes.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A collection of reference descriptions for child nodes.</returns>
    private async Task<List<ReferenceDescription>> BrowseForwardByNodeIdAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var browser = BrowserFactory.GetForwardBrowser(CurrentSession);
            var references = await browser.BrowseAsync(NodeIdParser.Parse(nodeId, nameof(nodeId)), cancellationToken);
            return references.ToList();
        }
        catch (ServiceResultException ex) when (ex.IsNodeFailure())
        {
            SkipNode(nodeId, ex.Message);
            return [];
        }
    }

    /// <summary>
    /// Browses backwards by node identifier to find parent nodes.
    /// </summary>
    /// <param name="nodeId">The identifier of the node.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A collection of reference descriptions for parent nodes.</returns>
    private async Task<List<ReferenceDescription>> BrowseBackwardsByNodeIdAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var browser = BrowserFactory.GetBackwardsBrowser(CurrentSession);
            var references = await browser.BrowseAsync(NodeIdParser.Parse(nodeId, nameof(nodeId)), cancellationToken);
            return references.ToList();
        }
        catch (ServiceResultException ex) when (ex.IsNodeFailure())
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
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A Task representing the result of the asynchronous operation.</returns>
    private async Task<DataValue?> TryGetDataValueForVariableAsync(
        VariableNode node,
        bool loadComplexTypeSystem = false,
        CancellationToken cancellationToken = default)
    {
        ReadValueId[] nodesToRead =
        [
            new ReadValueId
            {
                NodeId = node.NodeId,
                AttributeId = Attributes.Value,
            },
        ];

        if (loadComplexTypeSystem)
        {
            // The loaded types stay registered with the session after the type system is disposed.
            using var complexTypeSystem = ComplexTypeSystem.Create(CurrentSession, telemetry);
            var nodeDataType = node.DataType;
            LogLoadingComplexTypeSystem(node.NodeId.ToString(), nodeDataType.ToString());
            await complexTypeSystem.LoadTypeAsync(nodeDataType, subTypes: false, throwOnError: false, ct: cancellationToken);
        }

        var readResponse = await CurrentSession.ReadAsync(
            requestHeader: null,
            0,
            TimestampsToReturn.Both,
            nodesToRead,
            cancellationToken);

        if (readResponse is null || readResponse.Results.Count != 1)
        {
            return null;
        }

        var resultValue = readResponse.Results[0];
        if (resultValue.WrappedValue.IsNull)
        {
            var statusCode = resultValue.StatusCode;

            if (!loadComplexTypeSystem &&
                statusCode == StatusCodes.BadDataTypeIdUnknown)
            {
                return await TryGetDataValueForVariableAsync(node, loadComplexTypeSystem: true, cancellationToken);
            }

            if (StatusCode.IsGood(statusCode))
            {
                // The value was read, and it's null, e.g. a variable of the Null data type: nothing went wrong.
                LogSessionReadNodeVariableValueNull(node.NodeId.ToString());
            }
            else
            {
                LogSessionReadNodeVariableValueEmpty(node.NodeId.ToString(), statusCode.ToString());
            }

            return null;
        }

        // A structure's value isn't kept as a sample: only its fields are, as variables of their own.
        return resultValue.WrappedValue.TypeInfo.BuiltInType == BuiltInType.ExtensionObject
            ? null
            : resultValue;
    }
}