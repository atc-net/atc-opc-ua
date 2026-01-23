namespace Atc.Opc.Ua.Contracts.Tests;

public sealed class OpcUaEnumDataTypeTests
{
    [Fact]
    public void OpcUaEnumMember_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var member = new OpcUaEnumMember();

        // Assert
        member.Value.Should().Be(0);
        member.Name.Should().BeEmpty();
        member.DisplayName.Should().BeEmpty();
        member.Description.Should().BeEmpty();
    }

    [Fact]
    public void OpcUaEnumMember_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var member = new OpcUaEnumMember
        {
            Value = 42,
            Name = "Running",
            DisplayName = "Server Running",
            Description = "The server is operational",
        };

        // Assert
        member.Value.Should().Be(42);
        member.Name.Should().Be("Running");
        member.DisplayName.Should().Be("Server Running");
        member.Description.Should().Be("The server is operational");
    }

    [Fact]
    public void OpcUaEnumMember_ToString_ShouldContainAllProperties()
    {
        // Arrange
        var member = new OpcUaEnumMember
        {
            Value = 1,
            Name = "Stopped",
            DisplayName = "Server Stopped",
            Description = "Server is not running",
        };

        // Act
        var result = member.ToString();

        // Assert
        result.Should().Contain("Value: 1");
        result.Should().Contain("Name: Stopped");
        result.Should().Contain("DisplayName: Server Stopped");
        result.Should().Contain("Description: Server is not running");
    }

    [Fact]
    public void OpcUaEnumMember_JsonSerialization_ShouldRoundTrip()
    {
        // Arrange
        var member = new OpcUaEnumMember
        {
            Value = 42,
            Name = "TestValue",
            DisplayName = "Test Display Name",
            Description = "A test description",
        };

        // Act
        var json = JsonSerializer.Serialize(member);
        var deserialized = JsonSerializer.Deserialize<OpcUaEnumMember>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(42);
        deserialized.Name.Should().Be("TestValue");
        deserialized.DisplayName.Should().Be("Test Display Name");
        deserialized.Description.Should().Be("A test description");
    }

    [Fact]
    public void OpcUaEnumDataType_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var dataType = new OpcUaEnumDataType();

        // Assert
        dataType.NodeId.Should().BeEmpty();
        dataType.Name.Should().BeEmpty();
        dataType.DisplayName.Should().BeEmpty();
        dataType.HasEnumValues.Should().BeFalse();
        dataType.Description.Should().BeEmpty();
        dataType.Members.Should().NotBeNull();
        dataType.Members.Should().BeEmpty();
    }

    [Fact]
    public void OpcUaEnumDataType_SetProperties_ShouldRetainValues()
    {
        // Arrange
        var dataType = new OpcUaEnumDataType
        {
            NodeId = "i=852",
            Name = "ServerState",
            DisplayName = "Server State",
            HasEnumValues = false,
            Description = "State of the server",
        };

        // Assert
        dataType.NodeId.Should().Be("i=852");
        dataType.Name.Should().Be("ServerState");
        dataType.DisplayName.Should().Be("Server State");
        dataType.HasEnumValues.Should().BeFalse();
        dataType.Description.Should().Be("State of the server");
    }

    [Fact]
    public void OpcUaEnumDataType_Members_CanAddMembers()
    {
        // Arrange
        var dataType = new OpcUaEnumDataType();

        // Act
        dataType.Members.Add(new OpcUaEnumMember { Value = 0, Name = "Running" });
        dataType.Members.Add(new OpcUaEnumMember { Value = 1, Name = "Stopped" });

        // Assert
        dataType.Members.Should().HaveCount(2);
        dataType.Members[0].Name.Should().Be("Running");
        dataType.Members[1].Name.Should().Be("Stopped");
    }

    [Fact]
    public void OpcUaEnumDataType_ToStringSimple_ShouldContainKeyInfo()
    {
        // Arrange
        var dataType = new OpcUaEnumDataType
        {
            Name = "ServerState",
            HasEnumValues = true,
        };
        dataType.Members.Add(new OpcUaEnumMember { Value = 0, Name = "Running" });
        dataType.Members.Add(new OpcUaEnumMember { Value = 1, Name = "Stopped" });

        // Act
        var result = dataType.ToStringSimple();

        // Assert
        result.Should().Contain("Name: ServerState");
        result.Should().Contain("MemberCount: 2");
        result.Should().Contain("HasEnumValues: True");
    }

    [Fact]
    public void OpcUaEnumDataType_ToString_ShouldContainAllProperties()
    {
        // Arrange
        var dataType = new OpcUaEnumDataType
        {
            NodeId = "i=852",
            Name = "ServerState",
            DisplayName = "Server State",
            HasEnumValues = false,
            Description = "State of the server",
        };
        dataType.Members.Add(new OpcUaEnumMember { Value = 0, Name = "Running" });

        // Act
        var result = dataType.ToString();

        // Assert
        result.Should().Contain("NodeId: i=852");
        result.Should().Contain("Name: ServerState");
        result.Should().Contain("DisplayName: Server State");
        result.Should().Contain("HasEnumValues: False");
        result.Should().Contain("Description: State of the server");
        result.Should().Contain("MemberCount: 1");
    }

    [Fact]
    public void OpcUaEnumDataType_JsonSerialization_ShouldRoundTrip()
    {
        // Arrange
        var dataType = new OpcUaEnumDataType
        {
            NodeId = "i=852",
            Name = "ServerState",
            DisplayName = "Server State",
            HasEnumValues = false,
            Description = "State of the server",
        };
        dataType.Members.Add(new OpcUaEnumMember
        {
            Value = 0,
            Name = "Running",
            DisplayName = "Running",
            Description = "Server is operational",
        });
        dataType.Members.Add(new OpcUaEnumMember
        {
            Value = 1,
            Name = "Stopped",
            DisplayName = "Stopped",
            Description = "Server is stopped",
        });

        // Act
        var json = JsonSerializer.Serialize(dataType);
        var deserialized = JsonSerializer.Deserialize<OpcUaEnumDataType>(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.NodeId.Should().Be("i=852");
        deserialized.Name.Should().Be("ServerState");
        deserialized.Members.Should().HaveCount(2);
        deserialized.Members[0].Name.Should().Be("Running");
        deserialized.Members[1].Name.Should().Be("Stopped");
    }
}