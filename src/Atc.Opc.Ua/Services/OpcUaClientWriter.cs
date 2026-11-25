namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality to interact with an OPC UA server for writing node variables.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    /// <inheritdoc/>
    public Task WriteNodeAsync(
        string nodeId,
        object value,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nodeId);

        List<WriteValue> nodesToWriteCollection = [BuildWriteValue(nodeId.Trim(), value, nameof(value))];

        return WriteNodeValueCollectionAsync(nodesToWriteCollection, cancellationToken).NormalizeCancellation(cancellationToken);
    }

    /// <inheritdoc/>
    public Task WriteNodesAsync(
        IDictionary<string, object> nodesToWrite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodesToWrite);

        if (nodesToWrite.Count == 0)
        {
            throw new ArgumentException("At least one node to write is needed.", nameof(nodesToWrite));
        }

        if (nodesToWrite.Keys.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("A node id is missing or blank.", nameof(nodesToWrite));
        }

        var nodesToWriteCollection = new List<WriteValue>(nodesToWrite.Count);
        foreach (var (nodeId, value) in nodesToWrite)
        {
            nodesToWriteCollection.Add(BuildWriteValue(nodeId.Trim(), value, nameof(nodesToWrite)));
        }

        return WriteNodeValueCollectionAsync(nodesToWriteCollection, cancellationToken).NormalizeCancellation(cancellationToken);
    }

    /// <summary>
    /// Writes the values in one call, and checks the server's result for every node.
    /// </summary>
    /// <param name="nodesToWriteCollection">The nodes and values to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ServiceResultException">The server rejected a value, with the first failed node's status code.</exception>
    private async Task WriteNodeValueCollectionAsync(
        List<WriteValue> nodesToWriteCollection,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        var writeResponse = await CurrentSession.WriteAsync(requestHeader: null, nodesToWriteCollection, cancellationToken);
        if (writeResponse is null || writeResponse.Results.Count != nodesToWriteCollection.Count)
        {
            throw new ServiceResultException(StatusCodes.BadUnexpectedError, "The server didn't return a result for every node written.");
        }

        List<(NodeId NodeId, uint Code)> failed = [.. nodesToWriteCollection
            .Zip(writeResponse.Results.ToArray() ?? [], (node, result) => (node.NodeId, result.Code))
            .Where(x => StatusCode.IsBad(x.Code))];

        if (failed.Count == 0)
        {
            return;
        }

        var details = string.Join(", ", failed.Select(x => $"'{x.NodeId}': {GetStatusCodeName(x.Code) ?? $"0x{x.Code:X8}"}"));
        throw new ServiceResultException(failed[0].Code, $"Writing failed for {details}.");
    }

    /// <summary>
    /// Builds the WriteValue for a node.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="paramName">The caller's parameter, for an exception.</param>
    /// <returns>A WriteValue object ready for being sent to the server.</returns>
    /// <exception cref="ArgumentException">The node id isn't valid, or the value's type has no OPC UA type.</exception>
    private static WriteValue BuildWriteValue(
        string nodeId,
        object value,
        string paramName)
        => new()
        {
            NodeId = NodeIdParser.Parse(nodeId, paramName),
            AttributeId = Attributes.Value,
            Value = new DataValue(VariantExtensions.ToVariant(value, paramName)),
        };
}