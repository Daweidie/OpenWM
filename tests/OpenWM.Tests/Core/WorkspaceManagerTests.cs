using OpenWM.Core;
using OpenWM.Layout;

namespace OpenWM.Tests.Core;

public class WorkspaceManagerTests
{
    [Fact]
    public void MoveToWorkspace_MovesWindowAcrossWorkspaces()
    {
        var wm = new WorkspaceManager(3, LayoutKind.Dwindle);
        var win = new WindowInfo(new WindowHandle(1), "A", "X");
        wm.UpsertWindow(win);

        var moved = wm.MoveToWorkspace(win.Handle, 2);

        Assert.True(moved);
        Assert.Empty(wm.All[0].Windows);
        Assert.Single(wm.All[1].Windows);
    }

    [Fact]
    public void PromoteToMaster_MovesFocusedWindowToFront()
    {
        var wm = new WorkspaceManager(1, LayoutKind.Dwindle);
        var a = new WindowInfo(new WindowHandle(1), "A", "X");
        var b = new WindowInfo(new WindowHandle(2), "B", "X");
        wm.UpsertWindow(a);
        wm.UpsertWindow(b);

        var ok = wm.PromoteToMaster(b.Handle);

        Assert.True(ok);
        Assert.Equal(b.Handle, wm.Active.Windows[0].Handle);
    }
}
