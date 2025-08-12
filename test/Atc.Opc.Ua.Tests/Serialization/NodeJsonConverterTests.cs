namespace Atc.Opc.Ua.Tests.Serialization;

public sealed class NodeJsonConverterTests
{
    private static JsonSerializerOptions CreateOptions()
    {
        var o = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null,
        };

        o.Converters.Add(new NodeJsonConverter());
        return o;
    }

    [Fact]
    public void Serialize_ObjectRoot_ShouldIncludeDiscriminatorAndChildren()
    {
        // Arrange
        var root = new NodeObject
        {
            ParentNodeId = "i=84",
            NodeId = "i=85",
            NodeIdentifier = "85",
            DisplayName = "Objects",
        };
        var childObj = new NodeObject { ParentNodeId = root.NodeId, NodeId = "ns=2;s=Child", NodeIdentifier = "Child", DisplayName = "ChildObj" };
        var childVar = new NodeVariable { ParentNodeId = childObj.NodeId, NodeId = "ns=2;s=Child.Var", NodeIdentifier = "Child.Var", DisplayName = "ChildVar", DataTypeDotnet = "string", SampleValue = "abc", DataTypeOpcUa = new OpUaDataType { Name = "String", IsArray = false } };
        childObj.NodeVariables.Add(childVar);
        root.NodeObjects.Add(childObj);

        var scan = new NodeScanResult(Succeeded: true, root, ErrorMessage: null);
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(scan, options);

        // Assert (basic structural checks)
        json.Should().Contain("\"$type\":\"object\"");
        json.Should().Contain("ChildVar");
        json.Should().Contain("\"Name\":\"String\"");
    }

    [Fact]
    public void RoundTrip_ObjectRoot_ShouldPreserveHierarchy()
    {
        // Arrange
        var root = new NodeObject { NodeId = "i=1", NodeIdentifier = "1", DisplayName = "Root" };
        var objA = new NodeObject { ParentNodeId = root.NodeId, NodeId = "i=2", NodeIdentifier = "2", DisplayName = "ObjA" };
        var varA1 = new NodeVariable { ParentNodeId = objA.NodeId, NodeId = "i=3", NodeIdentifier = "3", DisplayName = "VarA1", DataTypeDotnet = "int", SampleValue = "42", DataTypeOpcUa = new OpUaDataType { Name = "Int32", IsArray = false } };
        objA.NodeVariables.Add(varA1);
        root.NodeObjects.Add(objA);
        var scan = new NodeScanResult(Succeeded: true, root, ErrorMessage: null);
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(scan, options);
        var clone = JsonSerializer.Deserialize<NodeScanResult>(json, options)!;

        // Assert
        clone.Succeeded.Should().BeTrue();
        clone.Root.Should().BeOfType<NodeObject>();
        var clonedRoot = (NodeObject)clone.Root!;
        clonedRoot.NodeObjects.Should().HaveCount(1);
        var clonedObjA = clonedRoot.NodeObjects[0];
        clonedObjA.NodeVariables.Should().HaveCount(1);
        clonedObjA.NodeVariables[0].DisplayName.Should().Be("VarA1");
        clonedObjA.NodeVariables[0].DataTypeOpcUa!.Name.Should().Be("Int32");
    }

    [Fact]
    public void RoundTrip_VariableRoot_WithNestedVariables_ShouldPreserveChain()
    {
        // Arrange
        var rootVar = new NodeVariable { NodeId = "v=1", NodeIdentifier = "1", DisplayName = "RootVar", DataTypeDotnet = "Variant" };
        var child1 = new NodeVariable { ParentNodeId = rootVar.NodeId, NodeId = "v=1.1", NodeIdentifier = "1.1", DisplayName = "Child1", DataTypeDotnet = "double", SampleValue = "1.23", DataTypeOpcUa = new OpUaDataType { Name = "Double", IsArray = false } };
        var child2 = new NodeVariable { ParentNodeId = child1.NodeId, NodeId = "v=1.1.1", NodeIdentifier = "1.1.1", DisplayName = "Child2", DataTypeDotnet = "string", SampleValue = "hello" };
        child1.NodeVariables.Add(child2);
        rootVar.NodeVariables.Add(child1);
        var scan = new NodeScanResult(Succeeded: true, rootVar, ErrorMessage: null);
        var options = CreateOptions();

        // Act
        var json = JsonSerializer.Serialize(scan, options);
        var clone = JsonSerializer.Deserialize<NodeScanResult>(json, options)!;

        // Assert
        clone.Root.Should().BeOfType<NodeVariable>();
        var clonedRoot = (NodeVariable)clone.Root!;
        clonedRoot.NodeVariables.Should().HaveCount(1);
        var clonedChild1 = clonedRoot.NodeVariables[0];
        clonedChild1.NodeVariables.Should().HaveCount(1);
        clonedChild1.NodeVariables[0].DisplayName.Should().Be("Child2");
    }

    [Fact]
    public void Deserialize_ObjectRoot_MissingCollections_ShouldYieldEmptyLists()
    {
        // Arrange
        const string json = "{\"Succeeded\":true,\"Root\":{\"$type\":\"object\",\"ParentNodeId\":\"i=0\",\"NodeId\":\"i=1\",\"NodeIdentifier\":\"1\",\"NodeClass\":\"Object\",\"DisplayName\":\"Root\"},\"ErrorMessage\":null}";
        var options = CreateOptions();

        // Act
        var clone = JsonSerializer.Deserialize<NodeScanResult>(json, options)!;

        // Assert
        clone.Root.Should().BeOfType<NodeObject>();
        ((NodeObject)clone.Root!).NodeObjects.Should().BeEmpty();
        ((NodeObject)clone.Root!).NodeVariables.Should().BeEmpty();
    }

    [Fact]
    public void Deserialize_VariableRoot_DatatypeNull_ShouldLeaveOpcUaNull()
    {
        // Arrange
        const string json = "{\"Succeeded\":true,\"Root\":{\"$type\":\"variable\",\"ParentNodeId\":\"i=0\",\"NodeId\":\"i=9\",\"NodeIdentifier\":\"9\",\"NodeClass\":\"Variable\",\"DisplayName\":\"Var\",\"DataTypeDotnet\":\"object\",\"SampleValue\":\"\",\"NodeVariables\":[]},\"ErrorMessage\":null}";
        var options = CreateOptions();

        // Act
        var clone = JsonSerializer.Deserialize<NodeScanResult>(json, options)!;

        // Assert
        clone.Root.Should().BeOfType<NodeVariable>();
        ((NodeVariable)clone.Root!).DataTypeOpcUa.Should().BeNull();
    }

    [Fact]
    public void Deserialize_InvalidDiscriminator_ShouldThrow()
    {
        // Arrange
        const string json = "{\"Succeeded\":true,\"Root\":{\"$type\":\"bogus\",\"ParentNodeId\":\"i=0\",\"NodeId\":\"i=1\",\"NodeIdentifier\":\"1\",\"NodeClass\":\"Object\",\"DisplayName\":\"Root\"},\"ErrorMessage\":null}";
        var options = CreateOptions();

        // Act
        var act = () => JsonSerializer.Deserialize<NodeScanResult>(json, options);

        // Assert
        act.Should().Throw<JsonException>();
    }
}
