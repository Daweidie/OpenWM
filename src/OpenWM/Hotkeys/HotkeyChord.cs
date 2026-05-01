namespace OpenWM.Hotkeys;

public readonly record struct HotkeyChord(uint Modifiers, uint VirtualKey)
{
    public override string ToString() => $"mod=0x{Modifiers:X}, vk=0x{VirtualKey:X}";
}

public static class HotkeyParser
{
    private static readonly Dictionary<string, uint> KeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Q"] = 0x51,
        ["F"] = 0x46,
        ["SPACE"] = 0x20,
        ["T"] = 0x54,
        ["L"] = 0x4C,
        ["H"] = 0x48,
        ["J"] = 0x4A,
        ["K"] = 0x4B,
        ["W"] = 0x57,
        ["M"] = 0x4D,
        ["UP"] = 0x26,
        ["DOWN"] = 0x28,
        ["PAGEUP"] = 0x21,
        ["PAGEDOWN"] = 0x22,
        ["LEFT"] = 0x25,
        ["RIGHT"] = 0x27,
        ["1"] = 0x31,
        ["2"] = 0x32,
        ["3"] = 0x33,
        ["4"] = 0x34,
        ["5"] = 0x35,
        ["6"] = 0x36,
        ["7"] = 0x37,
        ["8"] = 0x38,
        ["9"] = 0x39,
    };

    public static bool TryParse(string raw, out HotkeyChord chord)
    {
        chord = default;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var parts = raw.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return false;
        }

        uint mod = 0;
        var key = parts[^1].ToUpperInvariant();
        foreach (var p in parts[..^1])
        {
            if (p.Equals("WIN", StringComparison.OrdinalIgnoreCase)) mod |= Native.NativeMethods.MOD_WIN;
            else if (p.Equals("CTRL", StringComparison.OrdinalIgnoreCase)) mod |= Native.NativeMethods.MOD_CONTROL;
            else if (p.Equals("SHIFT", StringComparison.OrdinalIgnoreCase)) mod |= Native.NativeMethods.MOD_SHIFT;
            else if (p.Equals("ALT", StringComparison.OrdinalIgnoreCase)) mod |= Native.NativeMethods.MOD_ALT;
        }

        if (!KeyMap.TryGetValue(key, out var vk))
        {
            return false;
        }

        chord = new HotkeyChord(mod, vk);
        return true;
    }
}
