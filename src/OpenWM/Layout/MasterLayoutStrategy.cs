using OpenWM.Core;

namespace OpenWM.Layout;

public sealed class MasterLayoutStrategy : ILayoutStrategy
{
    public LayoutKind Kind => LayoutKind.Master;

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

        var result = new List<PositionedWindow>(windows.Count);
        var masterWidth = (int)(workArea.Width * masterRatio);
        masterWidth = Math.Clamp(masterWidth, workArea.Width / 4, (workArea.Width * 3) / 4);

        var masterRect = new Rect(workArea.X, workArea.Y, masterWidth, workArea.Height).Shrink(gaps);
        result.Add(new PositionedWindow(windows[0], masterRect));

        var stackCount = windows.Count - 1;
        var stackWidth = workArea.Width - masterWidth;
        var stackX = workArea.X + masterWidth;
        var eachHeight = stackCount == 0 ? workArea.Height : workArea.Height / stackCount;

        for (var i = 1; i < windows.Count; i++)
        {
            var y = workArea.Y + ((i - 1) * eachHeight);
            var h = i == windows.Count - 1
                ? workArea.Bottom - y
                : eachHeight;
            var rect = new Rect(stackX, y, stackWidth, h).Shrink(gaps);
            result.Add(new PositionedWindow(windows[i], rect));
        }

        return result;
    }
}
