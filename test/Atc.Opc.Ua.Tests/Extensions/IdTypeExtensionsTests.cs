namespace Atc.Opc.Ua.Tests.Extensions;

public sealed class IdTypeExtensionsTests
{
    [Theory]
    [InlineData(IdType.Numeric, "Numeric")]
    [InlineData(IdType.String, "String")]
    [InlineData(IdType.Guid, "Guid")]
    [InlineData(IdType.Opaque, "Opaque")]
    public void GetIdentifierTypeName_KnownIdTypes_ShouldReturnExpectedName(
        IdType idType,
        string expectedName)
    {
        // Act
        var result = idType.GetIdentifierTypeName();

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void GetIdentifierTypeName_UnknownIdType_ShouldReturnUnknown()
    {
        // Arrange
        const IdType unknownIdType = (IdType)999;

        // Act
        var result = unknownIdType.GetIdentifierTypeName();

        // Assert
        result.Should().Be("Unknown");
    }
}