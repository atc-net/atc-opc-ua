namespace Atc.Opc.Ua.Tests.Extensions;

public sealed class VariantExtensionsTests
{
    private static readonly CultureInfo DanishCulture = new("da-DK");

    [Fact]
    public void ToInvariantString_NullValue_ShouldReturnNull()
        => Variant.Null.ToInvariantString().Should().BeNull();

    [Fact]
    public void ToInvariantString_FloatOnDanishCulture_ShouldUseDecimalPoint()
    {
        // Arrange
        var variant = new Variant(21.5f);

        // Act
        var result = WithCulture(DanishCulture, () => variant.ToInvariantString());

        // Assert
        result.Should().Be("21.5");
    }

    [Fact]
    public void ToInvariantString_DoubleOnDanishCulture_ShouldUseDecimalPoint()
    {
        // Arrange
        var variant = new Variant(-999.9);

        // Act
        var result = WithCulture(DanishCulture, () => variant.ToInvariantString());

        // Assert
        result.Should().Be("-999.9");
    }

    [Fact]
    public void ToInvariantString_FloatArrayOnDanishCulture_ShouldUseDecimalPointForEachElement()
    {
        // Arrange
        var variant = new Variant(new[] { 1.5f, 2.25f });

        // Act
        var result = WithCulture(DanishCulture, () => variant.ToInvariantString());

        // Assert
        result.Should().Be("{1.5|2.25}");
    }

    [Fact]
    public void ToInvariantString_DateTime_ShouldUseRoundTripFormat()
    {
        // Arrange
        var variant = new Variant(new DateTime(2026, 11, 10, 20, 42, 24, 500, DateTimeKind.Utc));

        // Act
        var result = WithCulture(DanishCulture, () => variant.ToInvariantString());

        // Assert
        result.Should().Be("2026-11-10T20:42:24.5000000Z");
    }

    [Fact]
    public void ToInvariantString_DateTimeArray_ShouldUseRoundTripFormatForEachElement()
    {
        // Arrange
        var variant = new Variant(new[]
        {
            new DateTime(2026, 11, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 11, 11, 0, 0, 0, DateTimeKind.Utc),
        });

        // Act
        var result = variant.ToInvariantString();

        // Assert
        result.Should().Be("{2026-11-10T00:00:00.0000000Z|2026-11-11T00:00:00.0000000Z}");
    }

    [Theory]
    [InlineData(true, "True")]
    [InlineData(false, "False")]
    public void ToInvariantString_Boolean_ShouldReturnBooleanText(
        bool value,
        string expected)
        => new Variant(value).ToInvariantString().Should().Be(expected);

    [Fact]
    public void ToInvariantString_String_ShouldReturnTheString()
        => new Variant("Supply temperature").ToInvariantString().Should().Be("Supply temperature");

    [Fact]
    public void ToInvariantString_Int16_ShouldReturnTheNumber()
        => new Variant((short)-42).ToInvariantString().Should().Be("-42");

    private static string? WithCulture(
        CultureInfo culture,
        Func<string?> action)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}