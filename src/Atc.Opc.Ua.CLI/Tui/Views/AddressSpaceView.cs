namespace Atc.Opc.Ua.CLI.Tui.Views;

/// <summary>
/// TreeView-based panel for browsing the OPC UA address space.
/// Supports lazy-loading of child nodes on expand.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class AddressSpaceView : FrameView
{
    private readonly TreeView<BrowsedNode> treeView;
    private readonly OpcUaTuiService tuiService;
    private readonly IApplication app;
    private readonly Label emptyLabel;

    public AddressSpaceView(IApplication app, OpcUaTuiService tuiService)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(tuiService);

        this.app = app;
        this.tuiService = tuiService;

        Title = "Address Space";
        CanFocus = true;

        treeView = new TreeView<BrowsedNode>
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            TreeBuilder = new DelegateTreeBuilder<BrowsedNode>(
                GetChildren,
                CanExpand),
        };

        treeView.SelectionChanged += OnSelectionChanged;
        treeView.ObjectActivated += OnObjectActivated;

        emptyLabel = new Label
        {
            Text = "Not connected. Press 'c' to connect.",
            X = Pos.Center(),
            Y = Pos.Center(),
        };

        Add(emptyLabel, treeView);
        treeView.Visible = false;
    }

    /// <summary>
    /// Raised when the user selects a different node in the tree.
    /// </summary>
    [SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Internal TUI event, not public API")]
    [SuppressMessage("Design", "MA0046:The delegate must have 2 parameters", Justification = "Internal TUI event")]
    public event Action<BrowsedNode>? NodeSelected;

    /// <summary>
    /// Raised when the user activates (Enter/double-click) a variable node.
    /// </summary>
    [SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "Internal TUI event, not public API")]
    [SuppressMessage("Design", "MA0046:The delegate must have 2 parameters", Justification = "Internal TUI event")]
    public event Action<BrowsedNode>? SubscribeRequested;

    public BrowsedNode? SelectedNode => treeView.SelectedObject;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not crash on browse failure")]
    public async Task LoadRootAsync(CancellationToken cancellationToken)
    {
        try
        {
            var (succeeded, children, _) = await tuiService
                .BrowseChildrenAsync("i=84", cancellationToken);

            if (!succeeded || children is null)
            {
                return;
            }

            var rootNode = new BrowsedNode
            {
                NodeId = "i=84",
                DisplayName = "Root",
                NodeClass = NodeClassType.Object,
                HasChildren = true,
                ChildrenLoaded = true,
            };

            foreach (var child in children)
            {
                rootNode.Children.Add(BrowsedNode.FromBrowseResult(child, rootNode));
            }

            app.Invoke(() =>
            {
                treeView.ClearObjects();
                treeView.AddObject(rootNode);
                treeView.Expand(rootNode);
                treeView.Visible = true;
                emptyLabel.Visible = false;
            });
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception)
        {
            // Browse failed; leave empty state.
        }
    }

    public void Clear()
    {
        treeView.ClearObjects();
        treeView.Visible = false;
        emptyLabel.Visible = true;
        emptyLabel.Text = "Not connected. Press 'c' to connect.";
    }

    private IEnumerable<BrowsedNode> GetChildren(BrowsedNode node)
    {
        if (node.ChildrenLoaded)
        {
            return node.Children;
        }

        _ = LoadChildrenAsync(node);
        return [];
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not crash on browse failure")]
    private async Task LoadChildrenAsync(BrowsedNode node)
    {
        if (node.ChildrenLoaded)
        {
            return;
        }

        var (succeeded, children, _) = await tuiService
            .BrowseChildrenAsync(node.NodeId, CancellationToken.None);

        if (!succeeded || children is null)
        {
            node.ChildrenLoaded = true;
            node.HasChildren = false;
            app.Invoke(() => treeView.RefreshObject(node, false));
            return;
        }

        node.ChildrenLoaded = true;
        foreach (var child in children)
        {
            node.Children.Add(BrowsedNode.FromBrowseResult(child, node));
        }

        app.Invoke(() =>
        {
            treeView.RefreshObject(node, false);
            if (node.Children.Count > 0)
            {
                treeView.Expand(node);
            }
        });
    }

    private static bool CanExpand(BrowsedNode node) => node.HasChildren;

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs<BrowsedNode> e)
    {
        if (e.NewValue is not null)
        {
            NodeSelected?.Invoke(e.NewValue);
        }
    }

    private void OnObjectActivated(object? sender, ObjectActivatedEventArgs<BrowsedNode> e)
    {
        if (e.ActivatedObject?.NodeClass == NodeClassType.Variable)
        {
            SubscribeRequested?.Invoke(e.ActivatedObject);
        }
    }
}
