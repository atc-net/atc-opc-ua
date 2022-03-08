namespace Atc.Opc.Ua.Factories;

public static class BrowserFactory
{
    public static Browser GetForwardBrowser(
        Session session)
        => new(session)
        {
            BrowseDirection = BrowseDirection.Forward,
            NodeClassMask = (int)NodeClass.Object | (int)NodeClass.Variable,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
            IncludeSubtypes = true,
        };

    public static Browser GetBackwardsBrowser(
        Session session)
        => new(session)
        {
            BrowseDirection = BrowseDirection.Inverse,
            NodeClassMask = (int)NodeClass.Object,
            ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences,
        };
}