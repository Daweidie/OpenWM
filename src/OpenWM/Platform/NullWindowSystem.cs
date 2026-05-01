using OpenWM.Core;

namespace OpenWM.Platform;

public sealed class NullWindowSystem : IWindowSystem
{
    public bool SupportsWindowVisibilityControl => false;

    public Rect GetPrimaryWorkArea() => new(0, 0, 1920, 1080);

    public IReadOnlyList<WindowInfo> SnapshotVisibleWindows() => [];

    public bool TryApplyLayout(IReadOnlyList<PositionedWindow> layout) => true;

    public bool TrySetWindowVisibility(WindowHandle handle, bool visible) => false;

    public bool TryLaunchProcess(string command) => false;

    public bool TryFocus(WindowHandle handle) => false;

    public bool TryClose(WindowHandle handle) => false;
}
