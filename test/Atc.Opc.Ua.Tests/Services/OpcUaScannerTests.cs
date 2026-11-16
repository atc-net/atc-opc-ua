namespace Atc.Opc.Ua.Tests.Services;

public sealed class OpcUaScannerTests
{
    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ClientNotConnected_ShouldThrowInvalidOperationException(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(false);
        var scanner = new OpcUaScanner(logger);

        // Act
        var act = () => scanner.ScanAsync(client, new OpcUaScannerOptions(), cancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        await client.DidNotReceiveWithAnyArgs().ReadNodeObjectAsync(
            Arg.Any<string>(),
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_NegativeObjectDepth_ShouldThrowArgumentOutOfRangeException(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        var scanner = new OpcUaScanner(logger);

        // Act
        var act = () => scanner.ScanAsync(client, new OpcUaScannerOptions { ObjectDepth = -1 }, cancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).WithMessage("*ObjectDepth*");
        await client.DidNotReceiveWithAnyArgs().ReadNodeObjectAsync(
            Arg.Any<string>(),
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_NegativeVariableDepth_ShouldThrowArgumentOutOfRangeException(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        var scanner = new OpcUaScanner(logger);

        // Act
        var act = () => scanner.ScanAsync(client, new OpcUaScannerOptions { VariableDepth = -2 }, cancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ArgumentOutOfRangeException>()).WithMessage("*VariableDepth*");
        await client.DidNotReceiveWithAnyArgs().ReadNodeObjectAsync(
            Arg.Any<string>(),
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_EmptyStartingNode_ShouldScanTheObjectsFolder(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        var root = new NodeObject { NodeId = ObjectIds.ObjectsFolder.ToString(), DisplayName = "Objects" };
        client.ReadNodeObjectAsync(
            root.NodeId,
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(root);
        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = string.Empty }, cancellationToken);

        // Assert
        result.Root.Should().BeSameAs(root);
        result.SkippedNodeIds.Should().BeEmpty();
        await client.Received(1).ReadNodeObjectAsync(
            root.NodeId,
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_StartingNodeWithSpaces_ShouldTrimIt(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        var root = new NodeObject { NodeId = "ns=2;s=Demo.Dynamic.Scalar", DisplayName = "Root" };
        client.ReadNodeObjectAsync(
            root.NodeId,
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(root);
        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = "   ns=2;s=Demo.Dynamic.Scalar  " }, cancellationToken);

        // Assert
        result.Root.Should().BeSameAs(root);
        await client.Received(1).ReadNodeObjectAsync(
            root.NodeId,
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_Options_ShouldBePassedToTheClient(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        var root = new NodeObject { NodeId = "root", DisplayName = "Root" };
        string[] includeObjects = ["objA", "objB"];
        string[] excludeObjects = ["objX"];
        string[] includeVariables = ["var1"];
        string[] excludeVariables = ["var9"];

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
                skippedNodeIds: Arg.Any<ICollection<string>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(root);

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

        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, options, cancellationToken);

        // Assert
        result.Root.Should().BeSameAs(root);
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_ClientReadFails_ShouldThrowTheClientsException(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        client.ReadNodeObjectAsync(
            Arg.Any<string>(),
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new ServiceResultException(StatusCodes.BadUserAccessDenied));
        var scanner = new OpcUaScanner(logger);

        // Act
        var act = () => scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = "root" }, cancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ServiceResultException>()).Which.StatusCode.Should().Be(StatusCodes.BadUserAccessDenied);
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_StartingNodeDoesNotExist_ShouldThrowBadNodeIdUnknown(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        client.ReadNodeObjectAsync(
            Arg.Any<string>(),
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns((NodeObject?)null);
        var scanner = new OpcUaScanner(logger);

        // Act
        var act = () => scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = "ns=2;s=DoesNotExist" }, cancellationToken);

        // Assert
        (await act.Should().ThrowAsync<ServiceResultException>()).Which.StatusCode.Should().Be(StatusCodes.BadNodeIdUnknown);
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_StartingNodeIsAVariable_ShouldScanItAsAVariable(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        var variable = new NodeVariable { NodeId = "ns=2;s=Demo.Dynamic.Scalar.Float", DisplayName = "Float" };
        var clientParameterName = "nodeId"; // the client's parameter, as its ArgumentException names it
        client.ReadNodeObjectAsync(
            Arg.Any<string>(),
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Node has the NodeClass Variable, not Object.", clientParameterName));
        client.ReadNodeVariableAsync(
                variable.NodeId,
                includeSampleValue: Arg.Any<bool>(),
                nodeVariableReadDepth: Arg.Any<int>(),
                skippedNodeIds: Arg.Any<ICollection<string>>(),
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(variable);

        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = variable.NodeId }, cancellationToken);

        // Assert
        result.Root.Should().BeSameAs(variable);
    }

    [Theory, AutoNSubstituteData]
    public async Task ScanAsync_NodesSkippedByTheClient_ShouldReturnTheirIds(
        IOpcUaClient client,
        ILogger<OpcUaScanner> logger,
        CancellationToken cancellationToken)
    {
        // Arrange
        client.IsConnected().Returns(true);
        var root = new NodeObject { NodeId = "root", DisplayName = "Root" };
        client.ReadNodeObjectAsync(
            Arg.Any<string>(),
            includeObjects: Arg.Any<bool>(),
            includeVariables: Arg.Any<bool>(),
            includeSampleValues: Arg.Any<bool>(),
            nodeObjectReadDepth: Arg.Any<int>(),
            nodeVariableReadDepth: Arg.Any<int>(),
            includeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeObjectNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            includeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            excludeVariableNodeIds: Arg.Any<IReadOnlyCollection<string>>(),
            skippedNodeIds: Arg.Any<ICollection<string>>(),
            cancellationToken: Arg.Any<CancellationToken>())
            .Returns(callInfo =>
        {
            callInfo.ArgAt<ICollection<string>>(10).Add("ns=2;s=Locked");
            return root;
        });

        var scanner = new OpcUaScanner(logger);

        // Act
        var result = await scanner.ScanAsync(client, new OpcUaScannerOptions { StartingNodeId = root.NodeId }, cancellationToken);

        // Assert
        result.SkippedNodeIds.Should().Equal("ns=2;s=Locked");
    }
}