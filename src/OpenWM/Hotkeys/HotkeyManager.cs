using OpenWM.Native;

namespace OpenWM.Hotkeys;

/// <summary>
/// Manages global hotkey registration and dispatching using Win32 RegisterHotKey.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private readonly IntPtr _hwnd;
    private readonly Dictionary<int, Action> _handlers = new();
    private int _nextId = 1;
    private bool _disposed;

    public HotkeyManager(IntPtr messageWindowHandle)
    {
        _hwnd = messageWindowHandle;
    }

    /// <summary>Register a global hotkey. Returns the assigned hotkey id.</summary>
    public int Register(uint modifiers, uint virtualKey, Action handler)
    {
        int id = _nextId++;
        if (!NativeMethods.RegisterHotKey(_hwnd, id, modifiers | NativeMethods.MOD_NOREPEAT, virtualKey))
            throw new InvalidOperationException(
                $"Failed to register hotkey (mod=0x{modifiers:X}, vk=0x{virtualKey:X}). " +
                $"Win32 error: {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
        _handlers[id] = handler;
        return id;
    }

    /// <summary>Unregister all hotkeys.</summary>
    public void UnregisterAll()
    {
        foreach (var id in _handlers.Keys)
            NativeMethods.UnregisterHotKey(_hwnd, id);
        _handlers.Clear();
    }

    /// <summary>
    /// Dispatch a WM_HOTKEY message.  Call this from your message loop when
    /// <c>msg.message == NativeMethods.WM_HOTKEY</c>.
    /// </summary>
    public void Dispatch(int hotkeyId)
    {
        if (_handlers.TryGetValue(hotkeyId, out var handler))
            handler();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            UnregisterAll();
            _disposed = true;
        }
    }
}
