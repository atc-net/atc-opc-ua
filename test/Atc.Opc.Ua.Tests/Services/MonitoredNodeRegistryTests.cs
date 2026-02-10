namespace Atc.Opc.Ua.Tests.Services;

public sealed class MonitoredNodeRegistryTests
{
    [Fact]
    public void NextHandle_CalledConcurrently_ShouldReturnUniqueHandles()
    {
        // Arrange
        var registry = new MonitoredNodeRegistry();
        var handles = new ConcurrentBag<uint>();

        // Act
        Parallel.For(0, 1000, _ => handles.Add(registry.NextHandle()));

        // Assert
        handles.Should().OnlyHaveUniqueItems();
        handles.Should().HaveCount(1000);
        handles.Should().NotContain(0u);
    }

    [Fact]
    public void Add_ThenTryGet_ShouldReturnTheNode()
    {
        // Arrange
        var registry = new MonitoredNodeRegistry();
        var node = CreateNode(registry.NextHandle(), "Float", 3);

        // Act
        registry.Add(node);

        // Assert
        registry.TryGet(node.Handle, out var found).Should().BeTrue();
        found.Should().Be(node);
    }

    [Fact]
    public void TryGet_UnknownHandle_ShouldReturnFalse()
        => new MonitoredNodeRegistry().TryGet(42, out _).Should().BeFalse();

    [Fact]
    public void Remove_KnownHandle_ShouldRemoveTheNode()
    {
        // Arrange
        var registry = new MonitoredNodeRegistry();
        var node = CreateNode(registry.NextHandle());
        registry.Add(node);

        // Act
        var removed = registry.Remove(node.Handle);

        // Assert
        removed.Should().BeTrue();
        registry.Count.Should().Be(0);
    }

    [Fact]
    public void AddAndRemove_CalledConcurrently_ShouldKeepTheRegistryConsistent()
    {
        // Arrange
        var registry = new MonitoredNodeRegistry();

        // Act
        Parallel.For(0, 1000, i =>
        {
            var node = CreateNode(registry.NextHandle());
            registry.Add(node);
            if (i % 2 == 0)
            {
                registry.Remove(node.Handle);
            }
        });

        // Assert
        registry.Count.Should().Be(500);
    }

    [Fact]
    public void Rebind_RecreatedItems_ShouldPointTheNodesAtTheRecreatedItemsAndKeepTheMetadata()
    {
        // Arrange
        var registry = new MonitoredNodeRegistry();
        var first = CreateNode(registry.NextHandle(), "Float", 3);
        var second = CreateNode(registry.NextHandle(), "Boolean", 1);
        registry.Add(first);
        registry.Add(second);

        var recreatedFirst = new MonitoredItem { Handle = first.Handle };
        var recreatedSecond = new MonitoredItem { Handle = second.Handle };

        // Act
        var rebound = registry.Rebind([recreatedFirst, recreatedSecond]);

        // Assert
        rebound.Should().Be(2);
        registry.TryGet(first.Handle, out var firstNode).Should().BeTrue();
        firstNode.Item.Should().BeSameAs(recreatedFirst);
        firstNode.DataTypeName.Should().Be("Float");
        firstNode.AccessLevel.Should().Be(3);
        registry.TryGet(second.Handle, out var secondNode).Should().BeTrue();
        secondNode.Item.Should().BeSameAs(recreatedSecond);
    }

    [Fact]
    public void Rebind_NodeWithoutRecreatedItem_ShouldRemoveTheNode()
    {
        // Arrange
        var registry = new MonitoredNodeRegistry();
        var kept = CreateNode(registry.NextHandle());
        var lost = CreateNode(registry.NextHandle());
        registry.Add(kept);
        registry.Add(lost);

        // Act
        var rebound = registry.Rebind([new MonitoredItem { Handle = kept.Handle }]);

        // Assert
        rebound.Should().Be(1);
        registry.TryGet(lost.Handle, out _).Should().BeFalse();
        registry.Count.Should().Be(1);
    }

    [Fact]
    public void Rebind_ItemWithoutClientHandle_ShouldBeIgnored()
    {
        // Arrange
        var registry = new MonitoredNodeRegistry();
        var node = CreateNode(registry.NextHandle());
        registry.Add(node);

        // Act
        var rebound = registry.Rebind([new MonitoredItem { Handle = "not ours" }, new MonitoredItem { Handle = node.Handle }]);

        // Assert
        rebound.Should().Be(1);
    }

    private static MonitoredNode CreateNode(
        uint handle,
        string? dataTypeName = null,
        byte? accessLevel = null)
        => new(handle, new MonitoredItem { Handle = handle }, dataTypeName, accessLevel);
}