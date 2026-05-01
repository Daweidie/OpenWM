using OpenWM.Core;
using OpenWM.Layout;

namespace OpenWM.Tests.Layout;

public class MasterLayoutStrategyTests
{
    [Fact]
    public void Arrange_WithTwoWindows_AssignsMasterAndStack()
    {
        var strategy = new MasterLayoutStrategy();
        var windows = new[]
        {
            new WindowInfo(new WindowHandle(1), "A", "X"),
            new WindowInfo(new WindowHandle(2), "B", "X"),
        };

        var result = strategy.Arrange(windows, new Rect(0, 0, 1000, 600), 0, 0.6);

        Assert.Equal(2, result.Count);
        Assert.True(result[0].Bounds.Width > result[1].Bounds.Width);
    }
}
