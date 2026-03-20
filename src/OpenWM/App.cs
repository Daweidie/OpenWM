using OpenWM.Config;
using OpenWM.Core;
using OpenWM.Hooks;
using OpenWM.Hotkeys;
using OpenWM.Layout;
using OpenWM.Native;
using OpenWM.Workspaces;

namespace OpenWM;

/// <summary>
/// Orchestrates all OpenWM subsystems.  Create one instance and call <see cref="Run"/>.
/// </summary>
public sealed class App : IDisposable
{
    private readonly Configuration _config;
    private readonly WindowManager _wm;
    private readonly WorkspaceManager _wsm;
    private readonly WindowEventHook _hook;
    private HotkeyManager? _hotkeys;
    private IntPtr _msgHwnd;
    private bool _disposed;

    // Virtual-key codes we use for workspace switching (1–9)
    private static readonly uint[] WorkspaceKeys =
    {
        0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39 // '1'–'9'
    };

    public App(Configuration config)
    {
        _config = config;
        _wm = new WindowManager();
        _hook = new WindowEventHook();

        ILayout defaultLayout = config.DefaultLayout.ToLowerInvariant() switch
        {
            "master"   => new MasterLayout { MasterRatio = config.MasterRatio },
            "floating" => new FloatingLayout(),
            _          => new DwindleLayout(),
        };

        var workArea = GetPrimaryWorkArea();
        _wsm = new WorkspaceManager(config.WorkspaceCount, workArea, defaultLayout);
    }

    public void Run()
    {
        // Create a hidden message-only window to receive WM_HOTKEY
        _msgHwnd = CreateMessageWindow();

        // Wire up win-event hooks
        _hook.WindowCreated     += OnWindowCreated;
        _hook.WindowDestroyed   += OnWindowDestroyed;
        _hook.WindowMoveSizeEnd += OnWindowMoveSizeEnd;
        _hook.Install();

        // Register hotkeys
        _hotkeys = new HotkeyManager(_msgHwnd);
        RegisterHotkeys();

        // Initial window scan
        _wm.RefreshWindows(_wsm.ActiveWorkspace.Id);
        foreach (var (_, win) in _wm.Windows)
        {
            ApplyWindowRule(win);
            _wsm.AddWindowToActive(win, _wm, _config.Gaps);
        }

        // Launch autostart programs
        foreach (var prog in _config.Autostart)
            TryLaunch(prog);

        // Message loop
        Console.WriteLine("[OpenWM] Running.  Press Ctrl-C or Win+Ctrl+Q to quit.");
        while (NativeMethods.GetMessage(out var msg, IntPtr.Zero, 0, 0))
        {
            NativeMethods.TranslateMessage(ref msg);

            if (msg.message == NativeMethods.WM_HOTKEY)
                _hotkeys.Dispatch((int)msg.wParam);

            NativeMethods.DispatchMessage(ref msg);
        }
    }

    // ── Hotkey registration ──────────────────────────────────────────────────

    private void RegisterHotkeys()
    {
        var mod = _config.ModifierKey;

        // Win+Ctrl+Q  →  Quit
        _hotkeys!.Register(mod, 0x51 /* Q */, Quit);

        // Win+Ctrl+F  →  Toggle floating for focused window
        _hotkeys.Register(mod, 0x46 /* F */, ToggleFocusedFloating);

        // Win+Ctrl+Space  →  Toggle fullscreen for focused window
        _hotkeys.Register(mod, 0x20 /* Space */, ToggleFocusedFullscreen);

        // Win+Ctrl+T  →  Tile / re-apply layout
        _hotkeys.Register(mod, 0x54 /* T */, RetileActive);

        // Win+Ctrl+L  →  Cycle layout
        _hotkeys.Register(mod, 0x4C /* L */, CycleLayout);

        // Win+Ctrl+W  →  Close focused window
        _hotkeys.Register(mod, 0x57 /* W */, CloseFocused);

        // Win+Ctrl+M  →  Promote focused window to master
        _hotkeys.Register(mod, 0x4D /* M */, PromoteFocused);

        // Win+Ctrl+Left/Right  →  Focus previous/next window
        _hotkeys.Register(mod, 0x25 /* Left  */, FocusPrev);
        _hotkeys.Register(mod, 0x27 /* Right */, FocusNext);

        // Win+Ctrl+1–9  →  Switch workspace
        for (int i = 0; i < Math.Min(_config.WorkspaceCount, 9); i++)
        {
            int wsId = i + 1;
            _hotkeys.Register(mod, WorkspaceKeys[i], () => SwitchWorkspace(wsId));
        }

        // Win+Ctrl+Shift+1–9  →  Move focused window to workspace
        const uint shiftMod = NativeMethods.MOD_SHIFT;
        for (int i = 0; i < Math.Min(_config.WorkspaceCount, 9); i++)
        {
            int wsId = i + 1;
            _hotkeys.Register(mod | shiftMod, WorkspaceKeys[i], () => MoveFocusedToWorkspace(wsId));
        }
    }

    // ── Hotkey handlers ──────────────────────────────────────────────────────

    private void Quit()
    {
        Console.WriteLine("[OpenWM] Quit hotkey pressed.");
        NativeMethods.PostQuitMessage(0);
    }

    private void ToggleFocusedFloating()
    {
        var win = _wm.GetFocusedWindow();
        if (win == null) return;
        win.IsFloating = !win.IsFloating;
        Console.WriteLine($"[OpenWM] Toggle floating: {win}");
        RetileActive();
    }

