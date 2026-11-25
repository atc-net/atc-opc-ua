namespace Atc.Opc.Ua.Tests.Factories;

public sealed class BrowserFactoryTests
{
    [Fact]
    public void GetForwardBrowser_ShouldReturnConfiguredBrowser()
    {
        // Arrange
        using var session = CreateSession();

        // Act
        var browser = BrowserFactory.GetForwardBrowser(session);

        // Assert
        Assert.NotNull(browser);
        Assert.Equal(BrowseDirection.Forward, browser.BrowseDirection);
        Assert.Equal((uint)NodeClass.Object | (uint)NodeClass.Variable, browser.NodeClassMask);
        Assert.Equal(ReferenceTypeIds.HierarchicalReferences, browser.ReferenceTypeId);
        Assert.True(browser.IncludeSubtypes);
    }

    [Fact]
    public void GetBackwardsBrowser_ShouldReturnConfiguredBrowser()
    {
        // Arrange
        using var session = CreateSession();

        // Act
        var browser = BrowserFactory.GetBackwardsBrowser(session);

        // Assert
        Assert.NotNull(browser);
        Assert.Equal(BrowseDirection.Inverse, browser.BrowseDirection);
        Assert.Equal((uint)NodeClass.Object | (uint)NodeClass.Variable, browser.NodeClassMask);
        Assert.Equal(ReferenceTypeIds.HierarchicalReferences, browser.ReferenceTypeId);
        Assert.True(browser.IncludeSubtypes);
    }

    private static ISession CreateSession()
    {
        var session = Substitute.For<ISession>();
        session.OperationLimits.Returns(new OperationLimits());
        session.ServerCapabilities.Returns(new ServerCapabilities());
        return session;
    }
}