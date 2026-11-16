namespace Atc.Opc.Ua.Services;

/// <summary>
/// OpcUaClient Command Execution.
/// </summary>
[SuppressMessage("Design", "MA0048:File name must match type name", Justification = "OK - By Design")]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "OK")]
public partial class OpcUaClient
{
    /// <inheritdoc/>
    public Task<IList<MethodExecutionResult>> ExecuteMethodAsync(
        string parentNodeId,
        string methodNodeId,
        List<MethodExecutionParameter> arguments,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentNodeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodNodeId);
        ArgumentNullException.ThrowIfNull(arguments);

        return InvokeExecuteMethodAsync(parentNodeId.Trim(), methodNodeId.Trim(), arguments, cancellationToken)
            .NormalizeCancellation(cancellationToken);
    }

    /// <summary>
    /// Calls the method, and checks the server's result.
    /// </summary>
    /// <param name="parentNodeId">The object the method belongs to.</param>
    /// <param name="methodNodeId">The method.</param>
    /// <param name="arguments">The method's input arguments.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The method's output arguments.</returns>
    /// <exception cref="ServiceResultException">The server rejected the call, with its status code.</exception>
    private async Task<IList<MethodExecutionResult>> InvokeExecuteMethodAsync(
        string parentNodeId,
        string methodNodeId,
        List<MethodExecutionParameter> arguments,
        CancellationToken cancellationToken)
    {
        EnsureConnected();

        var request = new CallMethodRequest
        {
            ObjectId = new NodeId(parentNodeId),
            MethodId = new NodeId(methodNodeId),
        };

        if (arguments.Count > 0)
        {
            HandleArguments(request, arguments);
        }

        LogSessionExecuteCommandRequest(parentNodeId, methodNodeId, ArgumentsToString(arguments));

        var callResponse = await CurrentSession.CallAsync(requestHeader: null, [request], cancellationToken);
        if (callResponse?.Results is not { Count: > 0 } results)
        {
            throw new ServiceResultException(StatusCodes.BadUnexpectedError, "The server didn't return a result for the method call.");
        }

        var statusCode = results[0].StatusCode.Code;
        if (StatusCode.IsBad(statusCode))
        {
            throw new ServiceResultException(
                statusCode,
                $"Calling method '{methodNodeId}' on '{parentNodeId}' failed: {GetStatusCodeName(statusCode) ?? $"0x{statusCode:X8}"}.");
        }

        return MapMethodExecutionResults(results);
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