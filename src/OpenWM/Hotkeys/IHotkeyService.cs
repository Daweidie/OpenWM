namespace OpenWM.Hotkeys;

public interface IHotkeyService : IDisposable
{
    event EventHandler<int>? Triggered;
    bool TryRegister(HotkeyChord chord, int id);
    Task StartAsync(CancellationToken cancellationToken);
    void Stop();
}
