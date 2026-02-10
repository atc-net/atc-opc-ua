namespace Atc.Opc.Ua.CLI.Tui.Views;

/// <summary>
/// Real-time oscilloscope view using braille character rendering.
/// Displays up to 5 signals with auto-scaling Y axis and configurable time window.
/// </summary>
[SuppressMessage("Reliability", "CA2213:Disposable fields should be disposed", Justification = "Terminal.Gui manages child view disposal")]
[SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "Terminal.Gui manages view lifecycle")]
[SuppressMessage("Major Code Smell", "S4487:Unread \"private\" fields should be removed", Justification = "Timer handle kept for future cancellation support")]
public sealed class ScopeView : FrameView
{
    private const int MaxSignals = 5;
    private const int DefaultBufferSize = 500;

    private readonly IApplication app;
    private readonly Dictionary<uint, SignalBuffer> signals = [];
    private readonly Lock renderLock = new();
    private BrailleCanvas? canvas;
    private object? updateTimer;
    private bool timerRunning;

    public ScopeView(IApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        this.app = app;
        Title = "Scope";
        CanFocus = false;
    }

    /// <summary>
    /// Adds a signal to the scope display.
    /// </summary>
    /// <param name="handle">The monitored item handle.</param>
    /// <param name="displayName">The display name of the signal.</param>
    /// <returns>True if the signal was added; false if at capacity.</returns>
    public bool AddSignal(uint handle, string displayName)
    {
        lock (renderLock)
        {
            if (signals.Count >= MaxSignals || signals.ContainsKey(handle))
            {
                return false;
            }

            signals[handle] = new SignalBuffer(displayName, DefaultBufferSize);
            EnsureTimerRunning();
            return true;
        }
    }

    /// <summary>
    /// Removes a signal from the scope display.
    /// </summary>
    /// <param name="handle">The monitored item handle.</param>
    public void RemoveSignal(uint handle)
    {
        lock (renderLock)
        {
            signals.Remove(handle);
        }
    }

    /// <summary>
    /// Pushes a new sample value for a signal.
    /// </summary>
    /// <param name="handle">The monitored item handle.</param>
    /// <param name="value">The sample value.</param>
    public void PushSample(uint handle, double value)
    {
        lock (renderLock)
        {
            if (signals.TryGetValue(handle, out var buffer))
            {
                buffer.Add(value);
            }
        }
    }

    /// <summary>
    /// Clears all signals.
    /// </summary>
    public void Clear()
    {
        lock (renderLock)
        {
            signals.Clear();
        }
    }

    protected override bool OnDrawingContent(Terminal.Gui.ViewBase.DrawContext? context)
    {
        base.OnDrawingContent(context);

        var innerWidth = Viewport.Width;
        var innerHeight = Viewport.Height;

        if (innerWidth <= 0 || innerHeight <= 0)
        {
            return true;
        }

        lock (renderLock)
        {
            if (signals.Count == 0)
            {
                DrawEmptyState(innerWidth, innerHeight);
                return true;
            }

            RenderSignals(innerWidth, innerHeight);
        }

        return true;
    }

    private void DrawEmptyState(int width, int height)
    {
        var msg = "No signals. Subscribe to variables, they will appear here.";
        var x = (width - msg.Length) / 2;
        var y = height / 2;

        if (x >= 0 && y >= 0)
        {
            Move(x, y);
            AddStr(msg);
        }
    }

    private void RenderSignals(int charWidth, int charHeight)
    {
        if (canvas is null || canvas.PixelWidth != charWidth * 2 || canvas.PixelHeight != charHeight * 4)
        {
            canvas = new BrailleCanvas(charWidth, charHeight);
        }

        canvas.Clear();

        // Calculate global min/max across all signals for auto-scaling
        var globalMin = double.MaxValue;
        var globalMax = double.MinValue;

        foreach (var signal in signals.Values)
        {
            if (signal.Count == 0)
            {
                continue;
            }

            var (min, max) = signal.GetMinMax();
            if (min < globalMin)
            {
                globalMin = min;
            }

            if (max > globalMax)
            {
                globalMax = max;
            }
        }

        if (globalMin >= globalMax)
        {
            // All values are the same; add a small margin
            globalMin -= 1.0;
            globalMax += 1.0;
        }

        var pixelWidth = canvas.PixelWidth;
        var pixelHeight = canvas.PixelHeight;
        var range = globalMax - globalMin;

        // Draw each signal
        foreach (var signal in signals.Values)
        {
            if (signal.Count < 2)
            {
                continue;
            }

            DrawSignal(signal, pixelWidth, pixelHeight, globalMin, range);
        }

        // Render braille canvas to terminal
        for (var cy = 0; cy < charHeight; cy++)
        {
            Move(0, cy);
            for (var cx = 0; cx < charWidth; cx++)
            {
                AddRune(canvas.GetChar(cx, cy));
            }
        }

        // Draw legend at bottom
        DrawLegend(charWidth, charHeight);
    }

