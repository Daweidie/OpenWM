using OpenWM.Core;

namespace OpenWM.Layout;

public sealed class FloatingLayoutStrategy : ILayoutStrategy
{
    public LayoutKind Kind => LayoutKind.Floating;

    public IReadOnlyList<PositionedWindow> Arrange(IReadOnlyList<WindowInfo> windows, Rect workArea, int gaps, double masterRatio)
    {
        // Floating layout keeps current positions, so no automatic placement.
        return [];
    }
}
