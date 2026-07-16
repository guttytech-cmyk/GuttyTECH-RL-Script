using System.Runtime.InteropServices;

namespace GuttyQuickChat;

internal static class RocketLeagueWindow
{
    private const int SwRestore = 9;
    private const byte VkMenu = 0x12;
    private const uint KeyeventfKeyup = 0x0002;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static IntPtr Find()
    {
        foreach (var proc in System.Diagnostics.Process.GetProcessesByName("RocketLeague"))
        {
            try
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                    return proc.MainWindowHandle;
            }
            catch
            {
                // ignore
            }
        }

        return IntPtr.Zero;
    }

    public static bool Focus(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
            return false;

        if (IsIconic(hwnd))
            ShowWindow(hwnd, SwRestore);

        var fg = NativeInput.GetForegroundWindowHandle();
        if (fg == hwnd)
            return true;

        var fgThread = GetWindowThreadProcessId(fg, out _);
        var targetThread = GetWindowThreadProcessId(hwnd, out _);
        var currentThread = GetCurrentThreadId();

        AttachThreadInput(currentThread, fgThread, true);
        AttachThreadInput(currentThread, targetThread, true);

        keybd_event(VkMenu, 0, 0, UIntPtr.Zero);
        keybd_event(VkMenu, 0, KeyeventfKeyup, UIntPtr.Zero);
        var ok = SetForegroundWindow(hwnd);

        AttachThreadInput(currentThread, targetThread, false);
        AttachThreadInput(currentThread, fgThread, false);

        return ok;
    }
}
