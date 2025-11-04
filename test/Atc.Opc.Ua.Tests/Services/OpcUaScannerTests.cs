namespace Atc.Opc.Ua.Tests.Services;

public sealed class OpcUaScannerTests
{
    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ShouldReturnError_WhenClientNotConnected(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client
            .IsConnected()
            .Returns(false);

        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions(), cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Root.Should().BeNull();
        result.ErrorMessage.Should().NotBeNull();

        await client
            .DidNotReceiveWithAnyArgs()
            .ReadNodeObjectAsync(
                nodeId: null!,
                includeObjects: false,
                includeVariables: false,
                includeSampleValues: false,
                cancellationToken: cancellationToken,
                nodeObjectReadDepth: 0);
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ShouldReturnError_WhenObjectDepthNegative(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client
            .IsConnected()
            .Returns(true);

        var scanner = new OpcUaScanner(logger);
        var options = new OpcUaScannerOptions { ObjectDepth = -1 };

        // Act
        var result = await scanner.ScanAsync(client, options, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("ObjectDepth");

        await client
            .DidNotReceiveWithAnyArgs()
            .ReadNodeObjectAsync(
                nodeId: null!,
                includeObjects: false,
                includeVariables: false,
                includeSampleValues: false,
                cancellationToken: cancellationToken,
                nodeObjectReadDepth: 0);
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ShouldReturnError_WhenVariableDepthNegative(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client
            .IsConnected()
            .Returns(true);

        var scanner = new OpcUaScanner(logger);
        var options = new OpcUaScannerOptions { VariableDepth = -2 };

        // Act
        var result = await scanner.ScanAsync(client, options, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("VariableDepth");

        await client
            .DidNotReceiveWithAnyArgs()
            .ReadNodeObjectAsync(
                nodeId: null!,
                includeObjects: false,
                includeVariables: false,
                includeSampleValues: false,
                cancellationToken: cancellationToken,
                nodeObjectReadDepth: 0);
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ShouldUseDefaultStartingNode_WhenStartingNodeEmpty(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client
            .IsConnected()
            .Returns(true);

        var root = new NodeObject { NodeId = ObjectIds.ObjectsFolder.ToString(), DisplayName = "Objects" };

        client.ReadNodeObjectAsync(
                nodeId: ObjectIds.ObjectsFolder.ToString(),
                includeObjects: true,
                includeVariables: true,
                includeSampleValues: Arg.Any<bool>(),
                nodeObjectReadDepth: Arg.Any<int>(),
                nodeVariableReadDepth: Arg.Any<int>(),
                includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((true, root, (string?)null)));

        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = string.Empty }, cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Root.Should().NotBeNull();

        await client.Received(1).ReadNodeObjectAsync(
            nodeId: ObjectIds.ObjectsFolder.ToString(),
            includeObjects: true,
            includeVariables: true,
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ShouldTrimStartingNodeId(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client
            .IsConnected()
            .Returns(true);

        var root = new NodeObject { NodeId = "ns=2;s=Demo.Dynamic.Scalar", DisplayName = "Root" };

        client.ReadNodeObjectAsync(
                nodeId: root.NodeId,
                includeObjects: true,
                includeVariables: true,
                includeSampleValues: Arg.Any<bool>(),
                nodeObjectReadDepth: Arg.Any<int>(),
                nodeVariableReadDepth: Arg.Any<int>(),
                includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((true, root, (string?)null)));

        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = "   ns=2;s=Demo.Dynamic.Scalar  " }, cancellationToken);

            // Assert
        result.Succeeded.Should().BeTrue();
        result.Root.Should().NotBeNull();

        await client.Received(1).ReadNodeObjectAsync(
            nodeId: root.NodeId,
            includeObjects: true,
            includeVariables: true,
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ShouldPassOptionsToClient(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client
            .IsConnected()
            .Returns(true);

        var root = new NodeObject { NodeId = "root", DisplayName = "Root" };
        var includeObjects = new[] { "objA", "objB" };
        var excludeObjects = new[] { "objX" };
        var includeVariables = new[] { "var1" };
        var excludeVariables = new[] { "var9" };

        client.ReadNodeObjectAsync(
                root.NodeId,
                includeObjects: true,
                includeVariables: true,
                includeSampleValues: true,
                nodeObjectReadDepth: 2,
                nodeVariableReadDepth: 3,
                includeObjectNodeIds: Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(includeObjects)),
                excludeObjectNodeIds: Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(excludeObjects)),
                includeVariableNodeIds: Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(includeVariables)),
                excludeVariableNodeIds: Arg.Is<IReadOnlyCollection<string>>(c => c.SequenceEqual(excludeVariables)),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((true, root, (string?)null)));

        var scanner = new OpcUaScanner(logger);
        var options = new OpcUaScannerOptions
        {
            StartingNodeId = root.NodeId,
            ObjectDepth = 2,
            VariableDepth = 3,
            IncludeSampleValues = true,
        };
        foreach (var id in includeObjects)
        {
            options.IncludeObjectNodeIds.Add(id);
        }

        foreach (var id in excludeObjects)
        {
            options.ExcludeObjectNodeIds.Add(id);
        }

        foreach (var id in includeVariables)
        {
            options.IncludeVariableNodeIds.Add(id);
        }

        foreach (var id in excludeVariables)
        {
            options.ExcludeVariableNodeIds.Add(id);
        }

        // Act
        var result = await scanner.ScanAsync(client, options, cancellationToken);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Root.Should().NotBeNull();
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ShouldReturnError_WhenClientReadFails(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client
            .IsConnected()
            .Returns(true);

        client.ReadNodeObjectAsync(
                nodeId: Arg.Any<string>(),
                includeObjects: true,
                includeVariables: true,
                includeSampleValues: Arg.Any<bool>(),
                nodeObjectReadDepth: Arg.Any<int>(),
                nodeVariableReadDepth: Arg.Any<int>(),
                includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((false, (NodeObject?)null, "boom")));

        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = "root" }, cancellationToken);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Root.Should().BeNull();
        result.ErrorMessage.Should().Be("boom");
    }
}
