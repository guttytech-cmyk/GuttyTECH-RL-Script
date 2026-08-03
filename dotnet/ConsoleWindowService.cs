using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GuttyRL;

/// <summary>Oculta somente o console da inicialização gráfica; o CLI permanece intacto.</summary>
[SupportedOSPlatform("windows")]
internal static class ConsoleWindowService
{
    private const int SwHide = 0;
    private const uint AttachParentProcess = 0xFFFFFFFF;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GetConsoleWindow();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AllocConsole();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(nint hWnd, int nCmdShow);

    public static bool IsHidden { get; private set; }

    public static void Hide()
    {
        try
        {
            nint handle = GetConsoleWindow();
            if (handle == 0)
                return;

            ShowWindow(handle, SwHide);
            IsHidden = true;
        }
        catch
        {
            IsHidden = false;
        }
    }

    public static void PrepareForCli(bool createWhenMissing)
    {
        try
        {
            if (GetConsoleWindow() == 0
                && !AttachConsole(AttachParentProcess)
                && createWhenMissing)
            {
                AllocConsole();
            }

            if (GetConsoleWindow() != 0)
                RebindStandardStreams();
        }
        catch { }
    }

    private static void RebindStandardStreams()
    {
        try
        {
            var output = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false))
            {
                AutoFlush = true,
            };
            var error = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false))
            {
                AutoFlush = true,
            };
            Console.SetOut(output);
            Console.SetError(error);
            Console.SetIn(new StreamReader(Console.OpenStandardInput(), Encoding.UTF8));
        }
        catch { }
    }
}
