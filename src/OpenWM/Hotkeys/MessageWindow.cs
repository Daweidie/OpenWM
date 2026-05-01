using OpenWM.Native;

namespace OpenWM.Hotkeys;

internal static class MessageWindow
{
    private const string ClassName = "OpenWM.MessageOnlyWindow";

    public static nint Create()
    {
        var wc = new WNDCLASS
        {
            lpszClassName = ClassName,
            lpfnWndProc = DefProc,
            hInstance = NativeMethods.GetModuleHandle(null),
        };

        _ = NativeMethods.RegisterClass(ref wc);

        return NativeMethods.CreateWindowEx(
            0,
            ClassName,
            "OpenWM",
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            wc.hInstance,
            0);
    }

    private static nint DefProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }
}
