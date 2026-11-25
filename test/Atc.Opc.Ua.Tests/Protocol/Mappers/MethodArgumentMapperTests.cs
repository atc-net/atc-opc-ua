namespace Atc.Opc.Ua.Tests.Protocol.Mappers;

public sealed class MethodArgumentMapperTests
{
    [Theory]
    [InlineData(OpcUaDataEncodingType.Boolean, "true", true)]
    [InlineData(OpcUaDataEncodingType.SByte, "-5", (sbyte)-5)]
    [InlineData(OpcUaDataEncodingType.Byte, "200", (byte)200)]
    [InlineData(OpcUaDataEncodingType.Int16, "-300", (short)-300)]
    [InlineData(OpcUaDataEncodingType.UInt16, "60000", (ushort)60000)]
    [InlineData(OpcUaDataEncodingType.Int32, "-70000", -70000)]
    [InlineData(OpcUaDataEncodingType.UInt32, "4000000000", 4000000000u)]
    [InlineData(OpcUaDataEncodingType.Int64, "-5000000000", -5000000000L)]
    [InlineData(OpcUaDataEncodingType.UInt64, "10000000000000000000", 10000000000000000000UL)]
    [InlineData(OpcUaDataEncodingType.Float, "1.5", 1.5f)]
    [InlineData(OpcUaDataEncodingType.Double, "2.25", 2.25d)]
    [InlineData(OpcUaDataEncodingType.String, "abc", "abc")]
    public void ToVariants_ValidValue_ShouldConvertToTheDataType(
        OpcUaDataEncodingType dataEncoding,
        string value,
        object expected)
        => MethodArgumentMapper
            .ToVariants([new MethodExecutionParameter(dataEncoding, value)])[0].AsBoxedObject()
            .Should().Be(expected);

    [Fact]
    public void ToVariants_Guid_ShouldConvertToGuid()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var variants = MethodArgumentMapper.ToVariants([new MethodExecutionParameter(OpcUaDataEncodingType.Guid, guid.ToString())]);

