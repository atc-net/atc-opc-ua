namespace Atc.Opc.Ua.CLI;

public static class ConsoleHelper
{
    /// <summary>
    /// Prints the ATC OPC UA startup banner (Braille logo + ATC block + info rows).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the banner has been printed.</returns>
    public static Task WriteHeaderAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        System.Console.OutputEncoding = Encoding.UTF8;

        var version = typeof(ConsoleHelper).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion.Split('+')[0] ?? "dev";

        if (!System.Console.IsOutputRedirected)
        {
            StartupBanner.Print(version);
        }

        return Task.CompletedTask;
    }
}
