using System.Globalization;

namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient Command Execution.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    public (bool Succeeded, string? ErrorMessage) ExecuteMethod(
        string methodNodeId,
        string parentNodeId,
        List<MethodExecutionParameter> arguments)
    {
        try
        {
            var request = new CallMethodRequest
            {
                ObjectId = new NodeId(parentNodeId),
                MethodId = new NodeId(methodNodeId),
            };

            if (arguments.Any())
            {
                HandleArguments(request, arguments);
            }

            var requests = new CallMethodRequestCollection
            {
                request,
            };

            Session!.Call(
                requestHeader: null,
                requests,
                out CallMethodResultCollection results,
                out DiagnosticInfoCollection _);

            if (results is not null &&
                results.Any() &&
                StatusCode.IsGood(results[0].StatusCode))
            {
                return (true, null);
            }

            var errorMessage = results![0].ToString();
            //LogExecuteCommandFailure(errorMessage);
            return (false, $"Executing command failed: {errorMessage}.");
        }
        catch (Exception ex)
        {
            //LogExecuteCommandFailure(ex.Message);
            return (false, $"Executing command failed: {ex.Message}.");
        }
    }

    [SuppressMessage("Design", "MA0051:Method is too long", Justification = "OK")]
    private static void HandleArguments(
        CallMethodRequest request,
        List<MethodExecutionParameter> arguments)
    {
        var variantCollection = new VariantCollection();

        // TODO: Fill out
        foreach (var (dataEncoding, value) in arguments)
        {
            switch (dataEncoding)
            {
                case OpcUaDataEncodingType.None:
                    break;
                case OpcUaDataEncodingType.Boolean:
                    if (bool.TryParse(value, out var boolResult))
                    {
                        var variant = new Variant(boolResult);
                        variantCollection.Add(variant);
                    }

                    break;
                case OpcUaDataEncodingType.SByte:
                    break;
                case OpcUaDataEncodingType.Byte:
                    break;
                case OpcUaDataEncodingType.Int16:
                    break;
                case OpcUaDataEncodingType.UInt16:
                    break;
                case OpcUaDataEncodingType.Int32:
                    break;
                case OpcUaDataEncodingType.UInt32:
                    break;
                case OpcUaDataEncodingType.Int64:
                    break;
                case OpcUaDataEncodingType.UInt64:
                    break;
                case OpcUaDataEncodingType.Float:
                    break;
                case OpcUaDataEncodingType.Double:
                    if (double.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var doubleResult))
                    {
                        var variant = new Variant(doubleResult);
                        variantCollection.Add(variant);
                    }

                    break;
                case OpcUaDataEncodingType.String:
                    break;
                case OpcUaDataEncodingType.DateTime:
                    break;
                case OpcUaDataEncodingType.Guid:
                    break;
                case OpcUaDataEncodingType.ByteString:
                    break;
                case OpcUaDataEncodingType.XmlElement:
                    break;
                case OpcUaDataEncodingType.NodeId:
                    break;
                case OpcUaDataEncodingType.ExpandedNodeId:
                    break;
                case OpcUaDataEncodingType.StatusCode:
                    break;
                case OpcUaDataEncodingType.QualifiedName:
                    break;
                case OpcUaDataEncodingType.LocalizedText:
                    break;
                case OpcUaDataEncodingType.ExtensionObject:
                    break;
                case OpcUaDataEncodingType.DataValue:
                    break;
                case OpcUaDataEncodingType.Variant:
                    break;
                case OpcUaDataEncodingType.DiagnosticInfo:
                    break;
                default:
                    throw new SwitchCaseDefaultException(dataEncoding);
            }
        }

        request.InputArguments = variantCollection;
    }
}