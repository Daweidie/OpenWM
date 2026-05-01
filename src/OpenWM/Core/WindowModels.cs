namespace OpenWM.Core;

public readonly record struct WindowHandle(nint Value)
{
    public bool IsValid => Value != 0;
    public override string ToString() => $"0x{Value:X}";
}

public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public int Right => X + Width;
    public int Bottom => Y + Height;

    public Rect Shrink(int gap)
    {
        if (gap <= 0)
        {
            return this;
        }

        var w = Math.Max(0, Width - (gap * 2));
        var h = Math.Max(0, Height - (gap * 2));
        return new Rect(X + gap, Y + gap, w, h);
    }
}

public sealed record WindowInfo(WindowHandle Handle, string Title, string ClassName)
{
    public bool IsFloating { get; set; }
    public bool IsFullscreen { get; set; }
    public bool IsManaged { get; set; } = true;
}

public sealed record PositionedWindow(WindowInfo Window, Rect Bounds);
