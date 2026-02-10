namespace Atc.Opc.Ua.CLI.Tui.Views;

/// <summary>
/// Lightweight braille character canvas for sub-character resolution plotting.
/// Each terminal character cell maps to a 2x4 pixel grid using Unicode braille
/// characters (U+2800..U+28FF), providing 2x horizontal and 4x vertical resolution.
/// </summary>
public sealed class BrailleCanvas
{
    // Braille dot positions within a 2x4 cell:
    // (0,0)→bit0  (1,0)→bit3
    // (0,1)→bit1  (1,1)→bit4
    // (0,2)→bit2  (1,2)→bit5
    // (0,3)→bit6  (1,3)→bit7
    private static readonly int[][] DotBits =
    [
        [0x01, 0x02, 0x04, 0x40],
        [0x08, 0x10, 0x20, 0x80],
    ];

    private readonly int charWidth;
    private readonly int charHeight;
    private readonly int[][] cells;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrailleCanvas"/> class.
    /// </summary>
    /// <param name="charWidth">Width in terminal character columns.</param>
    /// <param name="charHeight">Height in terminal character rows.</param>
    public BrailleCanvas(int charWidth, int charHeight)
    {
        this.charWidth = charWidth;
        this.charHeight = charHeight;
        cells = new int[charWidth][];
        for (var i = 0; i < charWidth; i++)
        {
            cells[i] = new int[charHeight];
        }
    }

    /// <summary>
    /// Gets the pixel width (2x character width).
    /// </summary>
    public int PixelWidth => charWidth * 2;

    /// <summary>
    /// Gets the pixel height (4x character height).
    /// </summary>
    public int PixelHeight => charHeight * 4;

    /// <summary>
    /// Clears all pixels.
    /// </summary>
    public void Clear()
    {
        for (var i = 0; i < charWidth; i++)
        {
            Array.Clear(cells[i]);
        }
    }

    /// <summary>
    /// Sets a braille pixel at the given position.
    /// </summary>
    /// <param name="x">Pixel X coordinate (0 to PixelWidth-1).</param>
    /// <param name="y">Pixel Y coordinate (0 to PixelHeight-1).</param>
    public void Set(int x, int y)
    {
        if (x < 0 || x >= PixelWidth || y < 0 || y >= PixelHeight)
        {
            return;
        }

        var cx = x / 2;
        var cy = y / 4;
        var px = x % 2;
        var py = y % 4;

        cells[cx][cy] |= DotBits[px][py];
    }

    /// <summary>
    /// Draws a line between two braille pixel coordinates using Bresenham's algorithm.
    /// </summary>
    /// <param name="x0">Start X.</param>
    /// <param name="y0">Start Y.</param>
    /// <param name="x1">End X.</param>
    /// <param name="y1">End Y.</param>
    public void DrawLine(int x0, int y0, int x1, int y1)
    {
        var dx = System.Math.Abs(x1 - x0);
        var dy = System.Math.Abs(y1 - y0);
        var sx = x0 < x1 ? 1 : -1;
        var sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;

        while (true)
        {
            Set(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    /// <summary>
    /// Gets the braille character at the given character cell position.
    /// </summary>
    /// <param name="cx">Character column.</param>
    /// <param name="cy">Character row.</param>
    /// <returns>The braille Unicode character.</returns>
    public char GetChar(int cx, int cy)
    {
        if (cx < 0 || cx >= charWidth || cy < 0 || cy >= charHeight)
        {
            return ' ';
        }

        var bits = cells[cx][cy];
        return bits == 0 ? ' ' : (char)(0x2800 + bits);
    }
}
