using System.Runtime.InteropServices;

namespace GuttyRL;

/// <summary>Habilita o processamento de sequencias ANSI/VT no console do Windows (true-color).</summary>
internal static partial class Vt
{
    private const int StdOutputHandle = -11;
    private const uint EnableVirtualTerminalProcessing = 0x0004;
    private const uint DisableNewlineAutoReturn = 0x0008;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetConsoleMode(nint hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetConsoleMode(nint hConsoleHandle, uint dwMode);

    public static bool Enable()
    {
        try
        {
            nint h = GetStdHandle(StdOutputHandle);
            if (h == 0 || h == -1) return false;
            if (!GetConsoleMode(h, out uint mode)) return false;
            return SetConsoleMode(h, mode | EnableVirtualTerminalProcessing);
        }
        catch { return false; }
    }
}
