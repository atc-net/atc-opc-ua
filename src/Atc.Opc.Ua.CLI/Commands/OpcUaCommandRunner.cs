namespace Atc.Opc.Ua.CLI.Commands;

/// <summary>
/// Runs a command's operation on a client connected for it, and turns its outcome into the exit code.
/// </summary>
[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "A failed command is logged and returns the failure exit code.")]
[SuppressMessage("Major Code Smell", "S6667:Logging in a catch clause should pass the caught exception as a parameter.", Justification = "The CLI shows the failure's message, not its stack trace.")]
internal static class OpcUaCommandRunner
{
    /// <summary>
    /// Creates a client with the command's certificate settings, connects with the command's server and credentials,
    /// runs the operation, and disconnects again, also when the operation fails. A failure is logged as an error.
    /// </summary>
    /// <param name="clientFactory">Creates the client.</param>
    /// <param name="settings">The command's server, credentials and certificate settings.</param>
    /// <param name="logger">The command's logger.</param>
    /// <param name="operation">What the command does with the connected client.</param>
    /// <param name="cancellationToken">Cancellation token for the command.</param>
    /// <returns><see cref="ConsoleExitStatusCodes.Success"/>, or <see cref="ConsoleExitStatusCodes.Failure"/> when connecting or the operation failed.</returns>
    public static async Task<int> RunAsync(
        OpcUaClientFactory clientFactory,
        OpcUaBaseCommandSettings settings,
        ILogger logger,
        Func<IOpcUaClient, CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var client = clientFactory.Create(settings);
            var serverUrl = new Uri(settings.ServerUrl!);
            if (settings.UserName is { IsSet: true } userName)
            {
                await client.ConnectAsync(serverUrl, userName.Value, settings.Password!.Value, cancellationToken);
            }
            else
            {
                await client.ConnectAsync(serverUrl, cancellationToken);
            }

            try
            {
                await operation(client, cancellationToken);
            }
            finally
            {
                await DisconnectAsync(client, logger);
            }

            return ConsoleExitStatusCodes.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("The command was cancelled.");
            return ConsoleExitStatusCodes.Failure;
        }
        catch (global::Opc.Ua.ServiceResultException ex) when (ex.StatusCode == global::Opc.Ua.StatusCodes.BadCertificateUntrusted)
        {
            logger.LogError(ex.Message);
            logger.LogInformation("The server's certificate isn't trusted. Rerun with --auto-accept to accept it, or trust it as the warning above says.");
            return ConsoleExitStatusCodes.Failure;
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return ConsoleExitStatusCodes.Failure;
        }
        finally
        {
            stopwatch.Stop();
            logger.LogDebug($"Time for operation: {stopwatch.Elapsed.GetPrettyTime()}");
        }
    }

    /// <summary>
    /// The exception for a node that doesn't exist, which fails the command.
    /// </summary>
    /// <param name="nodeId">The node.</param>
    /// <returns>The exception to throw.</returns>
    public static InvalidOperationException NodeNotFound(string nodeId)
        => new($"Node '{nodeId}' doesn't exist.");

    private static async Task DisconnectAsync(
        IOpcUaClient client,
        ILogger logger)
    {
        try
        {
            await client.DisconnectAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"Disconnecting failed: {ex.Message}");
        }
    }
}