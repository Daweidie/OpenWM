using OpenWM.Native;

namespace OpenWM.Layout;

/// <summary>
/// Floating layout: windows are not repositioned automatically.
/// Returns an empty arrangement so callers know not to move anything.
/// </summary>
public sealed class FloatingLayout : ILayout
{
    public string Name => "floating";

    public Dictionary<IntPtr, RECT> Arrange(IReadOnlyList<IntPtr> windows, RECT workArea, int gaps)
        => new();
}
