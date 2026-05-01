using OpenWM.Layout;

namespace OpenWM.Configuration;

public sealed class OpenWMOptions
{
    public int WorkspaceCount { get; set; } = 9;
    public int Gaps { get; set; } = 8;
    public double MasterRatio { get; set; } = 0.55;
    public int PollIntervalMs { get; set; } = 120;
    public LayoutKind DefaultLayout { get; set; } = LayoutKind.Dwindle;
    public List<string> FloatClasses { get; set; } = new() { "#32770", "OperationStatusWindow" };
    public VirtualDesktopOptions VirtualDesktop { get; set; } = new();
    public List<HotkeyBinding> Hotkeys { get; set; } = HotkeyBinding.Default();
}

public sealed class VirtualDesktopOptions
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "OpenWM Desktop";
    public bool LaunchStartupCommands { get; set; }
    public List<string> StartupCommands { get; set; } = new();
}

public sealed class HotkeyBinding
{
    public required string Chord { get; init; }
    public required string Action { get; init; }

    public static List<HotkeyBinding> Default() =>
    [
        new() { Chord = "Win+Ctrl+Q", Action = "quit" },
        new() { Chord = "Win+Ctrl+F", Action = "toggle-floating" },
        new() { Chord = "Win+Ctrl+Space", Action = "toggle-fullscreen" },
        new() { Chord = "Win+Ctrl+T", Action = "retile" },
        new() { Chord = "Win+Ctrl+L", Action = "cycle-layout" },
        new() { Chord = "Win+Ctrl+W", Action = "close-focused" },
        new() { Chord = "Win+Ctrl+M", Action = "promote-master" },
        new() { Chord = "Win+Ctrl+Left", Action = "focus-prev" },
        new() { Chord = "Win+Ctrl+Right", Action = "focus-next" },
        new() { Chord = "Win+Ctrl+PageUp", Action = "ws-prev" },
        new() { Chord = "Win+Ctrl+PageDown", Action = "ws-next" },
        new() { Chord = "Win+Alt+H", Action = "focus-left" },
        new() { Chord = "Win+Alt+L", Action = "focus-right" },
        new() { Chord = "Win+Alt+K", Action = "focus-up" },
        new() { Chord = "Win+Alt+J", Action = "focus-down" },
        new() { Chord = "Win+Ctrl+1", Action = "ws-1" },
        new() { Chord = "Win+Ctrl+2", Action = "ws-2" },
        new() { Chord = "Win+Ctrl+3", Action = "ws-3" },
        new() { Chord = "Win+Ctrl+4", Action = "ws-4" },
        new() { Chord = "Win+Ctrl+5", Action = "ws-5" },
        new() { Chord = "Win+Ctrl+6", Action = "ws-6" },
        new() { Chord = "Win+Ctrl+7", Action = "ws-7" },
        new() { Chord = "Win+Ctrl+8", Action = "ws-8" },
        new() { Chord = "Win+Ctrl+9", Action = "ws-9" },
        new() { Chord = "Win+Ctrl+Shift+1", Action = "move-ws-1" },
        new() { Chord = "Win+Ctrl+Shift+2", Action = "move-ws-2" },
        new() { Chord = "Win+Ctrl+Shift+3", Action = "move-ws-3" },
        new() { Chord = "Win+Ctrl+Shift+4", Action = "move-ws-4" },
        new() { Chord = "Win+Ctrl+Shift+5", Action = "move-ws-5" },
        new() { Chord = "Win+Ctrl+Shift+6", Action = "move-ws-6" },
        new() { Chord = "Win+Ctrl+Shift+7", Action = "move-ws-7" },
        new() { Chord = "Win+Ctrl+Shift+8", Action = "move-ws-8" },
        new() { Chord = "Win+Ctrl+Shift+9", Action = "move-ws-9" },
        new() { Chord = "Win+Alt+Shift+1", Action = "move-follow-ws-1" },
        new() { Chord = "Win+Alt+Shift+2", Action = "move-follow-ws-2" },
        new() { Chord = "Win+Alt+Shift+3", Action = "move-follow-ws-3" },
        new() { Chord = "Win+Alt+Shift+4", Action = "move-follow-ws-4" },
        new() { Chord = "Win+Alt+Shift+5", Action = "move-follow-ws-5" },
        new() { Chord = "Win+Alt+Shift+6", Action = "move-follow-ws-6" },
        new() { Chord = "Win+Alt+Shift+7", Action = "move-follow-ws-7" },
        new() { Chord = "Win+Alt+Shift+8", Action = "move-follow-ws-8" },
        new() { Chord = "Win+Alt+Shift+9", Action = "move-follow-ws-9" },
    ];
}
