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

            var errorMessage = results![0].StatusCode.ToString();
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

        foreach (var (dataEncoding, value) in arguments)
        {
            switch (dataEncoding)
            {
                case OpcUaDataEncodingType.None:
                    break;
                case OpcUaDataEncodingType.Boolean:
                    if (bool.TryParse(value, out var boolResult))
                    {
                        variantCollection.Add(new Variant(boolResult));
                    }

                    break;
                case OpcUaDataEncodingType.SByte:
                    if (sbyte.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var sbyteResult))
                    {
                        variantCollection.Add(new Variant(sbyteResult));
                    }

                    break;
                case OpcUaDataEncodingType.Byte:
                    if (byte.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var byteResult))
                    {
                        variantCollection.Add(new Variant(byteResult));
                    }

                    break;
                case OpcUaDataEncodingType.Int16:
                    if (short.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var int16Result))
                    {
                        variantCollection.Add(new Variant(int16Result));
                    }

                    break;
                case OpcUaDataEncodingType.UInt16:
                    if (ushort.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var uint16Result))
                    {
                        variantCollection.Add(new Variant(uint16Result));
                    }

                    break;
                case OpcUaDataEncodingType.Int32:
                    if (int.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var int32Result))
                    {
                        variantCollection.Add(new Variant(int32Result));
                    }

                    break;
                case OpcUaDataEncodingType.UInt32:
                    if (uint.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var uint32Result))
                    {
                        variantCollection.Add(new Variant(uint32Result));
                    }

                    break;
                case OpcUaDataEncodingType.Int64:
                    if (long.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var int64Result))
                    {
                        variantCollection.Add(new Variant(int64Result));
                    }

                    break;
                case OpcUaDataEncodingType.UInt64:
                    if (ulong.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var uint64Result))
                    {
                        variantCollection.Add(new Variant(uint64Result));
                    }

                    break;
                case OpcUaDataEncodingType.Float:
                    if (float.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var floatResult))
                    {
                        variantCollection.Add(new Variant(floatResult));
                    }

                    break;
                case OpcUaDataEncodingType.Double:
                    if (double.TryParse(value, NumberStyles.Any, GlobalizationConstants.EnglishCultureInfo, out var doubleResult))
                    {
                        variantCollection.Add(new Variant(doubleResult));
                    }

                    break;
                case OpcUaDataEncodingType.String:
                    variantCollection.Add(new Variant(value));

                    break;
                case OpcUaDataEncodingType.DateTime:
                    if (DateTime.TryParse(value, GlobalizationConstants.EnglishCultureInfo, DateTimeStyles.None, out var dateTimeResult))
                    {
                        variantCollection.Add(new Variant(dateTimeResult));
                    }

                    break;
                case OpcUaDataEncodingType.Guid:
                    if (Guid.TryParse(value, out var guidResult))
                    {
                        variantCollection.Add(new Variant(guidResult));
                    }

                    break;
                case OpcUaDataEncodingType.ByteString:
                case OpcUaDataEncodingType.XmlElement:
                case OpcUaDataEncodingType.NodeId:
                case OpcUaDataEncodingType.ExpandedNodeId:
                case OpcUaDataEncodingType.StatusCode:
                case OpcUaDataEncodingType.QualifiedName:
                case OpcUaDataEncodingType.LocalizedText:
                case OpcUaDataEncodingType.ExtensionObject:
                case OpcUaDataEncodingType.DataValue:
                case OpcUaDataEncodingType.Variant:
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