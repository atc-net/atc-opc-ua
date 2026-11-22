namespace Atc.Opc.Ua.Tests.Snapshots.Support;

/// <summary>
/// Starts the OPC PLC simulator in Docker for the live snapshots, once for all of them, and removes it afterwards.
/// </summary>
/// <remarks>
/// The snapshots were recorded with exactly this image and these arguments, so change them only together with the
/// snapshots. The simulator advertises <c>opc.tcp://localhost:50000</c> as its endpoint, and the client connects to
/// the endpoint the server advertises, so the container binds the fixed port 50000 rather than a random one.
/// </remarks>
public sealed class OpcPlcSimulator : IAsyncLifetime
{
    private const string Image = "mcr.microsoft.com/iotedge/opc-plc:2.15.11";
    private const int Port = 50000;
    private const string ReadyMessage = "PLC simulation started";

    private IContainer? container;
    private string? notRunningReason;

    public async ValueTask InitializeAsync()
    {
        try
        {
            container = new ContainerBuilder(Image)
                .WithCommand($"--pn={Port}", "--autoaccept", "--ut", "--ph=localhost")
                .WithPortBinding(Port, Port)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(ReadyMessage))
                .Build();

            await container.StartAsync(TestContext.Current.CancellationToken);
        }
        catch (DockerUnavailableException ex)
        {
            notRunningReason = $"Docker isn't running, so the OPC PLC simulator can't start: {ex.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (container is not null)
        {
            await container.DisposeAsync();
        }
    }

    /// <summary>
    /// Connects anonymously, or skips the test when Docker isn't running.
    /// </summary>
    /// <param name="cancellationToken">The test's cancellation token.</param>
    /// <returns>The connected client; the caller disposes it.</returns>
    public async Task<OpcUaClient> ConnectAsync(
        CancellationToken cancellationToken)
    {
        if (notRunningReason is not null)
        {
            Assert.Skip(notRunningReason);
        }

        var client = new OpcUaClient(NullLogger<OpcUaClient>.Instance);

        try
        {
            await client.ConnectAsync(new Uri($"opc.tcp://localhost:{Port}"), cancellationToken);
            return client;
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }
}