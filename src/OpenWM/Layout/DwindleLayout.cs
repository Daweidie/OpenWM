using OpenWM.Native;

namespace OpenWM.Layout;

/// <summary>
/// Dwindle layout: the first window takes up the left half; each subsequent window
/// splits the remaining space alternately horizontally and vertically — identical to
/// Hyprland's "dwindle" tiling strategy.
/// </summary>
public sealed class DwindleLayout : ILayout
{
    public string Name => "dwindle";

    public Dictionary<IntPtr, RECT> Arrange(IReadOnlyList<IntPtr> windows, RECT workArea, int gaps)
    {
        var result = new Dictionary<IntPtr, RECT>();
        if (windows.Count == 0) return result;

        if (windows.Count == 1)
        {
            result[windows[0]] = Shrink(workArea, gaps, outerGap: true);
            return result;
        }

        // Recursively split the available space
        ArrangeRecursive(windows, 0, workArea, gaps, splitHorizontal: true, result);
        return result;
    }

    private static void ArrangeRecursive(
        IReadOnlyList<IntPtr> windows,
        int startIndex,
        RECT space,
        int gaps,
        bool splitHorizontal,
        Dictionary<IntPtr, RECT> result)
    {
        int remaining = windows.Count - startIndex;
        if (remaining <= 0) return;

        if (remaining == 1)
        {
            result[windows[startIndex]] = Shrink(space, gaps, outerGap: false);
            return;
        }

        // Split the space: first window gets half, rest recurse into the other half.
        RECT first, rest;
        if (splitHorizontal)
        {
            int splitX = space.Left + space.Width / 2;
            first = new RECT(space.Left, space.Top, splitX, space.Bottom);
            rest  = new RECT(splitX, space.Top, space.Right, space.Bottom);
        }
        else
        {
            int splitY = space.Top + space.Height / 2;
            first = new RECT(space.Left, space.Top, space.Right, splitY);
            rest  = new RECT(space.Left, splitY, space.Right, space.Bottom);
        }

        result[windows[startIndex]] = Shrink(first, gaps, outerGap: false);
        ArrangeRecursive(windows, startIndex + 1, rest, gaps, !splitHorizontal, result);
    }

    /// <summary>Apply gap padding to a rectangle.</summary>
    private static RECT Shrink(RECT r, int gaps, bool outerGap)
    {
        int half = gaps / 2;
        int outer = outerGap ? gaps : half;
        return new RECT(
            r.Left + outer,
            r.Top + outer,
            r.Right - outer,
            r.Bottom - outer);
    }
}
