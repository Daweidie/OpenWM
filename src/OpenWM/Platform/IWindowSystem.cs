using OpenWM.Core;

namespace OpenWM.Platform;

public interface IWindowSystem
{
    bool SupportsWindowVisibilityControl { get; }
    Rect GetPrimaryWorkArea();
    IReadOnlyList<WindowInfo> SnapshotVisibleWindows();
    bool TryApplyLayout(IReadOnlyList<PositionedWindow> layout);
    bool TrySetWindowVisibility(WindowHandle handle, bool visible);
    bool TryLaunchProcess(string command);
    bool TryFocus(WindowHandle handle);
    bool TryClose(WindowHandle handle);
}