    private void DrawSignal(SignalBuffer signal, int pixelWidth, int pixelHeight, double min, double range)
    {
        var samples = signal.GetSamples();
        var step = (double)(samples.Length - 1) / (pixelWidth - 1);

        var prevX = 0;
        var prevY = MapToPixelY(samples[0], pixelHeight, min, range);

        for (var px = 1; px < pixelWidth; px++)
        {
            var sampleIndex = (int)(px * step);
            if (sampleIndex >= samples.Length)
            {
                sampleIndex = samples.Length - 1;
            }

            var curY = MapToPixelY(samples[sampleIndex], pixelHeight, min, range);
            canvas!.DrawLine(prevX, prevY, px, curY);
            prevX = px;
            prevY = curY;
        }
    }

    private static int MapToPixelY(double value, int pixelHeight, double min, double range)
    {
        var normalized = (value - min) / range;
        var py = (int)((1.0 - normalized) * (pixelHeight - 1));
        return System.Math.Clamp(py, 0, pixelHeight - 1);
    }

    private void DrawLegend(int charWidth, int charHeight)
    {
        if (charHeight < 2)
        {
            return;
        }

        var legend = new StringBuilder();
        var index = 0;
        foreach (var signal in signals.Values)
        {
            if (legend.Length > 0)
            {
                legend.Append("  ");
            }

            legend.Append(CultureInfo.InvariantCulture, $"[{index}] {signal.DisplayName}");

            if (signal.Count > 0)
            {
                legend.Append(CultureInfo.InvariantCulture, $": {signal.LastValue:F2}");
            }

            index++;
        }

        var legendText = legend.ToString();
        if (legendText.Length > charWidth)
        {
            legendText = legendText[..charWidth];
        }

        Move(0, charHeight - 1);
        AddStr(legendText);
    }

    private void EnsureTimerRunning()
    {
        if (timerRunning)
        {
            return;
        }

        timerRunning = true;
        updateTimer = app.AddTimeout(
            TimeSpan.FromMilliseconds(100),
            OnRedrawTimer);
    }

    private bool OnRedrawTimer()
    {
        SetNeedsDraw();

        lock (renderLock)
        {
            if (signals.Count == 0)
            {
                timerRunning = false;
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Circular buffer for storing signal sample values.
    /// </summary>
    private sealed class SignalBuffer
    {
        private readonly double[] buffer;
        private int head;
        private int count;

        public SignalBuffer(string displayName, int capacity)
        {
            DisplayName = displayName;
            buffer = new double[capacity];
        }

        public string DisplayName { get; }

        public int Count => count;

        public double LastValue => count > 0 ? buffer[(head - 1 + buffer.Length) % buffer.Length] : 0;

        public void Add(double value)
        {
            buffer[head] = value;
            head = (head + 1) % buffer.Length;
            if (count < buffer.Length)
            {
                count++;
            }
        }

        public double[] GetSamples()
        {
            var result = new double[count];
            var start = (head - count + buffer.Length) % buffer.Length;
            for (var i = 0; i < count; i++)
            {
                result[i] = buffer[(start + i) % buffer.Length];
            }

            return result;
        }

        public (double Min, double Max) GetMinMax()
        {
            var min = double.MaxValue;
            var max = double.MinValue;
            var start = (head - count + buffer.Length) % buffer.Length;
            for (var i = 0; i < count; i++)
            {
                var v = buffer[(start + i) % buffer.Length];
                if (v < min)
                {
                    min = v;
                }

                if (v > max)
                {
                    max = v;
                }
            }

            return (min, max);
        }
    }
}
