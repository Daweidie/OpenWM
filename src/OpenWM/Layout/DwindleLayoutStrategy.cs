using OpenWM.Core;

namespace OpenWM.Layout;

public sealed class DwindleLayoutStrategy : ILayoutStrategy
{
    public LayoutKind Kind => LayoutKind.Dwindle;

    public IReadOnlyList<PositionedWindow> Arrange(IReadOnlyList<WindowInfo> windows, Rect workArea, int gaps, double masterRatio)
    {
        if (windows.Count == 0)
        {
            return [];
        }

        var result = new List<PositionedWindow>(windows.Count);
        var remaining = workArea;

        for (var i = 0; i < windows.Count; i++)
        {
            if (i == windows.Count - 1)
            {
                result.Add(new PositionedWindow(windows[i], remaining.Shrink(gaps)));
                break;
            }

            var splitVertical = i % 2 == 0;
            if (splitVertical)
            {
                var left = new Rect(remaining.X, remaining.Y, Math.Max(1, remaining.Width / 2), remaining.Height);
                var rightWidth = Math.Max(1, remaining.Width - left.Width);
                remaining = new Rect(remaining.X + left.Width, remaining.Y, rightWidth, remaining.Height);
                result.Add(new PositionedWindow(windows[i], left.Shrink(gaps)));
            }
            else
            {
                var top = new Rect(remaining.X, remaining.Y, remaining.Width, Math.Max(1, remaining.Height / 2));
                var bottomHeight = Math.Max(1, remaining.Height - top.Height);
                remaining = new Rect(remaining.X, remaining.Y + top.Height, remaining.Width, bottomHeight);
                result.Add(new PositionedWindow(windows[i], top.Shrink(gaps)));
            }
        }

        return result;
    }
}
