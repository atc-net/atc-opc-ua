namespace Atc.Opc.Ua.Contracts.Tests;

public sealed class SubscriptionOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var options = new SubscriptionOptions();

        // Assert
        options.PublishingIntervalMs.Should().Be(250);
        options.SamplingIntervalMs.Should().Be(250);
        options.QueueSize.Should().Be(10u);
        options.DiscardOldest.Should().BeTrue();
    }

    [Fact]
    public void SetProperties_ShouldRetainValues()
    {
        // Arrange
        var options = new SubscriptionOptions
        {
            PublishingIntervalMs = 500,
            SamplingIntervalMs = 100,
            QueueSize = 20,
            DiscardOldest = false,
        };

        // Assert
        options.PublishingIntervalMs.Should().Be(500);
        options.SamplingIntervalMs.Should().Be(100);
        options.QueueSize.Should().Be(20u);
        options.DiscardOldest.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldContainAllProperties()
    {
        // Arrange
        var options = new SubscriptionOptions
        {
            PublishingIntervalMs = 250,
            SamplingIntervalMs = 250,
            QueueSize = 10,
            DiscardOldest = true,
        };

        // Act
        var result = options.ToString();

        // Assert
        result.Should().Contain("PublishingIntervalMs: 250");
        result.Should().Contain("SamplingIntervalMs: 250");
        result.Should().Contain("QueueSize: 10");
        result.Should().Contain("DiscardOldest: True");
    }

    [Fact]
    public void JsonSerialization_ShouldRoundTrip()
    {
        // Arrange
        var options = new SubscriptionOptions
        {
            PublishingIntervalMs = 1000,
            SamplingIntervalMs = 500,
            QueueSize = 5,
            DiscardOldest = false,
        };

        // Act
        var json = JsonSerializer.Serialize(options);
        var deserialized = JsonSerializer.Deserialize<SubscriptionOptions>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.PublishingIntervalMs.Should().Be(1000);
        deserialized.SamplingIntervalMs.Should().Be(500);
        deserialized.QueueSize.Should().Be(5u);
        deserialized.DiscardOldest.Should().BeFalse();
    }
}
