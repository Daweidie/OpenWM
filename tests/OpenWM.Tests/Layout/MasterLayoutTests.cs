using OpenWM.Layout;
using OpenWM.Native;

namespace OpenWM.Tests.Layout;

public class MasterLayoutTests
{
    private static readonly RECT Screen = new(0, 0, 1920, 1080);

    [Fact]
    public void NoWindows_ReturnsEmpty()
    {
        var layout = new MasterLayout();
        var result = layout.Arrange(Array.Empty<IntPtr>(), Screen, gaps: 0);
        Assert.Empty(result);
    }

    [Fact]
    public void SingleWindow_FillsWorkArea_WithGaps()
    {
        var layout = new MasterLayout();
        var handle = new IntPtr(1);
        var result = layout.Arrange(new[] { handle }, Screen, gaps: 8);

        Assert.Single(result);
        var r = result[handle];
        Assert.Equal(8, r.Left);
        Assert.Equal(8, r.Top);
        Assert.Equal(Screen.Right  - 8, r.Right);
        Assert.Equal(Screen.Bottom - 8, r.Bottom);
    }

    [Fact]
    public void TwoWindows_MasterOnLeft_StackOnRight()
    {
        var layout = new MasterLayout { MasterRatio = 0.5 };
        var master = new IntPtr(1);
        var stack  = new IntPtr(2);
        var result = layout.Arrange(new[] { master, stack }, Screen, gaps: 0);

        Assert.Equal(2, result.Count);
        // Master should be on the left
        Assert.True(result[master].Right <= result[stack].Left,
            "Master right edge should be left of stack left edge");
    }

    [Fact]
    public void MasterRatio_AffectsMasterWidth()
    {
        var narrow = new MasterLayout { MasterRatio = 0.3 };
        var wide   = new MasterLayout { MasterRatio = 0.7 };
        var handles = new[] { new IntPtr(1), new IntPtr(2) };

        var narrowResult = narrow.Arrange(handles, Screen, gaps: 0);
        var wideResult   = wide.Arrange(handles, Screen, gaps: 0);

        Assert.True(narrowResult[handles[0]].Width < wideResult[handles[0]].Width,
            "Wider ratio should give master window more width");
    }

    [Fact]
    public void MultipleStackWindows_ShareVerticalSpace()
    {
        var layout  = new MasterLayout();
        var handles = Enumerable.Range(1, 4).Select(i => new IntPtr(i)).ToList();
        var result  = layout.Arrange(handles, Screen, gaps: 0);

        Assert.Equal(4, result.Count);

        // All stack windows should be on the right of the master
        var master = result[handles[0]];
        for (int i = 1; i < handles.Count; i++)
        {
            Assert.True(result[handles[i]].Left >= master.Right,
                $"Stack window {i} should be to the right of master");
        }
    }

    [Fact]
    public void AllWindowsHavePositiveDimensions()
    {
        var layout  = new MasterLayout();
        var handles = Enumerable.Range(1, 6).Select(i => new IntPtr(i)).ToList();
        var result  = layout.Arrange(handles, Screen, gaps: 8);

        Assert.All(result.Values, r =>
        {
            Assert.True(r.Width  > 0, $"Width must be > 0, got {r.Width}");
            Assert.True(r.Height > 0, $"Height must be > 0, got {r.Height}");
        });
    }
}
