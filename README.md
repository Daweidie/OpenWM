# OpenWM
A Hyprland-inspired tiling window manager for Windows, written in C#.

## Features

- **Dwindle tiling** — the first window takes the left half; each subsequent window splits the remaining space alternately (mirrors Hyprland's default layout).
- **Master-Stack layout** — one master window on the left, remaining windows stacked on the right.
- **Floating layout** — windows are not repositioned automatically.
- **9 virtual workspaces** — switch between them or move windows across workspaces.
- **Global hotkeys** — keyboard-driven workflow, no mouse required.
- **Window rules** — automatically float windows by class name.
- **Auto-detect new windows** — WinEvent hooks keep the layout up-to-date without polling.
- **JSON configuration** — stored in `%APPDATA%\OpenWM\openwm.json`.

---

## Requirements

- Windows 10 / 11 (x64)
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

---

## Getting Started

```bash
# Clone
git clone https://github.com/Daweidie/OpenWM.git
cd OpenWM

# Build
dotnet build

# Run (from project root)
dotnet run --project src/OpenWM
```

> **Tip:** Add the built `OpenWM.exe` to your Windows startup folder so it launches automatically.

---

## Hotkeys

All hotkeys use **Win + Ctrl** as the default modifier (configurable).

| Hotkey | Action |
|---|---|
| `Win+Ctrl+Q` | Quit OpenWM |
| `Win+Ctrl+F` | Toggle floating for focused window |
| `Win+Ctrl+Space` | Toggle fullscreen for focused window |
| `Win+Ctrl+T` | Retile the active workspace |
| `Win+Ctrl+L` | Cycle layout: dwindle → master → floating |
| `Win+Ctrl+W` | Close the focused window |
| `Win+Ctrl+M` | Promote focused window to master |
| `Win+Ctrl+←` | Focus previous window |
| `Win+Ctrl+→` | Focus next window |
| `Win+Ctrl+1`…`9` | Switch to workspace 1–9 |
| `Win+Ctrl+Shift+1`…`9` | Move focused window to workspace 1–9 |

---

## Configuration

On first launch, OpenWM uses built-in defaults. To customise, create:

```
%APPDATA%\OpenWM\openwm.json
```

Example configuration:

```json
{
  "DefaultLayout": "dwindle",
  "Gaps": 8,
  "WorkspaceCount": 9,
  "MasterRatio": 0.55,
  "Autostart": [
    "C:\\Users\\you\\AppData\\Local\\Programs\\alacritty\\alacritty.exe"
  ],
  "FloatClasses": [
    "notepad",
    "#32770"
  ]
}
```

| Key | Default | Description |
|---|---|---|
| `DefaultLayout` | `"dwindle"` | Starting layout: `"dwindle"`, `"master"`, or `"floating"` |
| `Gaps` | `8` | Gap (px) between windows and screen edges |
| `WorkspaceCount` | `9` | Number of virtual workspaces |
| `MasterRatio` | `0.55` | Width fraction for master window (master layout) |
| `Autostart` | `[]` | Programs launched when OpenWM starts |
| `FloatClasses` | `["notepad","#32770"]` | Window classes that are always floating |

---

## Project Structure

```
OpenWM.sln
src/
  OpenWM/
    App.cs                    # Top-level orchestrator
    Program.cs                # Entry point
    Config/
      Configuration.cs        # JSON config
    Core/
      WindowInfo.cs           # Window data model
      WindowManager.cs        # Win32 window management
    Hooks/
      WindowEventHook.cs      # WinEvent accessibility hooks
    Hotkeys/
      HotkeyManager.cs        # Global hotkey registration
    Layout/
      ILayout.cs              # Layout interface
      DwindleLayout.cs        # Hyprland-style dwindle layout
      MasterLayout.cs         # Master-stack layout
      FloatingLayout.cs       # Floating (unmanaged) layout
    Native/
      NativeMethods.cs        # P/Invoke declarations
      NativeStructures.cs     # Win32 structs
    Workspaces/
      Workspace.cs            # Virtual desktop
      WorkspaceManager.cs     # Workspace switching & management
tests/
  OpenWM.Tests/
    Layout/
      DwindleLayoutTests.cs
      MasterLayoutTests.cs
    Workspaces/
      WorkspaceTests.cs
    Config/
      ConfigurationTests.cs
```

---

## Running Tests

```bash
dotnet test
```

---

## License

MIT

