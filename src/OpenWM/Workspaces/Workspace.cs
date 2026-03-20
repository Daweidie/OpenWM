using OpenWM.Core;
using OpenWM.Layout;
using OpenWM.Native;

namespace OpenWM.Workspaces;

/// <summary>
/// A virtual desktop (workspace). Each workspace has its own ordered list of
/// tiled windows and an active layout.
/// </summary>
public sealed class Workspace
{
    private readonly List<WindowInfo> _windows = new();

    public int Id { get; }
    public string Name { get; set; }
    public ILayout Layout { get; set; }

    /// <summary>The monitor work area this workspace occupies.</summary>
    public RECT WorkArea { get; set; }

    public IReadOnlyList<WindowInfo> Windows => _windows.AsReadOnly();

    public Workspace(int id, string name, ILayout layout, RECT workArea)
    {
        Id = id;
        Name = name;
        Layout = layout;
        WorkArea = workArea;
    }

    public void AddWindow(WindowInfo window)
    {
        if (!_windows.Contains(window))
            _windows.Add(window);
    }

    public bool RemoveWindow(WindowInfo window) => _windows.Remove(window);

    public bool ContainsWindow(WindowInfo window) => _windows.Contains(window);

    /// <summary>Move a window one position toward the beginning of the list.</summary>
    public void PromoteWindow(WindowInfo window)
    {
        int idx = _windows.IndexOf(window);
        if (idx > 0)
        {
            _windows.RemoveAt(idx);
            _windows.Insert(idx - 1, window);
        }
    }

    /// <summary>Swap two windows by index.</summary>
    public void SwapWindows(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _windows.Count) return;
        if (indexB < 0 || indexB >= _windows.Count) return;
        (_windows[indexA], _windows[indexB]) = (_windows[indexB], _windows[indexA]);
    }

    /// <summary>Calculate tiling rects for all non-floating windows.</summary>
    public Dictionary<IntPtr, RECT> CalculateLayout(int gaps)
    {
        var tiledHandles = _windows
            .Where(w => !w.IsFloating && !w.IsFullscreen)
            .Select(w => w.Handle)
            .ToList();
        return Layout.Arrange(tiledHandles, WorkArea, gaps);
    }

    public override string ToString() => $"Workspace {Id}: {Name} ({_windows.Count} windows, layout={Layout.Name})";
}
