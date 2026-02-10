namespace Atc.Opc.Ua.Contracts.Tests;

public sealed class NodeAttributeSetTests
{
    [Fact]
    public void DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var attrs = new NodeAttributeSet();

        // Assert
        attrs.NodeId.Should().BeEmpty();
        attrs.DisplayName.Should().BeEmpty();
        attrs.BrowseName.Should().BeEmpty();
        attrs.NodeClass.Should().Be(NodeClassType.Unspecified);
        attrs.Description.Should().BeNull();
        attrs.DataTypeName.Should().BeNull();
        attrs.Value.Should().BeNull();
        attrs.AccessLevel.Should().BeNull();
        attrs.UserAccessLevel.Should().BeNull();
        attrs.StatusCode.Should().Be(0u);
        attrs.ServerTimestamp.Should().BeNull();
        attrs.IsWritable.Should().BeFalse();
        attrs.IsGood.Should().BeTrue();
    }

    [Fact]
    public void SetProperties_ShouldRetainValues()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var attrs = new NodeAttributeSet
        {
            NodeId = "ns=2;s=Demo.Float",
            DisplayName = "Float Value",
            BrowseName = "2:Float",
            NodeClass = NodeClassType.Variable,
            Description = "A floating point value",
            DataTypeName = "Float",
            Value = "23.5",
            AccessLevel = 3,
            UserAccessLevel = 3,
            StatusCode = 0,
            ServerTimestamp = timestamp,
            IsWritable = true,
        };

        // Assert
        attrs.NodeId.Should().Be("ns=2;s=Demo.Float");
        attrs.DisplayName.Should().Be("Float Value");
        attrs.NodeClass.Should().Be(NodeClassType.Variable);
        attrs.Description.Should().Be("A floating point value");
        attrs.DataTypeName.Should().Be("Float");
        attrs.Value.Should().Be("23.5");
        attrs.AccessLevel.Should().Be(3);
        attrs.UserAccessLevel.Should().Be(3);
        attrs.IsWritable.Should().BeTrue();
        attrs.IsGood.Should().BeTrue();
    }

    [Fact]
    public void IsGood_WhenStatusCodeNonZero_ShouldBeFalse()
    {
        // Arrange
        var attrs = new NodeAttributeSet { StatusCode = 0x80000000 };

        // Assert
        attrs.IsGood.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldContainKeyProperties()
    {
        // Arrange
        var attrs = new NodeAttributeSet
        {
            NodeId = "ns=2;s=Test",
            DisplayName = "Test",
            NodeClass = NodeClassType.Variable,
            Value = "42",
            IsWritable = true,
        };

        // Act
        var result = attrs.ToString();

        // Assert
        result.Should().Contain("NodeId: ns=2;s=Test");
        result.Should().Contain("DisplayName: Test");
        result.Should().Contain("NodeClass: Variable");
        result.Should().Contain("Value: 42");
        result.Should().Contain("IsWritable: True");
    }

    [Fact]
    public void JsonSerialization_ShouldRoundTrip()
    {
        // Arrange
        var attrs = new NodeAttributeSet
        {
            NodeId = "ns=2;s=Demo.Float",
            DisplayName = "Float",
            BrowseName = "2:Float",
            NodeClass = NodeClassType.Variable,
            DataTypeName = "Float",
            Value = "23.5",
            AccessLevel = 3,
            IsWritable = true,
        };

        // Act
        var json = JsonSerializer.Serialize(attrs);
        var deserialized = JsonSerializer.Deserialize<NodeAttributeSet>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.NodeId.Should().Be("ns=2;s=Demo.Float");
        deserialized.DisplayName.Should().Be("Float");
        deserialized.NodeClass.Should().Be(NodeClassType.Variable);
        deserialized.Value.Should().Be("23.5");
        deserialized.IsWritable.Should().BeTrue();
    }
}
