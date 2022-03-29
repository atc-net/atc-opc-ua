using System.Text;

namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient Command Execution.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    public (bool Succeeded, IList<MethodExecutionResult>? ExecutionResults, string? ErrorMessage) ExecuteMethod(
        string parentNodeId,
        string methodNodeId,
        List<MethodExecutionParameter> arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

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

            LogSessionExecuteCommandRequest(parentNodeId, methodNodeId, ArgumentsToString(arguments));

            Session!.Call(
                requestHeader: null,
                new CallMethodRequestCollection { request },
                out var results,
                out _);

            if (results is not null &&
                results.Any() &&
                StatusCode.IsGood(results[0].StatusCode))
            {
                return (
                    Succeeded: true,
                    MapMethodExecutionResults(results),
                    null);
            }

            var errorMessage = results![0].ToString();
            LogSessionExecuteCommandFailure(parentNodeId, methodNodeId, errorMessage!);
            return (
                Succeeded: false,
                null,
                $"Executing command failed: {errorMessage}.");
        }
        catch (Exception ex)
        {
            LogSessionExecuteCommandFailure(parentNodeId, methodNodeId, ex.Message);
            return (
                Succeeded: false,
                null,
                $"Executing command failed: {ex.Message}.");
        }
    }

    private static string ArgumentsToString(
        IReadOnlyList<MethodExecutionParameter> arguments)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < arguments.Count; i++)
        {
            sb.Append('(');
            sb.Append(arguments[i].DataEncoding);
            sb.Append(", ");
            sb.Append(arguments[i].Value);
            sb.Append(')');
            if (i != arguments.Count - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
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

    private static List<MethodExecutionResult> MapMethodExecutionResults(
        CallMethodResultCollection results)
    {
        var data = new List<MethodExecutionResult>();
        foreach (var outputArgument in results[0].OutputArguments)
        {
            if (!Enum<OpcUaDataEncodingType>.TryParse(
                    outputArgument.TypeInfo.BuiltInType.ToString(),
                    ignoreCase: true,
                    out var dataType))
            {
                continue;
            }

            var dataValue = string.Empty;
            if (outputArgument.Value is not null)
            {
                dataValue = outputArgument.Value.ToString();
            }

            data.Add(
                new MethodExecutionResult(
                    dataType,
                    dataValue!));
        }

        return data;
    }
}