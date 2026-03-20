using OpenWM.Core;
using OpenWM.Layout;
using OpenWM.Native;

namespace OpenWM.Workspaces;

/// <summary>
/// Manages a collection of workspaces and switching between them.
/// Windows are hidden when their workspace is not active.
/// </summary>
public sealed class WorkspaceManager : IDisposable
{
    private readonly List<Workspace> _workspaces = new();
    private int _activeIndex;
    private bool _disposed;

    public IReadOnlyList<Workspace> Workspaces => _workspaces.AsReadOnly();
    public Workspace ActiveWorkspace => _workspaces[_activeIndex];

    public event EventHandler<Workspace>? WorkspaceChanged;

    public WorkspaceManager(int count, RECT defaultWorkArea, ILayout defaultLayout)
    {
        for (int i = 0; i < count; i++)
        {
            var layout = CloneLayout(defaultLayout);
            _workspaces.Add(new Workspace(i + 1, $"WS {i + 1}", layout, defaultWorkArea));
        }
        _activeIndex = 0;
    }

    /// <summary>Switch to workspace by 1-based id. Returns true if the switch happened.</summary>
    public bool SwitchTo(int workspaceId, WindowManager wm, int gaps)
    {
        int idx = workspaceId - 1;
        if (idx < 0 || idx >= _workspaces.Count) return false;
        if (idx == _activeIndex) return false;

        // Hide windows of current workspace
        foreach (var w in _workspaces[_activeIndex].Windows)
            NativeMethods.ShowWindow(w.Handle, NativeMethods.SW_HIDE);

        _activeIndex = idx;
        var target = _workspaces[_activeIndex];

        // Show windows of new workspace
        foreach (var w in target.Windows)
            NativeMethods.ShowWindow(w.Handle, NativeMethods.SW_SHOW);

        ApplyLayout(target, wm, gaps);
        WorkspaceChanged?.Invoke(this, target);
        return true;
    }

    /// <summary>Move a window to another workspace.</summary>
    public void MoveWindowToWorkspace(WindowInfo window, int workspaceId, WindowManager wm, int gaps)
    {
        int idx = workspaceId - 1;
        if (idx < 0 || idx >= _workspaces.Count) return;

        // Remove from current workspace
        foreach (var ws in _workspaces)
            ws.RemoveWindow(window);

        var target = _workspaces[idx];
        window.WorkspaceId = target.Id;
        target.AddWindow(window);

        if (idx != _activeIndex)
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SW_HIDE);

        // Re-tile current workspace
        ApplyLayout(ActiveWorkspace, wm, gaps);
    }

    /// <summary>Add a newly discovered window to the active workspace.</summary>
    public void AddWindowToActive(WindowInfo window, WindowManager wm, int gaps)
    {
        window.WorkspaceId = ActiveWorkspace.Id;
        ActiveWorkspace.AddWindow(window);
        ApplyLayout(ActiveWorkspace, wm, gaps);
    }

    /// <summary>Remove a window from whichever workspace owns it, then re-tile.</summary>
    public void RemoveWindow(WindowInfo window, WindowManager wm, int gaps)
    {
        foreach (var ws in _workspaces)
        {
            if (ws.RemoveWindow(window) && ws.Id == ActiveWorkspace.Id)
            {
                ApplyLayout(ActiveWorkspace, wm, gaps);
                break;
            }
        }
    }

    /// <summary>Find the workspace that contains the given window.</summary>
    public Workspace? FindWorkspace(WindowInfo window) =>
        _workspaces.FirstOrDefault(ws => ws.ContainsWindow(window));

    /// <summary>Apply the current layout for a workspace, moving all tiled windows.</summary>
    public static void ApplyLayout(Workspace workspace, WindowManager wm, int gaps)
    {
        var rects = workspace.CalculateLayout(gaps);
        foreach (var (handle, rect) in rects)
        {
            var info = wm.GetWindow(handle);
            if (info != null)
                wm.SetWindowRect(info, rect);
        }
    }

    private static ILayout CloneLayout(ILayout source) => source switch
    {
        DwindleLayout  => new DwindleLayout(),
        MasterLayout m => new MasterLayout { MasterRatio = m.MasterRatio },
        FloatingLayout => new FloatingLayout(),
        _              => new DwindleLayout(),
    };

    public void Dispose()
    {
        if (!_disposed)
        {
            _workspaces.Clear();
            _disposed = true;
        }
    }
}
