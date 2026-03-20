using OpenWM.Native;

namespace OpenWM.Layout;

/// <summary>
/// Master-stack layout: one master window on the left, remaining windows stacked
/// on the right — similar to Hyprland's "master" layout.
/// </summary>
public sealed class MasterLayout : ILayout
{
    public string Name => "master";

    /// <summary>Fraction of the work area width assigned to the master window (0–1).</summary>
    public double MasterRatio { get; set; } = 0.55;

    public Dictionary<IntPtr, RECT> Arrange(IReadOnlyList<IntPtr> windows, RECT workArea, int gaps)
    {
        var result = new Dictionary<IntPtr, RECT>();
        if (windows.Count == 0) return result;

        int half = gaps / 2;
        int innerLeft  = workArea.Left  + gaps;
        int innerTop   = workArea.Top   + gaps;
        int innerRight = workArea.Right  - gaps;
        int innerBottom = workArea.Bottom - gaps;

        if (windows.Count == 1)
        {
            result[windows[0]] = new RECT(innerLeft, innerTop, innerRight, innerBottom);
            return result;
        }

        int masterRight = workArea.Left + (int)((workArea.Width) * MasterRatio) - half;
        int stackLeft   = masterRight + gaps;

        // Master window
        var masterRect = new RECT(innerLeft, innerTop, masterRight, innerBottom);
        result[windows[0]] = masterRect;

        // Stack windows evenly
        int stackCount  = windows.Count - 1;
        int totalHeight = innerBottom - innerTop;
        int slotHeight  = totalHeight / stackCount;

        for (int i = 0; i < stackCount; i++)
        {
            int top    = innerTop + i * slotHeight;
            int bottom = (i == stackCount - 1) ? innerBottom : top + slotHeight - half;
            if (i > 0) top += half;

            result[windows[i + 1]] = new RECT(stackLeft, top, innerRight, bottom);
        }

        return result;
    }
}
