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
            request.InputArguments = MethodArgumentMapper.ToVariants(arguments);
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

        return MethodArgumentMapper.ToResults(results[0].OutputArguments);
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
}