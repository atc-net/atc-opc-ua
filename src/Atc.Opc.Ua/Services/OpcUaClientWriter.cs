namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient Writer.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    public bool WriteNode(
        string nodeId,
        object value)
    {
        var nodesToWriteCollection = new WriteValueCollection();
        var writeVal = BuildWriteValue(nodeId, value);
        nodesToWriteCollection.Add(writeVal);

        return WriteNodeValueCollection(nodesToWriteCollection);
    }

    public bool WriteNodes(
        IDictionary<string, object> nodesToWrite)
    {
        ArgumentNullException.ThrowIfNull(nodesToWrite);

        var nodesToWriteCollection = new WriteValueCollection();
        foreach (var (nodeId, value) in nodesToWrite)
        {
            var writeVal = BuildWriteValue(nodeId, value);
            nodesToWriteCollection.Add(writeVal);
        }

        return WriteNodeValueCollection(nodesToWriteCollection);
    }

    private bool WriteNodeValueCollection(
        WriteValueCollection nodesToWriteCollection)
    {
        try
        {
            Session!.Write(
                requestHeader: null,
                nodesToWriteCollection,
                out StatusCodeCollection results,
                out DiagnosticInfoCollection _);

            if (results is not null &&
                results.Any() &&
                StatusCode.IsGood(results[0]))
            {
                return true;
            }

            LogSessionWriteNodeVariableFailure(results![0].ToString());
            return false;
        }
        catch (Exception ex)
        {
            LogSessionWriteNodeVariableFailure(ex.Message);
            return false;
        }
    }

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