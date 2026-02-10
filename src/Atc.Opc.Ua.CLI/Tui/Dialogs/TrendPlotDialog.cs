namespace Atc.Opc.Ua.CLI.Tui.Dialogs;

/// <summary>
/// Modal dialog that displays a time-based trend plot for a single variable
/// using braille character rendering.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
public sealed class TrendPlotDialog : Dialog
{
    private readonly List<(DateTime Time, double Value)> history;
    private readonly BrailleCanvas canvas;
    private readonly Label statsLabel;
    private readonly int plotWidth;
    private readonly int plotHeight;

    private TrendPlotDialog(
        string displayName,
        List<(DateTime Time, double Value)> history)
    {
        this.history = history;

        Title = $"Trend: {displayName}";
        Width = Dim.Percent(80);
        Height = Dim.Percent(80);

        // Reserve fixed sizes for the braille plot
        plotWidth = 60;
        plotHeight = 15;
        canvas = new BrailleCanvas(plotWidth, plotHeight);

        statsLabel = new Label
        {
            X = 1,
            Y = plotHeight + 1,
            Width = Dim.Fill(1),
        };

        Add(statsLabel);
    }

    /// <summary>
    /// Shows the trend plot dialog for a set of historical data points.
    /// </summary>
    /// <param name="app">The Terminal.Gui application instance.</param>
    /// <param name="displayName">The display name of the variable.</param>
    /// <param name="history">The historical data points (time, value).</param>
    public static void Show(
        IApplication app,
        string displayName,
        List<(DateTime Time, double Value)> history)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(history);

        if (history.Count < 2)
        {
            MessageBox.Query(app, "Trend", "Not enough data points to plot.", "OK");
            return;
        }

        var dialog = new TrendPlotDialog(displayName, history);
        dialog.RenderPlot();

        var closeButton = new Button
        {
            Text = "Close",
            X = Pos.Center(),
            Y = Pos.AnchorEnd(2),
            IsDefault = true,
        };

        closeButton.Accepting += (_, e) =>
        {
            app.RequestStop();
            e.Handled = true;
        };

        dialog.Add(closeButton);
        app.Run(dialog);
    }

    protected override bool OnDrawingContent(Terminal.Gui.ViewBase.DrawContext? context)
    {
        base.OnDrawingContent(context);

        // Draw the braille plot
        for (var cy = 0; cy < plotHeight; cy++)
        {
            Move(1, cy);
            for (var cx = 0; cx < plotWidth; cx++)
            {
                AddRune(canvas.GetChar(cx, cy));
            }
        }

        return true;
    }

    private void RenderPlot()
    {
        canvas.Clear();

        var min = history.Min(h => h.Value);
        var max = history.Max(h => h.Value);

        if (min >= max)
        {
            min -= 1.0;
            max += 1.0;
        }

        var range = max - min;
        var pixelWidth = canvas.PixelWidth;
        var pixelHeight = canvas.PixelHeight;
        var step = (double)(history.Count - 1) / (pixelWidth - 1);

        var prevY = MapY(history[0].Value, pixelHeight, min, range);
        for (var px = 1; px < pixelWidth; px++)
        {
            var idx = (int)(px * step);
            if (idx >= history.Count)
            {
                idx = history.Count - 1;
            }

            var curY = MapY(history[idx].Value, pixelHeight, min, range);
            canvas.DrawLine(px - 1, prevY, px, curY);
            prevY = curY;
        }

        // Update stats
        var avg = history.Average(h => h.Value);
        var timeSpan = history[^1].Time - history[0].Time;
        statsLabel.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"Min: {min:F2}  Max: {max:F2}  Avg: {avg:F2}  Points: {history.Count}  Duration: {timeSpan:hh\\:mm\\:ss}");
    }

    private static int MapY(double value, int pixelHeight, double min, double range)
    {
        var normalized = (value - min) / range;
        var py = (int)((1.0 - normalized) * (pixelHeight - 1));
        return System.Math.Clamp(py, 0, pixelHeight - 1);
    }
}
