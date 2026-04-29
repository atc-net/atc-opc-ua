namespace Atc.Opc.Ua.CLI.UpdateCheck;

/// <summary>
/// Performs an opportunistic NuGet version check on startup and, when a newer
/// stable version is found, attempts an automatic <c>dotnet tool update</c>.
/// Adapted from atc-claude-kanban's <c>UpdateCheckService</c> for a CLI context
/// (no DI host, no SSE, console notice only).
/// </summary>
internal static class UpdateCheckRunner
{
    private const string PackageId = "atc-opc-ua";
    private const string CacheDirectoryName = "atc-opc-ua";
    private const string CacheFileName = "update-check.json";
    private const string EnvironmentSuppressionVar = "ATC_OPC_UA_NO_UPDATE_CHECK";

    private const string Dim = "\e[90m";
    private const string BrightWhite = "\e[97m";
    private const string Cyan = "\e[36m";
    private const string Reset = "\e[0m";

    [SuppressMessage("Major Code Smell", "S1075:URIs should not be hardcoded", Justification = "NuGet API endpoint is stable")]
    private static readonly Uri NuGetIndexUri = new("https://api.nuget.org/v3-flatcontainer/atc-opc-ua/index.json");

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan UpdateTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);

    private static readonly HttpClient SharedHttpClient = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private static bool suppressedForProcess;

    /// <summary>
    /// Suppresses the update check for the lifetime of this process.
    /// Used by the <c>--no-update-check</c> CLI flag preprocessor in <c>Program.cs</c>.
    /// </summary>
    internal static void SuppressForThisProcess()
        => suppressedForProcess = true;

    /// <summary>
    /// Runs the update check. Honours suppression flags and CI detection,
    /// uses a 24h on-disk cache, and only ever logs a non-fatal warning on failure.
    /// </summary>
    /// <param name="currentVersionString">Current assembly informational version (without <c>+meta</c>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once the check finishes (success, suppression, or any handled failure).</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Update check must never crash the application")]
    internal static async Task RunAsync(
        string currentVersionString,
        CancellationToken cancellationToken)
    {
        if (IsSuppressed())
        {
            return;
        }

        if (!Version.TryParse(currentVersionString, out var currentVersion))
        {
            return;
        }

        try
        {
            var cacheFilePath = GetCacheFilePath();
            var cachedResult = await ReadCacheAsync(cacheFilePath, cancellationToken);

            if (cachedResult is not null &&
                IsCacheFresh(cachedResult, TimeProvider.System))
            {
                HandleCacheHit(cachedResult, currentVersion);
                return;
            }

            var latestVersion = await FetchLatestVersionAsync(cancellationToken);
            if (latestVersion is null ||
                latestVersion <= currentVersion)
            {
                await WriteCacheAsync(
                    cacheFilePath,
                    currentVersion.ToString(3),
                    updatePerformed: false,
                    cancellationToken);

                return;
            }

            await PerformUpdateAsync(
                cacheFilePath,
                currentVersion.ToString(3),
                latestVersion.ToString(3),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested — expected.
        }
        catch (Exception)
        {
            // Update check is best-effort; never propagate.
        }
    }

    /// <summary>
    /// Whether the update check is skipped: with <c>--no-update-check</c>, with <c>ATC_OPC_UA_NO_UPDATE_CHECK=1</c>, or on CI.
    /// </summary>
    /// <param name="suppressedForProcess">Whether <c>--no-update-check</c> was given.</param>
    /// <param name="getEnvironmentVariable">Reads an environment variable, e.g. <see cref="Environment.GetEnvironmentVariable(string)"/>.</param>
    /// <returns><see langword="true"/> when the check is skipped.</returns>
    internal static bool IsSuppressed(
        bool suppressedForProcess,
        Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        return suppressedForProcess
            || string.Equals(getEnvironmentVariable(EnvironmentSuppressionVar), "1", StringComparison.Ordinal)
            || IsRunningInCi(getEnvironmentVariable);
    }

    /// <summary>
    /// The highest stable version in NuGet's version index. Prerelease versions, e.g. <c>4.1.0-beta.1</c>, don't count.
    /// </summary>
    /// <param name="versions">The versions in the index.</param>
    /// <returns>The highest stable version, or <see langword="null"/> when there's none.</returns>
    internal static Version? GetLatestStableVersion(
        IEnumerable<string> versions)
        => versions
            .Where(v => !v.Contains('-', StringComparison.Ordinal))
            .Select(x => Version.TryParse(x, out var parsed) ? parsed : null)
            .Where(x => x is not null)
            .Max();

    /// <summary>
    /// Whether a cached result is recent enough to skip asking NuGet: less than a day old.
    /// </summary>
    /// <param name="cache">The cached result.</param>
    /// <param name="timeProvider">The clock.</param>
    /// <returns><see langword="true"/> when NuGet needn't be asked.</returns>
    internal static bool IsCacheFresh(
        UpdateCheckCache cache,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(timeProvider);

        return timeProvider.GetUtcNow() - cache.LastCheck < CacheTtl;
    }

    /// <summary>
    /// Whether a cached result shows the update hint: its version is newer than the running one, and the CLI didn't
    /// install it.
    /// </summary>
    /// <param name="cache">The cached result.</param>
    /// <param name="currentVersion">The running version.</param>
    /// <returns><see langword="true"/> when the hint is shown.</returns>
    internal static bool ShouldShowCachedUpdate(
        UpdateCheckCache cache,
        Version currentVersion)
    {
        ArgumentNullException.ThrowIfNull(cache);

        return !cache.UpdatePerformed
            && Version.TryParse(cache.LatestVersion, out var cachedLatest)
            && cachedLatest > currentVersion;
    }

    private static bool IsSuppressed()
        => IsSuppressed(suppressedForProcess, Environment.GetEnvironmentVariable);

    private static bool IsRunningInCi(
        Func<string, string?> getEnvironmentVariable)
        => !string.IsNullOrEmpty(getEnvironmentVariable("CI"))
        || !string.IsNullOrEmpty(getEnvironmentVariable("TF_BUILD"))
        || !string.IsNullOrEmpty(getEnvironmentVariable("GITHUB_ACTIONS"));

    private static string GetCacheFilePath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            CacheDirectoryName,
            CacheFileName);

    private static async Task<UpdateCheckCache?> ReadCacheAsync(
        string cacheFilePath,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(cacheFilePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
            return JsonSerializer.Deserialize<UpdateCheckCache>(json, JsonOptions);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    private static void HandleCacheHit(
        UpdateCheckCache cachedResult,
        Version currentVersion)
    {
        if (!ShouldShowCachedUpdate(cachedResult, currentVersion))
        {
            return;
        }

        PrintUpdateAvailable(
            currentVersion.ToString(3),
            cachedResult.LatestVersion);
    }

    private static async Task<Version?> FetchLatestVersionAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(HttpTimeout);

            var json = await SharedHttpClient.GetStringAsync(NuGetIndexUri, timeoutCts.Token);
            var index = JsonSerializer.Deserialize<NuGetVersionIndex>(json, JsonOptions);

            return index is null
                ? null
                : GetLatestStableVersion(index.Versions);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    private static async Task PerformUpdateAsync(
        string cacheFilePath,
        string currentVersionString,
        string latestVersionString,
        CancellationToken cancellationToken)
    {
        var updateSucceeded = await TryAutoUpdateAsync(cancellationToken);
        if (updateSucceeded)
        {
            PrintUpdateSuccess(latestVersionString);
        }
        else
        {
            PrintUpdateAvailable(currentVersionString, latestVersionString);
        }

        await WriteCacheAsync(
            cacheFilePath,
            latestVersionString,
            updateSucceeded,
            cancellationToken);
    }

    private static async Task<bool> TryAutoUpdateAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process();
#pragma warning disable S4036 // A .NET tool is updated with the dotnet on the PATH.
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"tool update -g {PackageId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
#pragma warning restore S4036

            process.Start();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(UpdateTimeout);

            await process.WaitForExitAsync(timeoutCts.Token);

            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or OperationCanceledException)
        {
            return false;
        }
    }

    private static void PrintUpdateSuccess(string latestVersion)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"  {Cyan}ℹ{Reset}  {BrightWhite}Update successful!{Reset} {Dim}v{latestVersion} will be used on your next run.{Reset}");
        System.Console.WriteLine();
    }

    private static void PrintUpdateAvailable(
        string currentVersion,
        string latestVersion)
    {
        System.Console.WriteLine();
        System.Console.WriteLine($"  {Cyan}ℹ{Reset}  {Dim}Update available:{Reset} {BrightWhite}{currentVersion}{Reset} {Dim}→{Reset} {BrightWhite}{latestVersion}{Reset}");
        System.Console.WriteLine($"     {Dim}Run:{Reset} {BrightWhite}dotnet tool update -g {PackageId}{Reset}");
        System.Console.WriteLine();
    }

    private static async Task WriteCacheAsync(
        string cacheFilePath,
        string latestVersion,
        bool updatePerformed,
        CancellationToken cancellationToken)
    {
        try
        {
            var directory = Path.GetDirectoryName(cacheFilePath);
            if (directory is not null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var cache = new UpdateCheckCache
            {
                LastCheck = TimeProvider.System.GetUtcNow(),
                LatestVersion = latestVersion,
                UpdatePerformed = updatePerformed,
            };

            var json = JsonSerializer.Serialize(cache, JsonOptions);
            await File.WriteAllTextAsync(cacheFilePath, json, cancellationToken);
        }
        catch (IOException)
        {
            // Best-effort — cache write failure is not critical.
        }
    }
}