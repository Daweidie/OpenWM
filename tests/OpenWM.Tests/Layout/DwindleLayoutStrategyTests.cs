using OpenWM.Core;
using OpenWM.Layout;

namespace OpenWM.Tests.Layout;

public class DwindleLayoutStrategyTests
{
    [Fact]
    public void Arrange_WithThreeWindows_ReturnsThreePlacements()
    {
        var strategy = new DwindleLayoutStrategy();
        var windows = new[]
        {
            new WindowInfo(new WindowHandle(1), "A", "X"),
            new WindowInfo(new WindowHandle(2), "B", "X"),
            new WindowInfo(new WindowHandle(3), "C", "X"),
        };

        var result = strategy.Arrange(windows, new Rect(0, 0, 1200, 800), 8, 0.55);

        Assert.Equal(3, result.Count);
        Assert.All(result, p => Assert.True(p.Bounds.Width > 0));
        Assert.All(result, p => Assert.True(p.Bounds.Height > 0));
    }
}
