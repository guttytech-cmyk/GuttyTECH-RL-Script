using System.Runtime.InteropServices;

namespace GuttyQuickChat;

internal static class NativeInput
{
    [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterHotKey")]
    private static extern bool RegisterHotKeyNative(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true, EntryPoint = "UnregisterHotKey")]
    private static extern bool UnregisterHotKeyNative(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public static bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, ushort vk) =>
        RegisterHotKeyNative(hwnd, id, modifiers, vk);

    public static void UnregisterHotKey(IntPtr hwnd, int id) =>
        UnregisterHotKeyNative(hwnd, id);

    private static readonly Dictionary<string, ushort> UeKeyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["T"] = 0x54, ["Y"] = 0x59, ["U"] = 0x55,
        ["One"] = 0x31, ["Two"] = 0x32, ["Three"] = 0x33, ["Four"] = 0x34,
        ["D1"] = 0x31, ["D2"] = 0x32, ["D3"] = 0x33, ["D4"] = 0x34,
        ["D5"] = 0x35, ["D6"] = 0x36, ["D7"] = 0x37, ["D8"] = 0x38, ["D9"] = 0x39,
        ["Five"] = 0x35, ["Six"] = 0x36, ["Seven"] = 0x37, ["Eight"] = 0x38, ["Nine"] = 0x39,
        ["LeftControl"] = 0xA2, ["RightControl"] = 0xA3,
        ["Enter"] = 0x0D, ["Return"] = 0x0D,
        ["F1"] = 0x70, ["F2"] = 0x71, ["F3"] = 0x72, ["F4"] = 0x73,
        ["F5"] = 0x74, ["F6"] = 0x75, ["F7"] = 0x76, ["F8"] = 0x77,
        ["F9"] = 0x78, ["F10"] = 0x79, ["F11"] = 0x7A, ["F12"] = 0x7B,
        ["NumPad1"] = 0x61, ["NumPad2"] = 0x62, ["NumPad3"] = 0x63, ["NumPad4"] = 0x64,
        ["NumPad5"] = 0x65, ["NumPad6"] = 0x66, ["NumPad7"] = 0x67, ["NumPad8"] = 0x68,
    };

    public static ushort ResolveVk(string keyName)
    {
        if (UeKeyMap.TryGetValue(keyName, out var vk))
            return vk;
        if (keyName.Length == 1)
            return char.ToUpperInvariant(keyName[0]);
        throw new InvalidOperationException($"Tecla desconhecida: {keyName}");
    }

    public static bool IsRocketLeagueFocused()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
            return false;
        GetWindowThreadProcessId(hwnd, out var pid);
        if (pid == 0)
            return false;
        try
        {
            using var proc = System.Diagnostics.Process.GetProcessById((int)pid);
            return proc.ProcessName.Equals("RocketLeague", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static IntPtr GetForegroundWindowHandle() => GetForegroundWindow();
}
