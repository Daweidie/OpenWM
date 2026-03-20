using OpenWM;
using OpenWM.Config;

// OpenWM — A Hyprland-inspired tiling window manager for Windows
// Usage: run OpenWM.exe (preferably at startup)

// Handle Ctrl+C gracefully
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    OpenWM.Native.NativeMethods.PostQuitMessage(0);
};

var config = Configuration.Load();

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║           OpenWM — Hyprland-like Window Manager      ║");
Console.WriteLine("║                   for Windows (C#)                   ║");
Console.WriteLine("╠══════════════════════════════════════════════════════╣");
Console.WriteLine($"║  Layout     : {config.DefaultLayout,-38}║");
Console.WriteLine($"║  Gaps       : {config.Gaps,-38}║");
Console.WriteLine($"║  Workspaces : {config.WorkspaceCount,-38}║");
Console.WriteLine("╠══════════════════════════════════════════════════════╣");
Console.WriteLine("║  Hotkeys  (Win+Ctrl+...)                             ║");
Console.WriteLine("║   Q      Quit                                        ║");
Console.WriteLine("║   F      Toggle floating for focused window          ║");
Console.WriteLine("║   Space  Toggle fullscreen for focused window        ║");
Console.WriteLine("║   T      Retile active workspace                     ║");
Console.WriteLine("║   L      Cycle layout (dwindle→master→floating)     ║");
Console.WriteLine("║   W      Close focused window                        ║");
Console.WriteLine("║   M      Promote focused window to master            ║");
Console.WriteLine("║   ←/→    Focus previous/next window                 ║");
Console.WriteLine("║   1-9    Switch to workspace 1-9                     ║");
Console.WriteLine("║   ⇧+1-9  Move focused window to workspace 1-9       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

using var app = new App(config);
app.Run();

