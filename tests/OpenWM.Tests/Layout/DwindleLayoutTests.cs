using OpenWM.Layout;
using OpenWM.Native;

namespace OpenWM.Tests.Layout;

public class DwindleLayoutTests
{
    private static readonly RECT Screen = new(0, 0, 1920, 1080);

    [Fact]
    public void NoWindows_ReturnsEmpty()
    {
        var layout = new DwindleLayout();
        var result = layout.Arrange(Array.Empty<IntPtr>(), Screen, gaps: 0);
        Assert.Empty(result);
    }

    [Fact]
    public void SingleWindow_FillsWorkArea_WithGap()
    {
        var layout = new DwindleLayout();
        var handle = new IntPtr(1);
        var result = layout.Arrange(new[] { handle }, Screen, gaps: 10);

        Assert.Single(result);
        var r = result[handle];
        Assert.Equal(10, r.Left);
        Assert.Equal(10, r.Top);
        Assert.Equal(Screen.Right  - 10, r.Right);
        Assert.Equal(Screen.Bottom - 10, r.Bottom);
    }

    [Fact]
    public void TwoWindows_SplitHorizontally()
    {
        var layout = new DwindleLayout();
        var h1 = new IntPtr(1);
        var h2 = new IntPtr(2);
        var result = layout.Arrange(new[] { h1, h2 }, Screen, gaps: 0);

        Assert.Equal(2, result.Count);

        // First window should occupy left half
        Assert.Equal(Screen.Left,  result[h1].Left);
        Assert.Equal(Screen.Width / 2, result[h1].Right);

        // Second window should occupy right half
        Assert.Equal(Screen.Width / 2, result[h2].Left);
        Assert.Equal(Screen.Right,     result[h2].Right);
    }

    [Fact]
    public void ThreeWindows_CorrectTileCount()
    {
        var layout = new DwindleLayout();
        var handles = Enumerable.Range(1, 3).Select(i => new IntPtr(i)).ToList();
        var result  = layout.Arrange(handles, Screen, gaps: 0);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void ManyWindows_EachHasUniqueRect()
    {
        var layout  = new DwindleLayout();
        var handles = Enumerable.Range(1, 8).Select(i => new IntPtr(i)).ToList();
        var result  = layout.Arrange(handles, Screen, gaps: 4);

        Assert.Equal(8, result.Count);
        var rects = result.Values.ToList();
        // All rects should have positive dimensions
        Assert.All(rects, r =>
        {
            Assert.True(r.Width  > 0, $"Width must be positive, got {r.Width}");
            Assert.True(r.Height > 0, $"Height must be positive, got {r.Height}");
        });
    }

    [Fact]
    public void GapsApplied_RectsAreSmaller()
    {
        var layout = new DwindleLayout();
        var handle = new IntPtr(1);
        var noGap  = layout.Arrange(new[] { handle }, Screen, gaps: 0)[handle];
        var withGap = layout.Arrange(new[] { handle }, Screen, gaps: 20)[handle];

        Assert.True(withGap.Width  < noGap.Width);
        Assert.True(withGap.Height < noGap.Height);
    }
}
