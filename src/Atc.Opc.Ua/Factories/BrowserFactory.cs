namespace Atc.Opc.Ua.Factories;

/// <summary>
/// Factory class for creating instances of OPC UA Browsers.
/// </summary>
public static class BrowserFactory
{
    /// <summary>
    /// Creates a Browser instance that browses in the forward direction.
    /// </summary>
    /// <param name="session">The OPC UA session for browsing.</param>
    /// <returns>A new Browser instance configured to browse forward.</returns>
    public static Browser GetForwardBrowser(ISession session)
        => new(session)
        {
            BrowseDirection = BrowseDirection.Forward,
            NodeClassMask = (int)NodeClass.Object | (int)NodeClass.Variable,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
        };

    /// <summary>
    /// Creates a Browser instance that browses in the backward direction.
    /// </summary>
    /// <param name="session">The OPC UA session for browsing.</param>
    /// <returns>A new Browser instance configured to browse backward.</returns>
    public static Browser GetBackwardsBrowser(ISession session)
        => new(session)
        {
            BrowseDirection = BrowseDirection.Inverse,
            NodeClassMask = (int)NodeClass.Object,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
        };
}