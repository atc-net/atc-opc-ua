namespace Atc.Opc.Ua.Contracts.Tests;

public sealed class MonitoredNodeValueTests
{
    [Fact]
    public void DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var value = new MonitoredNodeValue();

        // Assert
        value.MonitoredItemHandle.Should().Be(0u);
        value.NodeId.Should().BeEmpty();
        value.DisplayName.Should().BeEmpty();
        value.Value.Should().BeNull();
        value.ServerTimestamp.Should().BeNull();
        value.SourceTimestamp.Should().BeNull();
        value.StatusCode.Should().Be(0u);
        value.DataTypeName.Should().BeNull();
        value.AccessLevel.Should().BeNull();
        value.IsGood.Should().BeTrue();
    }

    [Fact]
    public void SetProperties_ShouldRetainValues()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var value = new MonitoredNodeValue
        {
            MonitoredItemHandle = 42,
            NodeId = "ns=2;s=Demo.Float",
            DisplayName = "Float Value",
            Value = "23.5",
            ServerTimestamp = timestamp,
            SourceTimestamp = timestamp,
            StatusCode = 0,
            DataTypeName = "Float",
            AccessLevel = 3,
        };

        // Assert
        value.MonitoredItemHandle.Should().Be(42u);
        value.NodeId.Should().Be("ns=2;s=Demo.Float");
        value.DisplayName.Should().Be("Float Value");
        value.Value.Should().Be("23.5");
        value.ServerTimestamp.Should().Be(timestamp);
        value.SourceTimestamp.Should().Be(timestamp);
        value.StatusCode.Should().Be(0u);
        value.DataTypeName.Should().Be("Float");
        value.AccessLevel.Should().Be(3);
        value.IsGood.Should().BeTrue();
    }

    [Fact]
    public void IsGood_WhenStatusCodeNonZero_ShouldBeFalse()
    {
        // Arrange
        var value = new MonitoredNodeValue { StatusCode = 0x80000000 };

        // Assert
        value.IsGood.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldContainKeyProperties()
    {
        // Arrange
        var value = new MonitoredNodeValue
        {
            NodeId = "ns=2;s=Test",
            DisplayName = "Test",
            Value = "42",
            StatusCode = 0,
        };

        // Act
        var result = value.ToString();

        // Assert
        result.Should().Contain("NodeId: ns=2;s=Test");
        result.Should().Contain("DisplayName: Test");
        result.Should().Contain("Value: 42");
        result.Should().Contain("StatusCode: 0x00000000");
    }

    [Fact]
    public void JsonSerialization_ShouldRoundTrip()
    {
        // Arrange
        var value = new MonitoredNodeValue
        {
            MonitoredItemHandle = 7,
            NodeId = "ns=2;s=Demo.Float",
            DisplayName = "Float",
            Value = "23.5",
            StatusCode = 0,
            DataTypeName = "Float",
        };

        // Act
        var json = JsonSerializer.Serialize(value);
        var deserialized = JsonSerializer.Deserialize<MonitoredNodeValue>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.MonitoredItemHandle.Should().Be(7u);
        deserialized.NodeId.Should().Be("ns=2;s=Demo.Float");
        deserialized.DisplayName.Should().Be("Float");
        deserialized.Value.Should().Be("23.5");
    }
}
