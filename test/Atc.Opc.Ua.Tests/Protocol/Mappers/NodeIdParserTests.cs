namespace Atc.Opc.Ua.Tests.Protocol.Mappers;

public sealed class NodeIdParserTests
{
    [Theory]
    [InlineData("i=85", "i=85")]
    [InlineData("ns=3;s=StepUp", "ns=3;s=StepUp")]
    [InlineData("ns=4;s=\"DB_Data\".\"Motor 1\"", "ns=4;s=\"DB_Data\".\"Motor 1\"")]
    [InlineData("ns=3;g=51b74e55-f2e3-4a4d-b79c-bf57c76ea67c", "ns=3;g=51b74e55-f2e3-4a4d-b79c-bf57c76ea67c")]
    [InlineData("ns=3;b=YWJj", "ns=3;b=YWJj")]
    public void Parse_ValidNodeId_ShouldReturnTheNodeId(
        string text,
        string expected)
        => NodeIdParser.Parse(text, "nodeId").ToString().Should().Be(expected);

    [Theory]
    [InlineData("ns=2;i=abc")]
    [InlineData("ns=x;i=1")]
    [InlineData("ns=2;x=1")]
    [InlineData("i=85 ")]
    [InlineData(" i=85")]
    [InlineData("ns=3;g=not-a-guid")]
    [InlineData("foo")]
    public void Parse_InvalidNodeId_ShouldThrowArgumentExceptionNamingTheParameter(
        string text)
        => FluentActions
            .Invoking(() => NodeIdParser.Parse(text, "parentNodeId"))
            .Should().Throw<ArgumentException>()
            .WithMessage($"'{text}' isn't a valid node id.*")
            .Which.ParamName.Should().Be("parentNodeId");

    [Fact]
    public void TryParse_ValidNodeId_ShouldReturnTrueAndTheNodeId()
    {
        // Act
        var parsed = NodeIdParser.TryParse("ns=3;s=StepUp", out var nodeId);

        // Assert
        parsed.Should().BeTrue();
        nodeId.Should().Be(new NodeId("StepUp", 3));
    }

    [Fact]
    public void TryParse_InvalidNodeId_ShouldReturnFalseAndTheNullNodeIdWithoutThrowing()
    {
        // Act
        var parsed = NodeIdParser.TryParse("ns=2;i=abc", out var nodeId);

        // Assert
        parsed.Should().BeFalse();
        nodeId.Should().Be(NodeId.Null);
    }
}