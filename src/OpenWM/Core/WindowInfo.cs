namespace OpenWM.Core;

/// <summary>
/// Represents a managed window tracked by OpenWM.
/// </summary>
public sealed class WindowInfo
{
    public IntPtr Handle { get; }
    public string Title { get; set; }
    public string ClassName { get; set; }
    public uint ProcessId { get; set; }
    public bool IsFloating { get; set; }
    public bool IsFullscreen { get; set; }
    public bool IsPseudoTiled { get; set; }

    /// <summary>The workspace this window is assigned to.</summary>
    public int WorkspaceId { get; set; }

    /// <summary>Saved geometry used when toggling fullscreen/floating.</summary>
    public Native.RECT SavedRect { get; set; }

    public WindowInfo(IntPtr handle, string title, string className, uint processId, int workspaceId)
    {
        Handle = handle;
        Title = title;
        ClassName = className;
        ProcessId = processId;
        WorkspaceId = workspaceId;
    }

    public override string ToString() => $"[0x{Handle:X}] {Title} ({ClassName}) WS={WorkspaceId}";
    public override bool Equals(object? obj) => obj is WindowInfo w && w.Handle == Handle;
    public override int GetHashCode() => Handle.GetHashCode();
}
