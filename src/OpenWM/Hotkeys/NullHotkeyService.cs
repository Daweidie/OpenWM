namespace OpenWM.Hotkeys;

public sealed class NullHotkeyService : IHotkeyService
{
    public event EventHandler<int>? Triggered;

    public bool TryRegister(HotkeyChord chord, int id) => false;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Stop()
    {
    }

    public void Dispose()
    {
    }
}
