using System.Text.Json;
using System.Text.Json.Serialization;
using OpenWM.Native;

namespace OpenWM.Config;

/// <summary>
/// OpenWM configuration — loaded from <c>%APPDATA%\OpenWM\openwm.json</c>.
/// All values have sensible defaults so a missing file works out of the box.
/// </summary>
public sealed class Configuration
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OpenWM", "openwm.json");

    // ── Layout ──────────────────────────────────────────────────────────────
    /// <summary>Default layout: "dwindle" | "master" | "floating".</summary>
    public string DefaultLayout { get; set; } = "dwindle";

    /// <summary>Gap between windows and screen edges (pixels).</summary>
    public int Gaps { get; set; } = 8;

    /// <summary>Number of virtual workspaces.</summary>
    public int WorkspaceCount { get; set; } = 9;

    // ── Master layout ────────────────────────────────────────────────────────
    /// <summary>Fraction of width given to the master window (master layout only).</summary>
    public double MasterRatio { get; set; } = 0.55;

    // ── Hotkeys ──────────────────────────────────────────────────────────────
    /// <summary>Modifier key(s) for all OpenWM hotkeys.  Bitmask of MOD_* constants.</summary>
    public uint ModifierKey { get; set; } = NativeMethods.MOD_WIN | NativeMethods.MOD_CONTROL;

    // ── Autostart ────────────────────────────────────────────────────────────
    /// <summary>Programs to start when OpenWM launches.</summary>
    public List<string> Autostart { get; set; } = new();

    // ── Window rules ─────────────────────────────────────────────────────────
    /// <summary>Windows matching these class names will always be floating.</summary>
    public List<string> FloatClasses { get; set; } = new()
    {
        "notepad",
        "#32770",  // Common dialog boxes
    };

    // ─────────────────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static Configuration Load(string? path = null)
    {
        path ??= DefaultPath;
        if (!File.Exists(path)) return new Configuration();
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Configuration>(json, _jsonOptions) ?? new Configuration();
        }
        catch
        {
            return new Configuration();
        }
    }

    public void Save(string? path = null)
    {
        path ??= DefaultPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(this, _jsonOptions);
        File.WriteAllText(path, json);
    }
}
