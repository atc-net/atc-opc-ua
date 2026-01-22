namespace Atc.Opc.Ua.Tests.Extensions;

public sealed class VariableNodeExtensionsTests
{
    [Fact]
    public void CreateBasicOpcUaDataType_NullVariableNode_ShouldThrowArgumentNullException()
    {
        // Arrange
        VariableNode? variableNode = null;

        // Act
        var act = () => variableNode!.CreateBasicOpcUaDataType();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("variableNode");
    }

    [Fact]
    public void CreateBasicOpcUaDataType_BuiltInInt32Type_ShouldReturnPrimitiveType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.Int32, isArray: false);

        // Act
        var result = variableNode.CreateBasicOpcUaDataType();

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("i=6");
        result.Name.Should().Be("Int32");
        result.DisplayName.Should().Be("Int32");
        result.IdentifierType.Should().Be("Numeric");
        result.Kind.Should().Be(OpcUaTypeKind.Primitive);
        result.IsArray.Should().BeFalse();
    }

    [Fact]
    public void CreateBasicOpcUaDataType_BuiltInStringType_ShouldReturnPrimitiveType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.String, isArray: false);

        // Act
        var result = variableNode.CreateBasicOpcUaDataType();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("String");
        result.Kind.Should().Be(OpcUaTypeKind.Primitive);
        result.IsArray.Should().BeFalse();
    }

    [Fact]
    public void CreateBasicOpcUaDataType_BuiltInArrayType_ShouldReturnArrayType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.Int32, isArray: true);

        // Act
        var result = variableNode.CreateBasicOpcUaDataType();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Int32");
        result.Kind.Should().Be(OpcUaTypeKind.Primitive);
        result.IsArray.Should().BeTrue();
    }

    [Fact]
    public void CreateBasicOpcUaDataType_NonBuiltInNumericType_ShouldReturnUnknownKind()
    {
        // Arrange - using a custom numeric NodeId that is not a built-in type
        var customDataTypeId = new NodeId(3063, 3); // ns=3;i=3063
        var variableNode = CreateVariableNode(customDataTypeId, isArray: false);

        // Act
        var result = variableNode.CreateBasicOpcUaDataType();

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("ns=3;i=3063");
        result.Name.Should().Be("3063");
        result.Kind.Should().Be(OpcUaTypeKind.Unknown);
        result.IdentifierType.Should().Be("Numeric");
    }

    [Fact]
    public void CreateBasicOpcUaDataType_NonBuiltInStringType_ShouldReturnUnknownKind()
    {
        // Arrange - using a custom string NodeId
        var customDataTypeId = new NodeId("CustomType", 2);
        var variableNode = CreateVariableNode(customDataTypeId, isArray: false);

        // Act
        var result = variableNode.CreateBasicOpcUaDataType();

        // Assert
        result.Should().NotBeNull();
        result.NodeId.Should().Be("ns=2;s=CustomType");
        result.Name.Should().Be("CustomType");
        result.Kind.Should().Be(OpcUaTypeKind.Unknown);
        result.IdentifierType.Should().Be("String");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_NullVariableNode_ShouldThrowArgumentNullException()
    {
        // Arrange
        VariableNode? variableNode = null;

        // Act
        var act = () => variableNode!.CreateBasicDotNetTypeDescriptor();

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("variableNode");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_BuiltInInt32Scalar_ShouldReturnPrimitiveType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.Int32, isArray: false);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Primitive);
        result.Name.Should().Be("Int32");
        result.ClrTypeName.Should().Be("int");
        result.ArrayElementType.Should().BeNull();
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_BuiltInStringScalar_ShouldReturnPrimitiveType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.String, isArray: false);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Primitive);
        result.Name.Should().Be("String");
        result.ClrTypeName.Should().Be("string");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_BuiltInDoubleScalar_ShouldReturnPrimitiveType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.Double, isArray: false);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Primitive);
        result.Name.Should().Be("Double");
        result.ClrTypeName.Should().Be("double");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_BuiltInBooleanScalar_ShouldReturnPrimitiveType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.Boolean, isArray: false);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Primitive);
        result.Name.Should().Be("Boolean");
        result.ClrTypeName.Should().Be("bool");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_BuiltInInt32Array_ShouldReturnArrayType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.Int32, isArray: true);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Array);
        result.Name.Should().Be("int[]");
        result.ClrTypeName.Should().Be("int[]");
        result.ArrayElementType.Should().NotBeNull();
        result.ArrayElementType!.Kind.Should().Be(DotNetTypeKind.Primitive);
        result.ArrayElementType.Name.Should().Be("Int32");
        result.ArrayElementType.ClrTypeName.Should().Be("int");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_BuiltInStringArray_ShouldReturnArrayType()
    {
        // Arrange
        var variableNode = CreateVariableNode(DataTypeIds.String, isArray: true);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Array);
        result.Name.Should().Be("string[]");
        result.ClrTypeName.Should().Be("string[]");
        result.ArrayElementType.Should().NotBeNull();
        result.ArrayElementType!.Kind.Should().Be(DotNetTypeKind.Primitive);
        result.ArrayElementType.Name.Should().Be("String");
        result.ArrayElementType.ClrTypeName.Should().Be("string");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_NonBuiltInScalar_ShouldReturnUnknownType()
    {
        // Arrange
        var customDataTypeId = new NodeId(3063, 3);
        var variableNode = CreateVariableNode(customDataTypeId, isArray: false);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Unknown);
        result.Name.Should().Be("3063");
        result.ClrTypeName.Should().Be("object");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_NonBuiltInArray_ShouldReturnArrayWithUnknownElementType()
    {
        // Arrange
        var customDataTypeId = new NodeId(3063, 3);
        var variableNode = CreateVariableNode(customDataTypeId, isArray: true);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        // Note: OpcUaToDotNetDataTypeMapper returns "Variant" for non-built-in types with array dimensions
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Array);
        result.ClrTypeName.Should().Be("Variant");
        result.ArrayElementType.Should().NotBeNull();
        result.ArrayElementType!.Kind.Should().Be(DotNetTypeKind.Unknown);
        result.ArrayElementType.ClrTypeName.Should().Be("object");
    }

    [Fact]
    public void CreateBasicDotNetTypeDescriptor_NonBuiltInStringIdentifier_ShouldExtractName()
    {
        // Arrange
        var customDataTypeId = new NodeId("MyCustomType", 2);
        var variableNode = CreateVariableNode(customDataTypeId, isArray: false);

        // Act
        var result = variableNode.CreateBasicDotNetTypeDescriptor();

        // Assert
        result.Should().NotBeNull();
        result.Kind.Should().Be(DotNetTypeKind.Unknown);
        result.Name.Should().Be("MyCustomType");
        result.ClrTypeName.Should().Be("object");
    }

    private static VariableNode CreateVariableNode(
        NodeId dataTypeId,
        bool isArray)
    {
        var variableNode = new VariableNode
        {
            NodeId = new NodeId("TestVariable", 1),
            BrowseName = new QualifiedName("TestVariable"),
            DisplayName = new LocalizedText("TestVariable"),
            DataType = dataTypeId,
            ValueRank = isArray ? ValueRanks.OneDimension : ValueRanks.Scalar,
        };

        if (isArray)
        {
            variableNode.ArrayDimensions = new UInt32Collection([0]);
        }

        return variableNode;
    }
}