using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenWM.Configuration;
using OpenWM.Core;
using OpenWM.DesktopManager;
using OpenWM.Hotkeys;
using OpenWM.Layout;
using OpenWM.Platform;

namespace OpenWM.App;

public sealed class OpenWMApp
{
    private enum FocusDirection
    {
        Left,
        Right,
        Up,
        Down,
    }

    private readonly IWindowSystem _windowSystem;
    private readonly IHotkeyService _hotkeys;
    private readonly IWorkspaceManager _workspaces;
    private readonly VirtualDesktopManager _virtualDesktopManager;
    private readonly LayoutEngine _layoutEngine;
    private readonly IOptionsMonitor<OpenWMOptions> _options;
    private readonly ILogger<OpenWMApp> _logger;
    private readonly Dictionary<int, string> _actionByHotkeyId = new();
    private int _lastSnapshotHash;
    private IReadOnlyList<PositionedWindow> _lastActiveLayout = [];

    public OpenWMApp(
        IWindowSystem windowSystem,
        IHotkeyService hotkeys,
        IWorkspaceManager workspaces,
        VirtualDesktopManager virtualDesktopManager,
        LayoutEngine layoutEngine,
        IOptionsMonitor<OpenWMOptions> options,
        ILogger<OpenWMApp> logger)
    {
        _windowSystem = windowSystem;
        _hotkeys = hotkeys;
        _workspaces = workspaces;
        _virtualDesktopManager = virtualDesktopManager;
        _layoutEngine = layoutEngine;
        _options = options;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _hotkeys.Triggered += OnHotkey;

        RegisterHotkeys(_options.CurrentValue);
        _options.OnChange(opts => RegisterHotkeys(opts));
        _virtualDesktopManager.Initialize();

        var hotkeyTask = _hotkeys.StartAsync(cancellationToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Math.Max(50, _options.CurrentValue.PollIntervalMs)));

        _logger.LogInformation("OpenWM started with {WorkspaceCount} workspaces", _options.CurrentValue.WorkspaceCount);
        _logger.LogInformation("OpenWM runs as a background window manager and does not create a standalone UI window.");

