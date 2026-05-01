using OpenWM.Core;

namespace OpenWM.Layout;

public sealed class DynamicLayoutStrategy : ILayoutStrategy
{
    public LayoutKind Kind => LayoutKind.Dynamic;

    public IReadOnlyList<PositionedWindow> Arrange(IReadOnlyList<WindowInfo> windows, Rect workArea, int gaps, double masterRatio)
    {
        if (windows.Count == 0)
        {
            return [];
        }

        if (windows.Count == 1)
        {
            return [new PositionedWindow(windows[0], workArea.Shrink(gaps))];
        }

        if (windows.Count == 2)
        {
            var half = Math.Max(1, workArea.Width / 2);
            var left = new Rect(workArea.X, workArea.Y, half, workArea.Height).Shrink(gaps);
            var right = new Rect(workArea.X + half, workArea.Y, workArea.Width - half, workArea.Height).Shrink(gaps);
            return [
                new PositionedWindow(windows[0], left),
                new PositionedWindow(windows[1], right),
            ];
        }

        // Hyprland-like dynamic behavior: 1 master + alternating stack split.
        var result = new List<PositionedWindow>(windows.Count);
        var dynamicRatio = Math.Clamp(masterRatio + (Math.Min(4, windows.Count) * 0.03), 0.4, 0.75);
        var masterWidth = (int)(workArea.Width * dynamicRatio);
        var masterRect = new Rect(workArea.X, workArea.Y, masterWidth, workArea.Height).Shrink(gaps);
        result.Add(new PositionedWindow(windows[0], masterRect));

        var stackRect = new Rect(workArea.X + masterWidth, workArea.Y, workArea.Width - masterWidth, workArea.Height);
        var stackWindows = windows.Skip(1).ToList();
        var remaining = stackRect;

        for (var i = 0; i < stackWindows.Count; i++)
        {
            if (i == stackWindows.Count - 1)
            {
                result.Add(new PositionedWindow(stackWindows[i], remaining.Shrink(gaps)));
                break;
            }

            var splitHorizontal = i % 2 == 0;
            if (splitHorizontal)
            {
                var topHeight = Math.Max(1, remaining.Height / 2);
                var top = new Rect(remaining.X, remaining.Y, remaining.Width, topHeight);
                remaining = new Rect(remaining.X, remaining.Y + topHeight, remaining.Width, remaining.Height - topHeight);
                result.Add(new PositionedWindow(stackWindows[i], top.Shrink(gaps)));
            }
            else
            {
                var leftWidth = Math.Max(1, remaining.Width / 2);
                var left = new Rect(remaining.X, remaining.Y, leftWidth, remaining.Height);
                remaining = new Rect(remaining.X + leftWidth, remaining.Y, remaining.Width - leftWidth, remaining.Height);
                result.Add(new PositionedWindow(stackWindows[i], left.Shrink(gaps)));
            }
        }

        return result;
    }
}
