using System.Runtime.InteropServices;

namespace GuttyRL;

/// <summary>Habilita ANSI/VT no console Windows e UTF-8 de entrada/saida.</summary>
internal static partial class Vt
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;
    private const uint DisableNewlineAutoReturn = 0x0008;
    private const uint Utf8CodePage = 65001;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleCP(uint wCodePageID);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleOutputCP(uint wCodePageID);

    public static bool Enable()
    {
        try
        {
            try { SetConsoleCP(Utf8CodePage); SetConsoleOutputCP(Utf8CodePage); } catch { }

            nint h = GetStdHandle(StdOutputHandle);
            if (h == 0 || h == -1) return false;
            if (!GetConsoleMode(h, out uint mode)) return false;
            uint vt = mode | EnableVirtualTerminalProcessing | DisableNewlineAutoReturn;
            return SetConsoleMode(h, vt);
        }
        catch { return false; }
    }
}
