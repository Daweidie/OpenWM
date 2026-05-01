using OpenWM.Core;

namespace OpenWM.Layout;

public interface ILayoutStrategy
{
    LayoutKind Kind { get; }
    IReadOnlyList<PositionedWindow> Arrange(IReadOnlyList<WindowInfo> windows, Rect workArea, int gaps, double masterRatio);
}
