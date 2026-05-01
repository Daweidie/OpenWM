using Microsoft.Extensions.Logging;
using OpenWM.Native;

namespace OpenWM.Hotkeys;

public sealed class WindowsHotkeyService : IHotkeyService
{
    private readonly ILogger<WindowsHotkeyService> _logger;
    private readonly Dictionary<int, HotkeyChord> _registered = new();
    private readonly Dictionary<int, HotkeyChord> _pending = new();
    private readonly object _sync = new();
    private nint _hwnd;
    private bool _running;

    public event EventHandler<int>? Triggered;

    public WindowsHotkeyService(ILogger<WindowsHotkeyService> logger)
    {
        _logger = logger;
    }

    public bool TryRegister(HotkeyChord chord, int id)
    {
        lock (_sync)
        {
            if (_hwnd == 0)
            {
                _pending[id] = chord;
                return true;
            }

            if (_registered.ContainsKey(id))
            {
                NativeMethods.UnregisterHotKey(_hwnd, id);
                _registered.Remove(id);
            }

            var ok = NativeMethods.RegisterHotKey(_hwnd, id, chord.Modifiers | NativeMethods.MOD_NOREPEAT, chord.VirtualKey);
            if (!ok)
            {
                var err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                if (err == 1409)
                {
                    _logger.LogWarning("Hotkey already occupied, skipping: {Chord}", chord);
                    return false;
                }

                _logger.LogError("Register hotkey failed for {Chord}, Win32={Err}", chord, err);
                return false;
            }

            _registered[id] = chord;
            return true;
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            _hwnd = MessageWindow.Create();
            foreach (var (id, chord) in _pending)
            {
                _ = TryRegister(chord, id);
            }

            _pending.Clear();
            _running = true;
        }

        await Task.Run(() =>
        {
            while (_running && NativeMethods.GetMessage(out var msg, 0, 0, 0))
            {
                if (msg.message == NativeMethods.WM_HOTKEY)
                {
                    Triggered?.Invoke(this, (int)msg.wParam);
                }

                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }
        }, cancellationToken);
    }

    public void Stop()
    {
        lock (_sync)
        {
            _running = false;
            if (_hwnd != 0)
            {
                foreach (var id in _registered.Keys)
                {
                    NativeMethods.UnregisterHotKey(_hwnd, id);
                }

                NativeMethods.PostMessage(_hwnd, NativeMethods.WM_CLOSE, 0, 0);
            }

            _registered.Clear();
            _pending.Clear();
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
