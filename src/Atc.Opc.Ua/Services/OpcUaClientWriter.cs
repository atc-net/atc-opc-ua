namespace Atc.Opc.Ua.Services;

/// <summary>
/// Provides functionality to interact with an OPC UA server for writing node variables.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    /// <summary>
    /// Writes a single node to the server.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A tuple indicating success and an error message if applicable.</returns>
    public Task<(bool Succeeded, string? ErrorMessage)> WriteNodeAsync(
        string nodeId,
        object value,
        CancellationToken cancellationToken = default)
    {
        var nodesToWriteCollection = new WriteValueCollection();
        var writeVal = BuildWriteValue(nodeId, value);
        nodesToWriteCollection.Add(writeVal);

        return WriteNodeValueCollectionAsync(nodesToWriteCollection, cancellationToken);
    }

    /// <summary>
    /// Writes multiple nodes to the server.
    /// </summary>
    /// <param name="nodesToWrite">A dictionary containing node identifiers and values to write.</param>
    /// <param name="cancellationToken">Cancellation token for the operation</param>
    /// <returns>A tuple indicating success and an error message if applicable.</returns>
    public Task<(bool Succeeded, string? ErrorMessage)> WriteNodesAsync(
        IDictionary<string, object> nodesToWrite,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nodesToWrite);

        var nodesToWriteCollection = new WriteValueCollection();
        foreach (var (nodeId, value) in nodesToWrite)
        {
            var writeVal = BuildWriteValue(nodeId, value);
            nodesToWriteCollection.Add(writeVal);
        }

        return WriteNodeValueCollectionAsync(nodesToWriteCollection, cancellationToken);
    }

    /// <summary>
    /// Executes the write operation on the server.
    /// </summary>
    /// <param name="nodesToWriteCollection">The collection of nodes to write.</param>
    /// <returns>A tuple indicating success and an error message if applicable.</returns>
    private async Task<(bool Succeeded, string? ErrorMessage)> WriteNodeValueCollectionAsync(
        WriteValueCollection nodesToWriteCollection,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var writeResponse = await Session!.WriteAsync(requestHeader: null, nodesToWriteCollection, cancellationToken);
            if (writeResponse is null)
            {
                return (false, "Writing node variable(s) failed - missing write response");
            }

            if (writeResponse.Results.Any() &&
                StatusCode.IsGood(writeResponse.Results[0]))
            {
                return (true, null);
            }

            var errorMessage = writeResponse.Results[0].ToString();
            LogSessionWriteNodeVariableFailure(errorMessage);
            return (false, $"Writing node variable(s) failed: {errorMessage}.");
        }
        catch (Exception ex)
        {
            LogSessionWriteNodeVariableFailure(ex.Message);
            return (false, $"Writing node variable(s) failed: {ex.Message}.");
        }
    }

    /// <summary>
    /// Builds the WriteValue for a node.
    /// </summary>
    /// <param name="nodeId">The node identifier.</param>
    /// <param name="value">The value to write.</param>
    /// <returns>A WriteValue object ready for being sent to the server.</returns>
    private static WriteValue BuildWriteValue(
        string nodeId,
        object value)
        => new()
        {
            NodeId = new NodeId(nodeId),
            AttributeId = Attributes.Value,
            Value = new DataValue
            {
                Value = value,
            },
        };
}