using System.Text;
using Microsoft.Extensions.Logging;
using OpenWM.Core;
using OpenWM.Native;

namespace OpenWM.Platform;

public sealed class WindowsWindowSystem : IWindowSystem
{
    private readonly ILogger<WindowsWindowSystem> _logger;

    public bool SupportsWindowVisibilityControl => true;

    public WindowsWindowSystem(ILogger<WindowsWindowSystem> logger)
    {
        _logger = logger;
    }

    public Rect GetPrimaryWorkArea()
    {
        var native = new RECT();
        if (!NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref native, 0))
        {
            return new Rect(0, 0, 1920, 1080);
        }

        return new Rect(native.Left, native.Top, native.Right - native.Left, native.Bottom - native.Top);
    }

    public IReadOnlyList<WindowInfo> SnapshotVisibleWindows()
    {
        var list = new List<WindowInfo>();

        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (!NativeMethods.IsWindowVisible(hWnd))
            {
                return true;
            }

            var titleBuf = new StringBuilder(256);
            NativeMethods.GetWindowText(hWnd, titleBuf, titleBuf.Capacity);
            var title = titleBuf.ToString();
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            var classBuf = new StringBuilder(128);
            NativeMethods.GetClassName(hWnd, classBuf, classBuf.Capacity);
            list.Add(new WindowInfo(new WindowHandle(hWnd), title, classBuf.ToString()));
            return true;
        }, 0);

        return list;
    }

    public bool TryApplyLayout(IReadOnlyList<PositionedWindow> layout)
    {
        var ok = true;
        foreach (var item in layout)
        {
            var rect = item.Bounds;
            var moved = NativeMethods.SetWindowPos(
                item.Window.Handle.Value,
                0,
                rect.X,
                rect.Y,
                rect.Width,
                rect.Height,
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_SHOWWINDOW);

            if (!moved)
            {
                ok = false;
            }
        }

        if (!ok)
        {
            _logger.LogWarning("Some windows failed to move while applying layout.");
        }

        return ok;
    }

    public bool TrySetWindowVisibility(WindowHandle handle, bool visible)
    {
        if (!handle.IsValid)
        {
            return false;
        }

        return NativeMethods.ShowWindow(handle.Value, visible ? NativeMethods.SW_SHOWNOACTIVATE : NativeMethods.SW_HIDE);
    }

    public bool TryLaunchProcess(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = true,
                CreateNoWindow = true,
            };
            _ = System.Diagnostics.Process.Start(startInfo);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to launch command: {Command}", command);
            return false;
        }
    }

    public bool TryFocus(WindowHandle handle)
    {
        if (!handle.IsValid)
        {
            return false;
        }

        return NativeMethods.SetForegroundWindow(handle.Value);
    }

    public bool TryClose(WindowHandle handle)
    {
        if (!handle.IsValid)
        {
            return false;
        }

        return NativeMethods.PostMessage(handle.Value, NativeMethods.WM_CLOSE, 0, 0);
    }
}
