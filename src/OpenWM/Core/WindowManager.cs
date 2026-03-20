using OpenWM.Native;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenWM.Core;

/// <summary>
/// Core service that enumerates, tracks, and manipulates windows.
/// </summary>
public sealed class WindowManager : IDisposable
{
    private readonly Dictionary<IntPtr, WindowInfo> _windows = new();
    private readonly object _lock = new();
    private bool _disposed;

    // Class names that should never be managed
    private static readonly HashSet<string> IgnoredClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell_TrayWnd",        // Taskbar
        "DV2ControlHost",       // Start menu
        "MsgrIMEWindowClass",
        "SysShadow",
        "Button",
        "Windows.UI.Core.CoreWindow",
        "ForegroundStaging",
        "ApplicationManager_DesktopShellWindow",
        "Static",
        "Scrollbar",
        "Progman",              // Desktop
        "WorkerW",              // Desktop
        "SHELLDLL_DefView",
    };

    public event EventHandler<WindowInfo>? WindowAdded;
    public event EventHandler<WindowInfo>? WindowRemoved;
    public event EventHandler<WindowInfo>? WindowFocused;

    public IReadOnlyDictionary<IntPtr, WindowInfo> Windows
    {
        get
        {
            lock (_lock) { return new Dictionary<IntPtr, WindowInfo>(_windows); }
        }
    }

    /// <summary>Refresh the tracked window list by enumerating all visible, manageable windows.</summary>
    public void RefreshWindows(int currentWorkspaceId)
    {
        var found = new HashSet<IntPtr>();
        NativeMethods.EnumWindows((hWnd, _) =>
        {
            if (ShouldManage(hWnd))
            {
                found.Add(hWnd);
                lock (_lock)
                {
                    if (!_windows.ContainsKey(hWnd))
                    {
                        var info = CreateWindowInfo(hWnd, currentWorkspaceId);
                        _windows[hWnd] = info;
                        WindowAdded?.Invoke(this, info);
                    }
                }
            }
            return true;
        }, IntPtr.Zero);

        // Remove windows that no longer exist
        List<IntPtr> toRemove;
        lock (_lock)
        {
            toRemove = _windows.Keys.Where(h => !found.Contains(h)).ToList();
        }
        foreach (var h in toRemove)
        {
            WindowInfo? info;
            lock (_lock)
            {
                _windows.TryGetValue(h, out info);
                if (!_windows.Remove(h)) info = null;
            }
            if (info != null) WindowRemoved?.Invoke(this, info);
        }
    }

    /// <summary>Add or update a window by handle.</summary>
    public WindowInfo? TrackWindow(IntPtr hWnd, int workspaceId)
    {
        if (!ShouldManage(hWnd)) return null;
        lock (_lock)
        {
            if (_windows.TryGetValue(hWnd, out var existing)) return existing;
            var info = CreateWindowInfo(hWnd, workspaceId);
            _windows[hWnd] = info;
            WindowAdded?.Invoke(this, info);
            return info;
        }
    }

    /// <summary>Remove a window from tracking.</summary>
    public bool UntrackWindow(IntPtr hWnd)
    {
        WindowInfo? info;
        lock (_lock)
        {
            if (!_windows.TryGetValue(hWnd, out info)) return false;
            _windows.Remove(hWnd);
        }
        WindowRemoved?.Invoke(this, info);
        return true;
    }

    /// <summary>Get a tracked window by handle, or null if not tracked.</summary>
    public WindowInfo? GetWindow(IntPtr hWnd)
    {
        lock (_lock) { return _windows.GetValueOrDefault(hWnd); }
    }

    /// <summary>Get the currently focused window.</summary>
    public WindowInfo? GetFocusedWindow()
    {
        var hWnd = NativeMethods.GetForegroundWindow();
        lock (_lock) { return _windows.GetValueOrDefault(hWnd); }
    }

    /// <summary>Focus a specific window.</summary>
    public void FocusWindow(WindowInfo window)
    {
        if (NativeMethods.IsIconic(window.Handle))
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
        NativeMethods.SetForegroundWindow(window.Handle);
        WindowFocused?.Invoke(this, window);
    }

    /// <summary>Close a window gracefully.</summary>
    public void CloseWindow(WindowInfo window)
    {
        NativeMethods.PostMessage(window.Handle, NativeMethods.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Move and resize a window to the given rectangle, accounting for invisible borders
    /// on DWM-composited windows (Windows 10+).
    /// </summary>
    public void SetWindowRect(WindowInfo window, Native.RECT target)
    {
        if (window.IsFullscreen) return;

        var hWnd = window.Handle;
        // Extended frame bounds may differ from window rect due to invisible DWM borders.
        // Use GetWindowRect + compare to calculate border offset.
        NativeMethods.GetWindowRect(hWnd, out var currentRect);

        int x = target.Left;
        int y = target.Top;
        int w = target.Width;
        int h = target.Height;

        NativeMethods.MoveWindow(hWnd, x, y, w, h, true);
    }

    /// <summary>Toggle fullscreen for a window on the given monitor work area.</summary>
    public void ToggleFullscreen(WindowInfo window, Native.RECT workArea)
    {
        if (window.IsFullscreen)
        {
            window.IsFullscreen = false;
            SetWindowRect(window, window.SavedRect);
        }
        else
        {
            NativeMethods.GetWindowRect(window.Handle, out var current);
            window.SavedRect = current;
            window.IsFullscreen = true;
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_SHOWNORMAL);
            NativeMethods.SetWindowPos(
                window.Handle,
                NativeMethods.HWND_TOP,
                workArea.Left, workArea.Top,
                workArea.Width, workArea.Height,
                NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_SHOWWINDOW);
        }
    }

    /// <summary>Minimize a window to the taskbar.</summary>
    public void MinimizeWindow(WindowInfo window)
    {
        NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_SHOWMINIMIZED);
    }

    /// <summary>Restore a minimized window.</summary>
    public void RestoreWindow(WindowInfo window)
    {
        NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_RESTORE);
    }

    /// <summary>Get the work area of the monitor that contains the given window.</summary>
    public static Native.RECT GetMonitorWorkArea(IntPtr hWnd)
    {
        var hMonitor = NativeMethods.MonitorFromWindow(hWnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<MONITORINFO>() };
        NativeMethods.GetMonitorInfo(hMonitor, ref info);
        return info.rcWork;
    }

    /// <summary>Enumerate all monitors and return their work areas.</summary>
    public static List<Native.RECT> GetAllMonitorWorkAreas()
    {
        var areas = new List<Native.RECT>();
        var gcHandle = GCHandle.Alloc(areas);
        try
        {
            NativeMethods.EnumDisplayMonitors(
                IntPtr.Zero, IntPtr.Zero,
                MonitorEnumCallback,
                GCHandle.ToIntPtr(gcHandle));
        }
        finally
        {
            gcHandle.Free();
        }
        return areas;
    }

    private static bool MonitorEnumCallback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
    {
        var handle = GCHandle.FromIntPtr(dwData);
        var areas = (List<Native.RECT>)handle.Target!;
        var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
        NativeMethods.GetMonitorInfo(hMonitor, ref info);
        areas.Add(info.rcWork);
        return true;
    }

    // ---- helpers ----

    private static WindowInfo CreateWindowInfo(IntPtr hWnd, int workspaceId)
    {
        var titleBuf = new StringBuilder(256);
        NativeMethods.GetWindowText(hWnd, titleBuf, 256);

        var classBuf = new StringBuilder(256);
        NativeMethods.GetClassName(hWnd, classBuf, 256);

        NativeMethods.GetWindowThreadProcessId(hWnd, out var pid);

        return new WindowInfo(hWnd, titleBuf.ToString(), classBuf.ToString(), pid, workspaceId);
    }

    private static bool ShouldManage(IntPtr hWnd)
    {
        if (!NativeMethods.IsWindowVisible(hWnd)) return false;

        var classBuf = new StringBuilder(256);
        NativeMethods.GetClassName(hWnd, classBuf, 256);
        var cls = classBuf.ToString();
        if (IgnoredClasses.Contains(cls)) return false;

        long style = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_STYLE);
        if ((style & NativeMethods.WS_CHILD) != 0) return false;

        long exStyle = NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
        if ((exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0 &&
            (exStyle & NativeMethods.WS_EX_APPWINDOW) == 0) return false;

        // Must have a title (length > 0)
        var titleBuf = new StringBuilder(256);
        int titleLen = NativeMethods.GetWindowText(hWnd, titleBuf, 256);
        if (titleLen == 0) return false;

        return true;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _windows.Clear();
            _disposed = true;
        }
    }
}
