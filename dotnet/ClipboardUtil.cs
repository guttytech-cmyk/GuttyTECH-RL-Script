using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GuttyRL;

/// <summary>Copia texto p/ clipboard do Windows (Win32 + fallback clip.exe).</summary>
[SupportedOSPlatform("windows")]
internal static class ClipboardUtil
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(nint hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetClipboardData(uint uFormat, nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalAlloc(uint uFlags, nuint dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalLock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(nint hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint GlobalFree(nint hMem);

    public static bool TryCopy(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (TryCopyWin32(text)) return true;
        return TryCopyClipExe(text);
    }

    private static bool TryCopyWin32(string text)
    {
        // CF_UNICODETEXT precisa de UTF-16 LE + null terminator.
        byte[] bytes = Encoding.Unicode.GetBytes(text + "\0");
        nint hGlobal = GlobalAlloc(GmemMoveable, (nuint)bytes.Length);
        if (hGlobal == 0) return false;

        nint locked = GlobalLock(hGlobal);
        if (locked == 0)
        {
            GlobalFree(hGlobal);
            return false;
        }

        try
        {
            Marshal.Copy(bytes, 0, locked, bytes.Length);
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }

        // Retry curto: outro app pode ter o clipboard aberto.
        for (int i = 0; i < 8; i++)
        {
            if (!OpenClipboard(0))
            {
                Thread.Sleep(20);
                continue;
            }

            try
            {
                if (!EmptyClipboard())
                    return false;
                if (SetClipboardData(CfUnicodeText, hGlobal) == 0)
                    return false;
                // Ownership transferida: nao liberar hGlobal.
                hGlobal = 0;
                return true;
            }
            finally
            {
                CloseClipboard();
                if (hGlobal != 0)
                {
                    GlobalFree(hGlobal);
                    hGlobal = 0;
                }
            }
        }

        if (hGlobal != 0) GlobalFree(hGlobal);
        return false;
    }

    private static bool TryCopyClipExe(string text)
    {
        try
        {
            string clip = Path.Combine(Environment.SystemDirectory, "clip.exe");
            if (!File.Exists(clip)) clip = "clip.exe";

            var psi = new ProcessStartInfo(clip)
            {
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                // clip.exe espera OEM/ANSI no stdin redirect; ASCII puro funciona em qualquer code page.
                StandardInputEncoding = Encoding.ASCII,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.StandardInput.Write(text);
            p.StandardInput.Close();
            if (!p.WaitForExit(3000)) return false;
            return p.ExitCode == 0;
        }
        catch { return false; }
    }
}
