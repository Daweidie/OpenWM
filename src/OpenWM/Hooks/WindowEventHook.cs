using OpenWM.Core;
using OpenWM.Native;

namespace OpenWM.Hooks;

/// <summary>
/// Hooks into Windows accessibility events to detect window creation,
/// destruction, and foreground changes, so the layout can be updated
/// automatically without polling.
/// </summary>
public sealed class WindowEventHook : IDisposable
{
    private readonly List<IntPtr> _hooks = new();
    private NativeMethods.WinEventDelegate? _delegate; // keep alive
    private bool _disposed;

    public event EventHandler<IntPtr>? WindowCreated;
    public event EventHandler<IntPtr>? WindowDestroyed;
    public event EventHandler<IntPtr>? WindowForeground;
    public event EventHandler<IntPtr>? WindowMoveSizeEnd;

    /// <summary>Install the WinEvent hooks. Must be called from the message-loop thread.</summary>
    public void Install()
    {
        _delegate = OnWinEvent;

        InstallHook(NativeMethods.EVENT_OBJECT_SHOW, NativeMethods.EVENT_OBJECT_HIDE);
        InstallHook(NativeMethods.EVENT_SYSTEM_FOREGROUND, NativeMethods.EVENT_SYSTEM_FOREGROUND);
        InstallHook(NativeMethods.EVENT_OBJECT_DESTROY, NativeMethods.EVENT_OBJECT_DESTROY);
        InstallHook(NativeMethods.EVENT_SYSTEM_MOVESIZEEND, NativeMethods.EVENT_SYSTEM_MOVESIZEEND);
    }

    private void InstallHook(uint eventMin, uint eventMax)
    {
        var h = NativeMethods.SetWinEventHook(
            eventMin, eventMax,
            IntPtr.Zero, _delegate!,
            0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNTHREAD);
        if (h != IntPtr.Zero)
            _hooks.Add(h);
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero) return;
        if (idObject != NativeMethods.OBJID_WINDOW) return;

        switch (eventType)
        {
            case NativeMethods.EVENT_OBJECT_SHOW:
                WindowCreated?.Invoke(this, hwnd);
                break;
            case NativeMethods.EVENT_OBJECT_DESTROY:
            case NativeMethods.EVENT_OBJECT_HIDE:
                WindowDestroyed?.Invoke(this, hwnd);
                break;
            case NativeMethods.EVENT_SYSTEM_FOREGROUND:
                WindowForeground?.Invoke(this, hwnd);
                break;
            case NativeMethods.EVENT_SYSTEM_MOVESIZEEND:
                WindowMoveSizeEnd?.Invoke(this, hwnd);
                break;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var h in _hooks)
                NativeMethods.UnhookWinEvent(h);
            _hooks.Clear();
            _disposed = true;
        }
    }
}
