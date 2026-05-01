using OpenWM.Hotkeys;
using OpenWM.Native;

namespace OpenWM.Tests.Hotkeys;

public class HotkeyParserTests
{
    [Fact]
    public void TryParse_ValidChord_ReturnsTrue()
    {
        var ok = HotkeyParser.TryParse("Win+Ctrl+Shift+1", out var chord);

        Assert.True(ok);
        Assert.Equal((uint)(NativeMethods.MOD_WIN | NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT), chord.Modifiers);
        Assert.Equal((uint)0x31, chord.VirtualKey);
    }

    [Fact]
    public void TryParse_InvalidChord_ReturnsFalse()
    {
        var ok = HotkeyParser.TryParse("Win+Ctrl+Unknown", out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryParse_HyprlandStyleDirectionKeys_ReturnsTrue()
    {
        var okH = HotkeyParser.TryParse("Win+Alt+H", out var h);
        var okJ = HotkeyParser.TryParse("Win+Alt+J", out var j);
        var okK = HotkeyParser.TryParse("Win+Alt+K", out var k);
        var okL = HotkeyParser.TryParse("Win+Alt+L", out var l);

        Assert.True(okH);
        Assert.True(okJ);
        Assert.True(okK);
        Assert.True(okL);
        Assert.Equal((uint)0x48, h.VirtualKey);
        Assert.Equal((uint)0x4A, j.VirtualKey);
        Assert.Equal((uint)0x4B, k.VirtualKey);
        Assert.Equal((uint)0x4C, l.VirtualKey);
    }

    [Fact]
    public void TryParse_WorkspaceCyclingKeys_ReturnsTrue()
    {
        var okPrev = HotkeyParser.TryParse("Win+Ctrl+PageUp", out var prev);
        var okNext = HotkeyParser.TryParse("Win+Ctrl+PageDown", out var next);

        Assert.True(okPrev);
        Assert.True(okNext);
        Assert.Equal((uint)0x21, prev.VirtualKey);
        Assert.Equal((uint)0x22, next.VirtualKey);
    }
}
