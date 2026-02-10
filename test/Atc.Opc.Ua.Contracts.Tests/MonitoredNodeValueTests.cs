namespace Atc.Opc.Ua.Contracts.Tests;

public sealed class MonitoredNodeValueTests
{
    [Fact]
    public void ShouldDeriveFromEventArgs()
    {
        // Arrange & Act
        var value = new MonitoredNodeValue();

        // Assert
        value.Should().BeAssignableTo<EventArgs>();
    }

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
        value.ValueAsText.Should().BeNull();
        value.StatusCodeName.Should().BeNull();
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
            Value = 23.5f,
            ValueAsText = "23.5",
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
        value.Value.Should().Be(23.5f);
        value.ValueAsText.Should().Be("23.5");
        value.ServerTimestamp.Should().Be(timestamp);
        value.SourceTimestamp.Should().Be(timestamp);
        value.StatusCode.Should().Be(0u);
        value.DataTypeName.Should().Be("Float");
        value.AccessLevel.Should().Be(3);
        value.IsGood.Should().BeTrue();
    }

    [Theory]
    [InlineData(0x00000000u, true)] // Good
    [InlineData(0x00000480u, true)] // Good, with the Overflow bit of a monitored item's queue
    [InlineData(0x002D0000u, true)] // Good subcode (GoodSubscriptionTransferred)
    [InlineData(0x40000000u, false)] // Uncertain
    [InlineData(0x80000000u, false)] // Bad
    [InlineData(0x80340000u, false)] // BadNodeIdUnknown
    public void IsGood_StatusCode_ShouldReflectTheSeverity(
        uint statusCode,
        bool expected)
    {
        // Arrange
        var value = new MonitoredNodeValue { StatusCode = statusCode };

        // Act
        var result = value.IsGood;

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToString_ShouldContainKeyProperties()
    {
        // Arrange
        var value = new MonitoredNodeValue
        {
            NodeId = "ns=2;s=Test",
            DisplayName = "Test",
            ValueAsText = "42",
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
            Value = 23.5f,
            ValueAsText = "23.5",
            StatusCode = 0,
            StatusCodeName = "Good",
            DataTypeName = "Float",
        };

        // Act
        var json = JsonSerializer.Serialize(value);
        var deserialized = JsonSerializer.Deserialize<MonitoredNodeValue>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized.MonitoredItemHandle.Should().Be(7u);
        deserialized.NodeId.Should().Be("ns=2;s=Demo.Float");
        deserialized.DisplayName.Should().Be("Float");
        deserialized.ValueAsText.Should().Be("23.5");
        deserialized.StatusCodeName.Should().Be("Good");
        deserialized.TryGetValue<double>(out var number).Should().BeTrue();
        number.Should().Be(23.5);
    }

    [Fact]
    public void TryGetValue_FloatAsDouble_ShouldConvert()
    {
        // Arrange
        var value = new MonitoredNodeValue { Value = 21.5f };

        // Act
        var converted = value.TryGetValue<double>(out var result);

        // Assert
        converted.Should().BeTrue();
        result.Should().Be(21.5);
    }

    [Fact]
    public void TryGetValue_Int16AsInt_ShouldConvert()
    {
        // Arrange
        var value = new MonitoredNodeValue { Value = (short)-42 };

        // Act
        var converted = value.TryGetValue<int>(out var result);

        // Assert
        converted.Should().BeTrue();
        result.Should().Be(-42);
    }

    [Fact]
    public void TryGetValue_IntThatFitsAsShort_ShouldConvert()
    {
        // Arrange
        var value = new MonitoredNodeValue { Value = 1000 };

        // Act
        var converted = value.TryGetValue<short>(out var result);

        // Assert
        converted.Should().BeTrue();
        result.Should().Be(1000);
    }

    [Fact]
    public void TryGetValue_IntTooLargeForShort_ShouldFail()
        => new MonitoredNodeValue { Value = 100_000 }.TryGetValue<short>(out _).Should().BeFalse();

    [Fact]
    public void TryGetValue_NumberWithDecimalsAsInt_ShouldFail()
        => new MonitoredNodeValue { Value = 21.5 }.TryGetValue<int>(out _).Should().BeFalse();

    [Fact]
    public void TryGetValue_DoubleAsFloat_ShouldFail()
        => new MonitoredNodeValue { Value = 21.5 }.TryGetValue<float>(out _).Should().BeFalse();

    [Fact]
    public void TryGetValue_BooleanAsBoolean_ShouldReturnIt()
    {
        // Arrange
        var value = new MonitoredNodeValue { Value = true };

        // Act
        var converted = value.TryGetValue<bool>(out var result);

        // Assert
        converted.Should().BeTrue();
        result.Should().BeTrue();
    }

    [Fact]
    public void TryGetValue_TextAsInt_ShouldFail()
        => new MonitoredNodeValue { Value = "42" }.TryGetValue<int>(out _).Should().BeFalse();

    [Fact]
    public void TryGetValue_NullValue_ShouldFail()
        => new MonitoredNodeValue().TryGetValue<double>(out _).Should().BeFalse();

    [Fact]
    public void TryGetValue_Int16AsNullableInt_ShouldConvert()
    {
        // Arrange
        var value = new MonitoredNodeValue { Value = (short)7 };

        // Act
        var converted = value.TryGetValue<int?>(out var result);

        // Assert
        converted.Should().BeTrue();
        result.Should().Be(7);
    }

    [Fact]
    public void TryGetValue_JsonNumberWithDecimalsAsInt_ShouldFail()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new MonitoredNodeValue { Value = 23.5f });
        var deserialized = JsonSerializer.Deserialize<MonitoredNodeValue>(json);

        // Act
        var converted = deserialized.TryGetValue<int>(out _);

        // Assert
        converted.Should().BeFalse();
    }
}