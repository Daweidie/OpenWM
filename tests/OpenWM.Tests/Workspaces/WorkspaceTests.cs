using OpenWM.Core;
using OpenWM.Layout;
using OpenWM.Native;
using OpenWM.Workspaces;

namespace OpenWM.Tests.Workspaces;

public class WorkspaceTests
{
    private static readonly RECT WorkArea = new(0, 0, 1920, 1080);

    private static WindowInfo MakeWindow(int id, int wsId = 1) =>
        new(new IntPtr(id), $"Window {id}", "TestClass", (uint)id, wsId);

    [Fact]
    public void AddWindow_IncreaseCount()
    {
        var ws = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        ws.AddWindow(MakeWindow(1));
        Assert.Single(ws.Windows);
    }

    [Fact]
    public void AddWindow_Duplicate_NotAddedTwice()
    {
        var ws  = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        var win = MakeWindow(1);
        ws.AddWindow(win);
        ws.AddWindow(win);
        Assert.Single(ws.Windows);
    }

    [Fact]
    public void RemoveWindow_DecreasesCount()
    {
        var ws  = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        var win = MakeWindow(1);
        ws.AddWindow(win);
        ws.RemoveWindow(win);
        Assert.Empty(ws.Windows);
    }

    [Fact]
    public void ContainsWindow_ReturnsCorrectly()
    {
        var ws  = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        var win = MakeWindow(1);
        Assert.False(ws.ContainsWindow(win));
        ws.AddWindow(win);
        Assert.True(ws.ContainsWindow(win));
    }

    [Fact]
    public void PromoteWindow_MovesToFront()
    {
        var ws   = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        var win1 = MakeWindow(1);
        var win2 = MakeWindow(2);
        var win3 = MakeWindow(3);
        ws.AddWindow(win1);
        ws.AddWindow(win2);
        ws.AddWindow(win3);

        ws.PromoteWindow(win3);
        Assert.Equal(win3, ws.Windows[1]); // promoted one step toward front

        ws.PromoteWindow(win3);
        Assert.Equal(win3, ws.Windows[0]); // now at the front
    }

    [Fact]
    public void SwapWindows_ExchangesPositions()
    {
        var ws   = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        var win1 = MakeWindow(1);
        var win2 = MakeWindow(2);
        ws.AddWindow(win1);
        ws.AddWindow(win2);

        ws.SwapWindows(0, 1);
        Assert.Equal(win2, ws.Windows[0]);
        Assert.Equal(win1, ws.Windows[1]);
    }

    [Fact]
    public void CalculateLayout_ExcludesFloatingWindows()
    {
        var ws      = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        var tiled   = MakeWindow(1);
        var floating = MakeWindow(2);
        floating.IsFloating = true;

        ws.AddWindow(tiled);
        ws.AddWindow(floating);

        var layout = ws.CalculateLayout(gaps: 0);
        Assert.True(layout.ContainsKey(tiled.Handle));
        Assert.False(layout.ContainsKey(floating.Handle));
    }

    [Fact]
    public void CalculateLayout_ExcludesFullscreenWindows()
    {
        var ws         = new Workspace(1, "WS 1", new DwindleLayout(), WorkArea);
        var normal     = MakeWindow(1);
        var fullscreen = MakeWindow(2);
        fullscreen.IsFullscreen = true;

        ws.AddWindow(normal);
        ws.AddWindow(fullscreen);

        var layout = ws.CalculateLayout(gaps: 0);
        Assert.True(layout.ContainsKey(normal.Handle));
        Assert.False(layout.ContainsKey(fullscreen.Handle));
    }
}
