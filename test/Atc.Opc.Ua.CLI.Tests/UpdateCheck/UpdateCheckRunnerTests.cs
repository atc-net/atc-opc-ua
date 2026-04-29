namespace Atc.Opc.Ua.CLI.Tests.UpdateCheck;

public sealed class UpdateCheckRunnerTests
{
    private const string SuppressionVariable = "ATC_OPC_UA_NO_UPDATE_CHECK";
    private static readonly DateTimeOffset LastCheck = new(2026, 11, 11, 8, 0, 0, TimeSpan.Zero);
    private static readonly Version CurrentVersion = new(4, 0, 0);

    [Fact]
    public void GetLatestStableVersion_StableAndPrereleaseVersions_ShouldReturnTheHighestStableVersion()
        => UpdateCheckRunner.GetLatestStableVersion(["3.1.0", "3.2.0", "4.0.0-beta.1"]).Should().Be(new Version(3, 2, 0));

    [Fact]
    public void GetLatestStableVersion_TwoDigitMajorVersion_ShouldCompareVersionsAsNumbers()
        => UpdateCheckRunner.GetLatestStableVersion(["9.9.9", "10.0.0"]).Should().Be(new Version(10, 0, 0));

    [Fact]
    public void GetLatestStableVersion_OnlyPrereleaseAndUnparsableVersions_ShouldReturnNull()
        => UpdateCheckRunner.GetLatestStableVersion(["4.0.0-beta.1", "latest"]).Should().BeNull();

    [Fact]
    public void GetLatestStableVersion_NoVersions_ShouldReturnNull()
        => UpdateCheckRunner.GetLatestStableVersion([]).Should().BeNull();

    [Fact]
    public void IsSuppressed_NoUpdateCheckFlag_ShouldBeTrue()
        => UpdateCheckRunner.IsSuppressed(suppressedForProcess: true, _ => null).Should().BeTrue();

    [Theory]
    [InlineData(SuppressionVariable, "1")]
    [InlineData("CI", "true")]
    [InlineData("TF_BUILD", "True")]
    [InlineData("GITHUB_ACTIONS", "true")]
    public void IsSuppressed_SuppressionOrCiVariableSet_ShouldBeTrue(
        string variable,
        string value)
        => UpdateCheckRunner.IsSuppressed(
                suppressedForProcess: false,
                x => string.Equals(x, variable, StringComparison.Ordinal) ? value : null)
            .Should().BeTrue();

    [Fact]
    public void IsSuppressed_NoFlagAndNoVariables_ShouldBeFalse()
        => UpdateCheckRunner.IsSuppressed(suppressedForProcess: false, _ => null).Should().BeFalse();

    [Fact]
    public void IsCacheFresh_CheckedLessThanADayAgo_ShouldBeTrue()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(LastCheck);
        var cache = new UpdateCheckCache { LastCheck = LastCheck, LatestVersion = "4.0.0" };
        timeProvider.Advance(TimeSpan.FromHours(23));

        // Act
        var fresh = UpdateCheckRunner.IsCacheFresh(cache, timeProvider);

        // Assert
        fresh.Should().BeTrue();
    }

    [Fact]
    public void IsCacheFresh_CheckedADayAgo_ShouldBeFalse()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider(LastCheck);
        var cache = new UpdateCheckCache { LastCheck = LastCheck, LatestVersion = "4.0.0" };
        timeProvider.Advance(TimeSpan.FromDays(1));

        // Act
        var fresh = UpdateCheckRunner.IsCacheFresh(cache, timeProvider);

        // Assert
        fresh.Should().BeFalse();
    }

    [Theory]
    [InlineData("4.1.0", false, true)]
    [InlineData("4.1.0", true, false)]
    [InlineData("4.0.0", false, false)]
    [InlineData("3.2.0", false, false)]
    [InlineData("latest", false, false)]
    public void ShouldShowCachedUpdate_CachedVersion_ShouldShowTheHintOnlyForANewerVersionTheCliDidNotInstall(
        string latestVersion,
        bool updatePerformed,
        bool expected)
    {
        // Arrange
        var cache = new UpdateCheckCache { LastCheck = LastCheck, LatestVersion = latestVersion, UpdatePerformed = updatePerformed };

        // Act
        var show = UpdateCheckRunner.ShouldShowCachedUpdate(cache, CurrentVersion);

        // Assert
        show.Should().Be(expected);
    }
}