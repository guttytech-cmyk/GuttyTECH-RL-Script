using System.Runtime.InteropServices;

namespace GuttyQuickChat;

/// <summary>Janela oculta para WM_HOTKEY (RegisterHotKey exige HWND valido).</summary>
internal sealed class HiddenWindow : IDisposable
{
    private const uint WmHotkey = 0x0312;
    private static readonly IntPtr HwndMessage = new(-3);

    private readonly string _className;
    private readonly WndProc _wndProc;
    private readonly Action<int> _onHotKey;
    private IntPtr _hwnd;
    private bool _registered;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WndClass
    {
        public uint Style;
        public WndProc LpfnWndProc;
        public int CbClsExtra;
        public int CbWndExtra;
        public IntPtr HInstance;
        public IntPtr HIcon;
        public IntPtr HCursor;
        public IntPtr HbrBackground;
        public string? LpszMenuName;
        public string LpszClassName;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassW(ref WndClass lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowExW(
        int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent,
        IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClassW(string lpClassName, IntPtr hInstance);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandleW(string? lpModuleName);

    public IntPtr Handle => _hwnd;

    public HiddenWindow(Action<int> onHotKey)
    {
        _onHotKey = onHotKey;
        _wndProc = WindowProc;
        _className = $"GuttyQC_{Environment.ProcessId}";
        var hInstance = GetModuleHandleW(null);

        var wc = new WndClass
        {
            LpfnWndProc = _wndProc,
            HInstance = hInstance,
            LpszClassName = _className
        };

        var atom = RegisterClassW(ref wc);
        if (atom == 0 && Marshal.GetLastWin32Error() != 1410)
            throw new InvalidOperationException($"RegisterClass falhou: {Marshal.GetLastWin32Error()}");

        _registered = atom != 0;

        _hwnd = CreateWindowExW(0, _className, "GuttyQuickChat", 0, 0, 0, 0, 0, HwndMessage, IntPtr.Zero, hInstance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"CreateWindowEx falhou: {Marshal.GetLastWin32Error()}");
    }

    private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WmHotkey)
        {
            _onHotKey((int)wParam);
            return IntPtr.Zero;
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_hwnd != IntPtr.Zero)
        {
            DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }

        if (_registered)
        {
            UnregisterClassW(_className, GetModuleHandleW(null));
            _registered = false;
        }
    }
}
