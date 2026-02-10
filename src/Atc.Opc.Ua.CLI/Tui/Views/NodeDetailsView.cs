namespace Atc.Opc.Ua.CLI.Tui.Views;

/// <summary>
/// Panel showing detailed attributes of the currently selected OPC UA node.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class NodeDetailsView : FrameView
{
    private readonly OpcUaTuiService tuiService;
    private readonly IApplication app;
    private readonly Label detailsLabel;

    public NodeDetailsView(IApplication app, OpcUaTuiService tuiService)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(tuiService);

        this.app = app;
        this.tuiService = tuiService;

        Title = "Node Details";

        detailsLabel = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = Dim.Fill(),
            Text = "Select a node to view details.",
        };

        Add(detailsLabel);
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Must not crash on attribute read failure")]
    public async Task ShowNodeAsync(string nodeId, string displayName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(nodeId);

        try
        {
            var (succeeded, attributes, _) = await tuiService
                .ReadNodeAttributesAsync(nodeId, cancellationToken);

            if (!succeeded || attributes is null)
            {
                app.Invoke(() => detailsLabel.Text = $"{displayName}  |  NodeId: {nodeId}  |  (unable to read attributes)");
                return;
            }

            var parts = new List<string>
            {
                $"NodeId: {attributes.NodeId}",
                $"Class: {attributes.NodeClass}",
            };

            if (!string.IsNullOrEmpty(attributes.DataTypeName))
            {
                parts.Add($"DataType: {attributes.DataTypeName}");
            }

            if (attributes.Value is not null)
            {
                parts.Add($"Value: {attributes.Value}");
            }

            if (attributes.AccessLevel.HasValue)
            {
                parts.Add($"Access: {FormatAccessLevel(attributes.AccessLevel.Value)}");
            }

            if (attributes.StatusCode != 0)
            {
                parts.Add($"Status: 0x{attributes.StatusCode:X8}");
            }
            else
            {
                parts.Add("Status: Good");
            }

            var text = string.Join("  |  ", parts);

            app.Invoke(() => detailsLabel.Text = text);
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception)
        {
            app.Invoke(() => detailsLabel.Text = $"{displayName}  |  NodeId: {nodeId}  |  (error reading attributes)");
        }
    }

    public void Clear()
    {
        detailsLabel.Text = "Select a node to view details.";
    }

    private static string FormatAccessLevel(byte accessLevel)
    {
        var readable = (accessLevel & 0x01) != 0;
        var writable = (accessLevel & 0x02) != 0;

        return (readable, writable) switch
        {
            (true, true) => "ReadWrite",
            (true, false) => "Read",
            (false, true) => "Write",
            _ => "None",
        };
    }
}