        // Assert
        variants[0].AsBoxedObject().Should().Be(new Uuid(guid));
    }

    [Fact]
    public void ToVariants_DateTimeWithoutTimeZone_ShouldReadItAsUtc()
    {
        // Act
        var variants = MethodArgumentMapper.ToVariants([new MethodExecutionParameter(OpcUaDataEncodingType.DateTime, "2026-11-20 12:00:00")]);

        // Assert
        variants[0].TryGetValue(out DateTimeUtc value).Should().BeTrue();
        var dateTime = value.ToDateTime();
        dateTime.Should().Be(new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc));
        dateTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void ToVariants_DateTimeWithOffset_ShouldConvertItToUtc()
    {
        // Act
        var variants = MethodArgumentMapper.ToVariants([new MethodExecutionParameter(OpcUaDataEncodingType.DateTime, "2026-11-20T12:00:00+01:00")]);

        // Assert
        variants[0].TryGetValue(out DateTimeUtc value).Should().BeTrue();
        var dateTime = value.ToDateTime();
        dateTime.Should().Be(new DateTime(2026, 11, 20, 11, 0, 0, DateTimeKind.Utc));
        dateTime.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(OpcUaDataEncodingType.Boolean, "yes")]
    [InlineData(OpcUaDataEncodingType.SByte, "200")]
    [InlineData(OpcUaDataEncodingType.Byte, "-1")]
    [InlineData(OpcUaDataEncodingType.Int16, "40000")]
    [InlineData(OpcUaDataEncodingType.UInt16, "-1")]
    [InlineData(OpcUaDataEncodingType.Int32, "abc")]
    [InlineData(OpcUaDataEncodingType.UInt32, "-1")]
    [InlineData(OpcUaDataEncodingType.Int64, "abc")]
    [InlineData(OpcUaDataEncodingType.UInt64, "-1")]
    [InlineData(OpcUaDataEncodingType.Float, "abc")]
    [InlineData(OpcUaDataEncodingType.Double, "abc")]
    [InlineData(OpcUaDataEncodingType.DateTime, "not a date")]
    [InlineData(OpcUaDataEncodingType.Guid, "not a guid")]
    public void ToVariants_InvalidValue_ShouldThrowNamingTheArgumentAndDataType(
        OpcUaDataEncodingType dataEncoding,
        string value)
        => FluentActions
            .Invoking(() => MethodArgumentMapper.ToVariants([new MethodExecutionParameter(dataEncoding, value)]))
            .Should().Throw<ArgumentException>()
            .WithMessage($"Method argument 1 ('{value}') isn't a valid {dataEncoding}.*");

    [Theory]
    [InlineData(OpcUaDataEncodingType.None)]
    [InlineData(OpcUaDataEncodingType.ByteString)]
    [InlineData(OpcUaDataEncodingType.NodeId)]
    [InlineData(OpcUaDataEncodingType.LocalizedText)]
    [InlineData(OpcUaDataEncodingType.ExtensionObject)]
    public void ToVariants_UnsupportedDataType_ShouldThrow(
        OpcUaDataEncodingType dataEncoding)
        => FluentActions
            .Invoking(() => MethodArgumentMapper.ToVariants([new MethodExecutionParameter(dataEncoding, "x")]))
            .Should().Throw<ArgumentException>()
            .WithMessage($"Method argument 1 has data type {dataEncoding}, which can't be sent as a method argument.*");

    [Fact]
    public void ToVariants_SecondArgumentInvalid_ShouldNameArgumentTwo()
    {
        // Arrange
        var arguments = new List<MethodExecutionParameter>
        {
            new(OpcUaDataEncodingType.Int32, "1"),
            new(OpcUaDataEncodingType.Int32, "abc"),
        };

        // Act
        var act = () => MethodArgumentMapper.ToVariants(arguments);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Method argument 2 ('abc') isn't a valid Int32.*");
    }

    [Fact]
    public void ToVariants_SeveralArguments_ShouldKeepTheirOrder()
    {
        // Arrange
        var arguments = new List<MethodExecutionParameter>
        {
            new(OpcUaDataEncodingType.Int32, "7"),
            new(OpcUaDataEncodingType.String, "seven"),
            new(OpcUaDataEncodingType.Boolean, "false"),
        };

        // Act
        var variants = MethodArgumentMapper.ToVariants(arguments);

        // Assert
        variants.Select(v => v.AsBoxedObject()).Should().Equal(7, "seven", false);
    }

    [Fact]
    public void ToResults_EmptyOutputBetweenOthers_ShouldKeepItAsNoneAndKeepThePositions()
    {
        // Arrange
        Variant[] outputArguments =
        [
            new Variant(42),
            Variant.Null,
            new Variant("ok"),
        ];

        // Act
        var results = MethodArgumentMapper.ToResults(outputArguments);

        // Assert
        results.Should().Equal(
            new MethodExecutionResult(OpcUaDataEncodingType.Int32, "42"),
            new MethodExecutionResult(OpcUaDataEncodingType.None, string.Empty),
            new MethodExecutionResult(OpcUaDataEncodingType.String, "ok"));
    }

    [Fact]
    public void ToResults_OutputOfABuiltInTypeWithoutAnEnumValue_ShouldGetNone()
    {
        // Arrange
        Variant[] outputArguments =
        [
            new Variant(new EnumValue(3)),
        ];

        // Act
        var results = MethodArgumentMapper.ToResults(outputArguments);

        // Assert
        results.Should().Equal(new MethodExecutionResult(OpcUaDataEncodingType.None, "3"));
    }

    [Fact]
    public void ToResults_ArrayOutput_ShouldBeJsonText()
    {
        // Arrange
        Variant[] outputArguments =
        [
            new Variant(new[] { 1, 2 }),
        ];

        // Act
        var results = MethodArgumentMapper.ToResults(outputArguments);

        // Assert
        results.Should().Equal(new MethodExecutionResult(OpcUaDataEncodingType.Int32, "[1,2]"));
    }

    [Fact]
    public void ToResults_DateTimeOutput_ShouldUseTheRoundTripFormat()
    {
        // Arrange
        Variant[] outputArguments =
        [
            new Variant(new DateTimeUtc(new DateTime(2026, 11, 20, 12, 0, 0, DateTimeKind.Utc))),
        ];

        // Act
        var results = MethodArgumentMapper.ToResults(outputArguments);

        // Assert
        results.Should().Equal(new MethodExecutionResult(OpcUaDataEncodingType.DateTime, "2026-11-20T12:00:00.0000000Z"));
    }
}