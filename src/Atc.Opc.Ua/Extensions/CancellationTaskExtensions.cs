namespace Atc.Opc.Ua.Extensions;

/// <summary>
/// Reports the caller's cancellation as <see cref="OperationCanceledException"/>.
/// </summary>
/// <remarks>
/// The OPC UA stack reports a cancelled request as its own exception, a <see cref="ServiceResultException"/> with
/// e.g. <c>BadRequestInterrupted</c>, so callers can't tell a cancelled call from a failed one.
/// </remarks>
internal static class CancellationTaskExtensions
{
    /// <summary>
    /// Awaits the task, and when it fails while <paramref name="cancellationToken"/> is cancelled, throws an
    /// <see cref="OperationCanceledException"/> with the stack's exception as <see cref="Exception.InnerException"/>.
    /// </summary>
    /// <typeparam name="T">The type of the task's result.</typeparam>
    /// <param name="task">The call.</param>
    /// <param name="cancellationToken">The caller's token, as passed to the call.</param>
    /// <returns>The task's result.</returns>
    public static async Task<T> NormalizeCancellation<T>(
        this Task<T> task,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            return await task;
        }
        catch (Exception ex) when (IsCallerCancellation(ex, cancellationToken))
        {
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
    }

    /// <inheritdoc cref="NormalizeCancellation{T}(Task{T}, CancellationToken)"/>
    public static async Task NormalizeCancellation(
        this Task task,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(task);

        try
        {
            await task;
        }
        catch (Exception ex) when (IsCallerCancellation(ex, cancellationToken))
        {
            throw new OperationCanceledException(ex.Message, ex, cancellationToken);
        }
    }

    private static bool IsCallerCancellation(
        Exception exception,
        CancellationToken cancellationToken)
        => cancellationToken.IsCancellationRequested &&
           exception is not OperationCanceledException;
}