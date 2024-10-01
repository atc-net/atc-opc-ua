namespace Atc.Opc.Ua.Tests.Factories;

public sealed class BrowserFactoryTests
{
    [Theory, AutoNSubstituteData]
    public void GetForwardBrowser_ShouldReturnConfiguredBrowser(ISession session)
    {
        // Act
        var browser = BrowserFactory.GetForwardBrowser(session);

        // Assert
        Assert.NotNull(browser);
        Assert.Equal(BrowseDirection.Forward, browser.BrowseDirection);
        Assert.Equal((int)NodeClass.Object | (int)NodeClass.Variable, browser.NodeClassMask);
        Assert.Equal(ReferenceTypeIds.HierarchicalReferences, browser.ReferenceTypeId);
        Assert.True(browser.IncludeSubtypes);
    }

    [Theory, AutoNSubstituteData]
    public void GetBackwardsBrowser_ShouldReturnConfiguredBrowser(ISession session)
    {
        // Act
        var browser = BrowserFactory.GetBackwardsBrowser(session);

        // Assert
        Assert.NotNull(browser);
        Assert.Equal(BrowseDirection.Inverse, browser.BrowseDirection);
        Assert.Equal((int)NodeClass.Object, browser.NodeClassMask);
        Assert.Equal(ReferenceTypeIds.HierarchicalReferences, browser.ReferenceTypeId);
    }
}