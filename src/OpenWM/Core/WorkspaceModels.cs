using OpenWM.Layout;

namespace OpenWM.Core;

public sealed class Workspace
{
    public int Id { get; }
    public LayoutKind Layout { get; set; }
    public List<WindowInfo> Windows { get; } = new();

    public Workspace(int id, LayoutKind defaultLayout)
    {
        Id = id;
        Layout = defaultLayout;
    }
}

public sealed class VirtualDesktop
{
    public string Name { get; }
    public DateTime CreatedAtUtc { get; } = DateTime.UtcNow;

    public VirtualDesktop(string name)
    {
        Name = string.IsNullOrWhiteSpace(name) ? "OpenWM Desktop" : name.Trim();
    }
}

public interface IWorkspaceManager
{
    Workspace Active { get; }
    IReadOnlyList<Workspace> All { get; }
    void SetActive(int id);
    void UpsertWindow(WindowInfo window);
    void RemoveWindow(WindowHandle handle);
    bool MoveToWorkspace(WindowHandle handle, int workspaceId);
    bool PromoteToMaster(WindowHandle handle);
    WindowInfo? FocusedWindow();
    bool Focus(WindowHandle handle);
    bool FocusNext();
    bool FocusPrevious();
}

public sealed class WorkspaceManager : IWorkspaceManager
{
    private readonly List<Workspace> _workspaces;
    private readonly Dictionary<WindowHandle, int> _windowIndex = new();
    private readonly Dictionary<int, WindowHandle> _focusedByWorkspace = new();
    private int _active = 1;
    private WindowHandle _focused = new(0);

    public WorkspaceManager(int workspaceCount, LayoutKind defaultLayout)
    {
        _workspaces = Enumerable.Range(1, Math.Max(1, workspaceCount))
            .Select(i => new Workspace(i, defaultLayout))
            .ToList();
    }

    public Workspace Active => _workspaces[_active - 1];
    public IReadOnlyList<Workspace> All => _workspaces;

    public void SetActive(int id)
    {
        if (id < 1 || id > _workspaces.Count)
        {
            return;
        }

        _active = id;
        if (_focusedByWorkspace.TryGetValue(id, out var remembered)
            && Active.Windows.Any(w => w.Handle == remembered))
        {
            _focused = remembered;
            return;
        }

        _focused = Active.Windows.FirstOrDefault()?.Handle ?? new WindowHandle(0);
        if (_focused.IsValid)
        {
            _focusedByWorkspace[id] = _focused;
        }
    }

    public void UpsertWindow(WindowInfo window)
    {
        if (_windowIndex.TryGetValue(window.Handle, out var wsId))
        {
            var ws = _workspaces[wsId - 1];
            var idx = ws.Windows.FindIndex(w => w.Handle == window.Handle);
            if (idx >= 0)
            {
                var existing = ws.Windows[idx];
                window.IsFloating = existing.IsFloating || window.IsFloating;
                window.IsFullscreen = existing.IsFullscreen || window.IsFullscreen;
                window.IsManaged = existing.IsManaged;
                ws.Windows[idx] = window;
            }
            return;
        }

        Active.Windows.Add(window);
        _windowIndex[window.Handle] = Active.Id;
        SetFocused(window.Handle);
    }

    public void RemoveWindow(WindowHandle handle)
    {
        if (!_windowIndex.TryGetValue(handle, out var wsId))
        {
            return;
        }

        var ws = _workspaces[wsId - 1];
        ws.Windows.RemoveAll(w => w.Handle == handle);
        _windowIndex.Remove(handle);

        if (_focused == handle)
        {
            _focused = ws.Windows.FirstOrDefault()?.Handle ?? new WindowHandle(0);
            if (_active == wsId)
            {
                if (_focused.IsValid)
                {
                    _focusedByWorkspace[wsId] = _focused;
                }
                else
                {
                    _focusedByWorkspace.Remove(wsId);
                }
            }
        }

        if (_focusedByWorkspace.TryGetValue(wsId, out var remembered) && remembered == handle)
        {
            var next = ws.Windows.FirstOrDefault()?.Handle ?? new WindowHandle(0);
            if (next.IsValid)
            {
                _focusedByWorkspace[wsId] = next;
            }
            else
            {
                _focusedByWorkspace.Remove(wsId);
            }
        }
    }

    public bool MoveToWorkspace(WindowHandle handle, int workspaceId)
    {
        if (workspaceId < 1 || workspaceId > _workspaces.Count)
        {
            return false;
        }

        if (!_windowIndex.TryGetValue(handle, out var fromId))
        {
            return false;
        }

        if (fromId == workspaceId)
        {
            return true;
        }

        var from = _workspaces[fromId - 1];
        var to = _workspaces[workspaceId - 1];
        var win = from.Windows.FirstOrDefault(w => w.Handle == handle);
        if (win is null)
        {
            return false;
        }

        from.Windows.Remove(win);
        to.Windows.Add(win);
        _windowIndex[handle] = workspaceId;

        if (_focused == handle && _active == fromId)
        {
            _focused = from.Windows.FirstOrDefault()?.Handle ?? new WindowHandle(0);
            if (_focused.IsValid)
            {
                _focusedByWorkspace[fromId] = _focused;
            }
            else
            {
                _focusedByWorkspace.Remove(fromId);
            }
        }

        if (!_focusedByWorkspace.ContainsKey(workspaceId))
        {
            _focusedByWorkspace[workspaceId] = handle;
        }

        return true;
    }

    public bool PromoteToMaster(WindowHandle handle)
    {
        var ws = Active;
        var idx = ws.Windows.FindIndex(w => w.Handle == handle);
        if (idx <= 0)
        {
            return idx == 0;
        }

        var selected = ws.Windows[idx];
        ws.Windows.RemoveAt(idx);
        ws.Windows.Insert(0, selected);
        SetFocused(selected.Handle);
        return true;
    }

    public WindowInfo? FocusedWindow()
    {
        if (!_focused.IsValid)
        {
            return Active.Windows.FirstOrDefault();
        }

        return Active.Windows.FirstOrDefault(w => w.Handle == _focused);
    }

    public bool Focus(WindowHandle handle)
    {
        if (!handle.IsValid)
        {
            return false;
        }

        if (!Active.Windows.Any(w => w.Handle == handle))
        {
            return false;
        }

        SetFocused(handle);
        return true;
    }

    public bool FocusNext() => FocusOffset(+1);

    public bool FocusPrevious() => FocusOffset(-1);

    private bool FocusOffset(int delta)
    {
        var list = Active.Windows;
        if (list.Count == 0)
        {
            return false;
        }

        var current = list.FindIndex(w => w.Handle == _focused);
        if (current < 0)
        {
            current = 0;
        }

        var next = (current + delta + list.Count) % list.Count;
        SetFocused(list[next].Handle);
        return true;
    }

    private void SetFocused(WindowHandle handle)
    {
        _focused = handle;
        if (handle.IsValid)
        {
            _focusedByWorkspace[_active] = handle;
        }
    }
}