    private void ToggleFocusedFullscreen()
    {
        var win = _wm.GetFocusedWindow();
        if (win == null) return;
        var workArea = WindowManager.GetMonitorWorkArea(win.Handle);
        _wm.ToggleFullscreen(win, workArea);
        Console.WriteLine($"[OpenWM] Toggle fullscreen: {win}");
    }

    private void RetileActive()
    {
        WorkspaceManager.ApplyLayout(_wsm.ActiveWorkspace, _wm, _config.Gaps);
    }

    private void CycleLayout()
    {
        var ws = _wsm.ActiveWorkspace;
        ws.Layout = ws.Layout switch
        {
            DwindleLayout  => new MasterLayout { MasterRatio = _config.MasterRatio },
            MasterLayout   => new FloatingLayout(),
            FloatingLayout => new DwindleLayout(),
            _              => new DwindleLayout(),
        };
        Console.WriteLine($"[OpenWM] Layout changed to: {ws.Layout.Name}");
        RetileActive();
    }

    private void CloseFocused()
    {
        var win = _wm.GetFocusedWindow();
        if (win == null) return;
        Console.WriteLine($"[OpenWM] Close: {win}");
        _wm.CloseWindow(win);
    }

    private void PromoteFocused()
    {
        var win = _wm.GetFocusedWindow();
        if (win == null) return;
        _wsm.ActiveWorkspace.PromoteWindow(win);
        RetileActive();
    }

    private void FocusNext()
    {
        var wins = _wsm.ActiveWorkspace.Windows
            .Where(w => !w.IsFloating)
            .ToList();
        if (wins.Count == 0) return;
        var focused = _wm.GetFocusedWindow();
        int idx = focused != null ? wins.IndexOf(focused) : -1;
        int next = (idx + 1) % wins.Count;
        _wm.FocusWindow(wins[next]);
    }

    private void FocusPrev()
    {
        var wins = _wsm.ActiveWorkspace.Windows
            .Where(w => !w.IsFloating)
            .ToList();
        if (wins.Count == 0) return;
        var focused = _wm.GetFocusedWindow();
        int idx = focused != null ? wins.IndexOf(focused) : 0;
        int prev = (idx - 1 + wins.Count) % wins.Count;
        _wm.FocusWindow(wins[prev]);
    }

    private void SwitchWorkspace(int wsId)
    {
        Console.WriteLine($"[OpenWM] Switch to workspace {wsId}");
        _wsm.SwitchTo(wsId, _wm, _config.Gaps);
    }

    private void MoveFocusedToWorkspace(int wsId)
    {
        var win = _wm.GetFocusedWindow();
        if (win == null) return;
        Console.WriteLine($"[OpenWM] Move {win} to workspace {wsId}");
        _wsm.MoveWindowToWorkspace(win, wsId, _wm, _config.Gaps);
    }

    // ── Win-event callbacks ──────────────────────────────────────────────────

    private void OnWindowCreated(object? sender, IntPtr hwnd)
    {
        if (_wm.GetWindow(hwnd) != null) return;
        var info = _wm.TrackWindow(hwnd, _wsm.ActiveWorkspace.Id);
        if (info == null) return;
        ApplyWindowRule(info);
        _wsm.AddWindowToActive(info, _wm, _config.Gaps);
    }

    private void OnWindowDestroyed(object? sender, IntPtr hwnd)
    {
        var info = _wm.GetWindow(hwnd);
        if (info == null) return;
        _wsm.RemoveWindow(info, _wm, _config.Gaps);
        _wm.UntrackWindow(hwnd);
    }

    private void OnWindowMoveSizeEnd(object? sender, IntPtr hwnd)
    {
        var info = _wm.GetWindow(hwnd);
        if (info == null) return;
        // When user manually moves a tiled window, promote it to floating.
        if (!info.IsFloating)
        {
            info.IsFloating = true;
            Console.WriteLine($"[OpenWM] Auto-float after move: {info}");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void ApplyWindowRule(WindowInfo win)
    {
        foreach (var cls in _config.FloatClasses)
        {
            if (win.ClassName.Equals(cls, StringComparison.OrdinalIgnoreCase) ||
                win.Title.Contains(cls, StringComparison.OrdinalIgnoreCase))
            {
                win.IsFloating = true;
                return;
            }
        }
    }

    private static RECT GetPrimaryWorkArea()
    {
        var r = default(RECT);
        NativeMethods.SystemParametersInfo(NativeMethods.SPI_GETWORKAREA, 0, ref r, 0);
        return r;
    }

    private static IntPtr CreateMessageWindow()
    {
        const string ClassName = "OpenWM_MessageWindow";
        var wndClass = new WNDCLASS
        {
            lpszClassName = ClassName,
            hInstance     = NativeMethods.GetModuleHandle(null),
            lpfnWndProc   = static (h, m, w, l) => NativeMethods.DefWindowProc(h, m, w, l),
        };
        NativeMethods.RegisterClass(ref wndClass);
        return NativeMethods.CreateWindowEx(
            0, ClassName, "OpenWM", 0,
            0, 0, 0, 0,
            IntPtr.Zero, IntPtr.Zero,
            NativeMethods.GetModuleHandle(null), IntPtr.Zero);
    }

    private static void TryLaunch(string program)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = program,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpenWM] Failed to launch '{program}': {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _hotkeys?.Dispose();
            _hook.Dispose();
            _wm.Dispose();
            _wsm.Dispose();
            _disposed = true;
        }
    }
}
