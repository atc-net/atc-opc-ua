namespace Atc.Opc.Ua.Contracts.Tests;

public sealed class NodeBrowseResultTests
{
    [Fact]
    public void DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var result = new NodeBrowseResult();

        // Assert
        result.NodeId.Should().BeEmpty();
        result.DisplayName.Should().BeEmpty();
        result.BrowseName.Should().BeEmpty();
        result.NodeClass.Should().Be(NodeClassType.Unspecified);
        result.DataTypeName.Should().BeNull();
        result.HasChildren.Should().BeFalse();
    }

    [Fact]
    public void SetProperties_ShouldRetainValues()
    {
        // Arrange
        var result = new NodeBrowseResult
        {
            NodeId = "ns=2;i=1234",
            DisplayName = "Temperature",
            BrowseName = "2:Temperature",
            NodeClass = NodeClassType.Variable,
            DataTypeName = "Float",
            HasChildren = false,
        };

        // Assert
        result.NodeId.Should().Be("ns=2;i=1234");
        result.DisplayName.Should().Be("Temperature");
        result.BrowseName.Should().Be("2:Temperature");
        result.NodeClass.Should().Be(NodeClassType.Variable);
        result.DataTypeName.Should().Be("Float");
        result.HasChildren.Should().BeFalse();
    }

    [Fact]
    public void ToString_ShouldContainKeyProperties()
    {
        // Arrange
        var result = new NodeBrowseResult
        {
            NodeId = "i=85",
            DisplayName = "Objects",
            NodeClass = NodeClassType.Object,
            HasChildren = true,
        };

        // Act
        var str = result.ToString();

        // Assert
        str.Should().Contain("NodeId: i=85");
        str.Should().Contain("DisplayName: Objects");
        str.Should().Contain("NodeClass: Object");
        str.Should().Contain("HasChildren: True");
    }

    [Fact]
    public void JsonSerialization_ShouldRoundTrip()
    {
        // Arrange
        var result = new NodeBrowseResult
        {
            NodeId = "ns=2;i=1234",
            DisplayName = "Temperature",
            BrowseName = "2:Temperature",
            NodeClass = NodeClassType.Variable,
            DataTypeName = "Float",
            HasChildren = false,
        };

        // Act
        var json = JsonSerializer.Serialize(result);
        var deserialized = JsonSerializer.Deserialize<NodeBrowseResult>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.NodeId.Should().Be("ns=2;i=1234");
        deserialized.DisplayName.Should().Be("Temperature");
        deserialized.NodeClass.Should().Be(NodeClassType.Variable);
        deserialized.DataTypeName.Should().Be("Float");
    }
}
