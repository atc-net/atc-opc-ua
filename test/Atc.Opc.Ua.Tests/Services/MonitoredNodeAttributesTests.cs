namespace Atc.Opc.Ua.Tests.Services;

public sealed class MonitoredNodeAttributesTests
{
    [Fact]
    public void FromDataValues_AllGood_ShouldReturnTheAttributes()
    {
        // Arrange
        var displayName = new DataValue(new Variant(new LocalizedText("StepUp")));
        var dataType = new DataValue(new Variant(DataTypeIds.UInt32));
        var accessLevel = new DataValue(new Variant((byte)3));

        // Act
        var attributes = MonitoredNodeAttributes.FromDataValues(displayName, dataType, accessLevel);

        // Assert
        attributes.StatusCode.Should().Be(StatusCodes.Good);
        attributes.DisplayName.Should().Be("StepUp");
        attributes.DataTypeId.Should().Be(DataTypeIds.UInt32);
        attributes.AccessLevel.Should().Be(3);
    }

    [Fact]
    public void FromDataValues_UnknownNode_ShouldReturnTheBadStatusAndNoAttributes()
    {
        // Arrange
        var unknown = new DataValue(StatusCodes.BadNodeIdUnknown);

        // Act
        var attributes = MonitoredNodeAttributes.FromDataValues(unknown, unknown, unknown);

        // Assert
        attributes.StatusCode.Should().Be(StatusCodes.BadNodeIdUnknown);
        attributes.DisplayName.Should().BeNull();
        attributes.DataTypeId.Should().BeNull();
        attributes.AccessLevel.Should().BeNull();
    }

    [Fact]
    public void FromDataValues_AccessLevelNotReadable_ShouldKeepTheOtherAttributes()
    {
        // Arrange
        var displayName = new DataValue(new Variant(new LocalizedText("StepUp")));
        var dataType = new DataValue(new Variant(DataTypeIds.UInt32));
        var accessLevel = new DataValue(StatusCodes.BadUserAccessDenied);

        // Act
        var attributes = MonitoredNodeAttributes.FromDataValues(displayName, dataType, accessLevel);

        // Assert
        attributes.StatusCode.Should().Be(StatusCodes.Good);
        attributes.DisplayName.Should().Be("StepUp");
        attributes.DataTypeId.Should().Be(DataTypeIds.UInt32);
        attributes.AccessLevel.Should().BeNull();
    }

    [Fact]
    public void FromDataValues_ObjectNode_ShouldHaveNoDataTypeOrAccessLevel()
    {
        // Arrange
        var displayName = new DataValue(new Variant(new LocalizedText("Server")));
        var notAVariable = new DataValue(StatusCodes.BadAttributeIdInvalid);

        // Act
        var attributes = MonitoredNodeAttributes.FromDataValues(displayName, notAVariable, notAVariable);

        // Assert
        attributes.StatusCode.Should().Be(StatusCodes.Good);
        attributes.DataTypeId.Should().BeNull();
        attributes.AccessLevel.Should().BeNull();
    }
}