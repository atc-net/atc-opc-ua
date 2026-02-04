namespace Atc.Opc.Ua.Tests.Extensions;

public sealed class NodeIdExtensionsTests
{
    [Fact]
    public void GetIdentifierAsString_NumericId_ShouldReturnNumberAsString()
    {
        // Arrange
        var nodeId = new NodeId(852);

        // Act
        var result = nodeId.GetIdentifierAsString();

        // Assert
        result.Should().Be("852");
    }

    [Fact]
    public void GetIdentifierAsString_NumericIdWithNamespace_ShouldReturnNumberAsString()
    {
        // Arrange
        var nodeId = new NodeId(3063, 3);

        // Act
        var result = nodeId.GetIdentifierAsString();

        // Assert
        result.Should().Be("3063");
    }

    [Fact]
    public void GetIdentifierAsString_StringId_ShouldReturnString()
    {
        // Arrange
        var nodeId = new NodeId("Root.Types.DataTypes.BaseDataType.Structure.IoT_DeviceAnalogue", 5);

        // Act
        var result = nodeId.GetIdentifierAsString();

        // Assert
        result.Should().Be("Root.Types.DataTypes.BaseDataType.Structure.IoT_DeviceAnalogue");
    }

    [Fact]
    public void GetIdentifierAsString_GuidId_ShouldReturnGuidAsString()
    {
        // Arrange
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789ABC");
        var nodeId = new NodeId(guid);

        // Act
        var result = nodeId.GetIdentifierAsString();

        // Assert
        result.Should().Be("12345678-1234-1234-1234-123456789abc");
    }

    [Fact]
    public void GetIdentifierAsString_OpaqueId_ShouldReturnHexString()
    {
        // Arrange
        var bytes = new byte[] { 0xA1, 0xB2, 0xC3 };
        var nodeId = new NodeId(bytes);

        // Act
        var result = nodeId.GetIdentifierAsString();

        // Assert
        result.Should().Be("A1-B2-C3");
    }

    [Fact]
    public void GetIdentifierAsString_OpaqueIdEmpty_ShouldReturnEmptyHexString()
    {
        // Arrange
        var bytes = Array.Empty<byte>();
        var nodeId = new NodeId(bytes);

        // Act
        var result = nodeId.GetIdentifierAsString();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetIdentifierAsString_NullNodeId_ShouldThrowArgumentNullException()
    {
        // Arrange
        NodeId nodeId = null!;

        // Act
        var act = () => nodeId.GetIdentifierAsString();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