        try
        {
            while (!cancellationToken.IsCancellationRequested && await timer.WaitForNextTickAsync(cancellationToken))
            {
                Tick();
            }
        }
        catch (OperationCanceledException)
        {
            // normal shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenWM loop failed");
        }
        finally
        {
            _hotkeys.Stop();
            await hotkeyTask;
        }
    }

    private void Tick()
    {
        var options = _options.CurrentValue;
        var snapshot = _windowSystem.SnapshotVisibleWindows();

        var hash = ComputeSnapshotHash(snapshot);
        if (hash == _lastSnapshotHash)
        {
            return;
        }

        _lastSnapshotHash = hash;

        var classRules = options.FloatClasses;

        foreach (var window in snapshot)
        {
            if (classRules.Any(c => window.ClassName.Contains(c, StringComparison.OrdinalIgnoreCase)))
            {
                window.IsFloating = true;
            }

            _workspaces.UpsertWindow(window);
        }

        var activeSet = snapshot.Select(s => s.Handle).ToHashSet();
        foreach (var ws in _workspaces.All)
        {
            foreach (var stale in ws.Windows.Where(w => !activeSet.Contains(w.Handle)).ToList())
            {
                _workspaces.RemoveWindow(stale.Handle);
            }
        }

        _virtualDesktopManager.ApplyWorkspaceIsolation();

        RetileActive();
    }

    private static int ComputeSnapshotHash(IReadOnlyList<WindowInfo> snapshot)
    {
        var hash = new HashCode();
        foreach (var win in snapshot)
        {
            hash.Add(win.Handle);
            hash.Add(win.Title);
            hash.Add(win.ClassName);
        }

        return hash.ToHashCode();
    }

    private void RegisterHotkeys(OpenWMOptions options)
    {
        _actionByHotkeyId.Clear();
        var id = 1;

        foreach (var hk in options.Hotkeys)
        {
            if (!HotkeyParser.TryParse(hk.Chord, out var chord))
            {
                _logger.LogWarning("Skip invalid hotkey chord: {Chord}", hk.Chord);
                continue;
            }

            if (_hotkeys.TryRegister(chord, id))
            {
                _actionByHotkeyId[id] = hk.Action;
                id++;
            }
        }

        _logger.LogInformation("Registered {Count} hotkeys", _actionByHotkeyId.Count);
    }

    private void OnHotkey(object? sender, int id)
    {
        if (!_actionByHotkeyId.TryGetValue(id, out var action))
        {
            return;
        }

        ExecuteAction(action);
    }

    private void ExecuteAction(string action)
    {
        try
        {
            if (action.Equals("quit", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Quit requested by hotkey");
                Environment.Exit(0);
                return;
            }

            if (action.Equals("toggle-floating", StringComparison.OrdinalIgnoreCase))
            {
                var focused = _workspaces.FocusedWindow();
                if (focused is not null)
                {
                    focused.IsFloating = !focused.IsFloating;
                    RetileActive();
                }
                return;
            }

            if (action.Equals("toggle-fullscreen", StringComparison.OrdinalIgnoreCase))
            {
                var focused = _workspaces.FocusedWindow();
                if (focused is not null)
                {
                    focused.IsFullscreen = !focused.IsFullscreen;
                    RetileActive();
                }
                return;
            }

            if (action.Equals("retile", StringComparison.OrdinalIgnoreCase))
            {
                RetileActive();
                return;
            }

            if (action.Equals("cycle-layout", StringComparison.OrdinalIgnoreCase))
            {
                var ws = _workspaces.Active;
                ws.Layout = ws.Layout switch
                {
                    LayoutKind.Dwindle => LayoutKind.Master,
                    LayoutKind.Master => LayoutKind.Dynamic,
                    LayoutKind.Dynamic => LayoutKind.Floating,
                    _ => LayoutKind.Dwindle,
                };
                RetileActive();
                return;
            }

            if (action.Equals("close-focused", StringComparison.OrdinalIgnoreCase))
            {
                var focused = _workspaces.FocusedWindow();
                if (focused is not null)
                {
                    _windowSystem.TryClose(focused.Handle);
                }
                return;
            }

            if (action.Equals("promote-master", StringComparison.OrdinalIgnoreCase))
            {
                var focused = _workspaces.FocusedWindow();
                if (focused is not null)
                {
                    _workspaces.PromoteToMaster(focused.Handle);
                    RetileActive();
                }
                return;
            }

            if (action.Equals("focus-prev", StringComparison.OrdinalIgnoreCase))
            {
                if (_workspaces.FocusPrevious())
                {
                    var focused = _workspaces.FocusedWindow();
                    if (focused is not null)
                    {
                        _windowSystem.TryFocus(focused.Handle);
                    }
                }
                return;
            }

            if (action.Equals("focus-left", StringComparison.OrdinalIgnoreCase))
            {
                TryFocusDirectional(FocusDirection.Left);
                return;
            }

            if (action.Equals("focus-right", StringComparison.OrdinalIgnoreCase))
            {
                TryFocusDirectional(FocusDirection.Right);
                return;
            }

            if (action.Equals("focus-up", StringComparison.OrdinalIgnoreCase))
            {
                TryFocusDirectional(FocusDirection.Up);
                return;
            }

            if (action.Equals("focus-down", StringComparison.OrdinalIgnoreCase))
            {
                TryFocusDirectional(FocusDirection.Down);
                return;
            }

            if (action.Equals("focus-next", StringComparison.OrdinalIgnoreCase))
            {
                if (_workspaces.FocusNext())
                {
                    var focused = _workspaces.FocusedWindow();
                    if (focused is not null)
                    {
                        _windowSystem.TryFocus(focused.Handle);
                    }
                }
                return;
            }

            if (action.Equals("ws-prev", StringComparison.OrdinalIgnoreCase))
            {
                _virtualDesktopManager.SwitchWorkspaceRelative(-1);
                RetileActive();
                return;
            }

            if (action.Equals("ws-next", StringComparison.OrdinalIgnoreCase))
            {
                _virtualDesktopManager.SwitchWorkspaceRelative(+1);
                RetileActive();
                return;
            }

            if (action.StartsWith("ws-", StringComparison.OrdinalIgnoreCase) && int.TryParse(action[3..], out var wsId))
            {
                _virtualDesktopManager.SwitchWorkspace(wsId);
                RetileActive();
                return;
            }

            if (action.StartsWith("move-ws-", StringComparison.OrdinalIgnoreCase) && int.TryParse(action[8..], out var targetWs))
            {
                if (_virtualDesktopManager.MoveFocusedToWorkspace(targetWs, follow: false))
                {
                    RetileActive();
                }
                return;
            }

            if (action.StartsWith("move-follow-ws-", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(action[15..], out var followWs))
            {
                if (_virtualDesktopManager.MoveFocusedToWorkspace(followWs, follow: true))
                {
                    RetileActive();
                }
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Action execution failed for {Action}", action);
        }
    }

    private void RetileActive()
    {
        var opts = _options.CurrentValue;
        var workArea = _windowSystem.GetPrimaryWorkArea();
        var planned = _layoutEngine.Build(_workspaces.Active, workArea, opts.Gaps, opts.MasterRatio);
        _lastActiveLayout = planned;
        _windowSystem.TryApplyLayout(planned);
    }

    private void TryFocusDirectional(FocusDirection direction)
    {
        if (_lastActiveLayout.Count == 0)
        {
            RetileActive();
        }

        if (_lastActiveLayout.Count == 0)
        {
            return;
        }

        var focused = _workspaces.FocusedWindow();
        var current = focused is null
            ? _lastActiveLayout[0]
            : _lastActiveLayout.FirstOrDefault(p => p.Window.Handle == focused.Handle) ?? _lastActiveLayout[0];

        var next = FindDirectionalTarget(current, _lastActiveLayout, direction);
        if (next is null)
        {
            return;
        }

        if (_workspaces.Focus(next.Window.Handle))
        {
            _windowSystem.TryFocus(next.Window.Handle);
        }
    }

    private static PositionedWindow? FindDirectionalTarget(
        PositionedWindow current,
        IReadOnlyList<PositionedWindow> windows,
        FocusDirection direction)
    {
        var (cx, cy) = CenterOf(current.Bounds);
        var best = default(PositionedWindow?);
        var bestScore = double.MaxValue;

        foreach (var candidate in windows)
        {
            if (candidate.Window.Handle == current.Window.Handle)
            {
                continue;
            }

            var (tx, ty) = CenterOf(candidate.Bounds);
            var dx = tx - cx;
            var dy = ty - cy;

            var passes = direction switch
            {
                FocusDirection.Left => dx < 0,
                FocusDirection.Right => dx > 0,
                FocusDirection.Up => dy < 0,
                FocusDirection.Down => dy > 0,
                _ => false,
            };

            if (!passes)
            {
                continue;
            }

            var primary = direction is FocusDirection.Left or FocusDirection.Right
                ? Math.Abs(dx)
                : Math.Abs(dy);
            var secondary = direction is FocusDirection.Left or FocusDirection.Right
                ? Math.Abs(dy)
                : Math.Abs(dx);
            var score = (primary * 1000d) + secondary;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private static (double X, double Y) CenterOf(Rect rect)
    {
        return (rect.X + (rect.Width / 2d), rect.Y + (rect.Height / 2d));
    }

    private void SwitchWorkspace(int workspaceId)
    {
        _workspaces.SetActive(workspaceId);
        RetileActive();
        var focused = _workspaces.FocusedWindow();
        if (focused is not null)
        {
            _windowSystem.TryFocus(focused.Handle);
        }
    }

    private void MoveWindowToWorkspace(int workspaceId)
    {
        var focused = _workspaces.FocusedWindow();
        if (focused is not null && _workspaces.MoveToWorkspace(focused.Handle, workspaceId))
        {
            SwitchWorkspace(workspaceId);
        }
    }
}
