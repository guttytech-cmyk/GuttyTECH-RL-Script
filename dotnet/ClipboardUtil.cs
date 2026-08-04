using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace GuttyRL;

/// <summary>Copia texto p/ clipboard do Windows (STA + Win32 + clip.exe + PowerShell).</summary>
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

        // GUI corre em STA; ExecuteAsync/Task.Run e MTA — OpenClipboard falha sem STA.
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA)
        {
            if (TryCopyWin32(text)) return true;
        }
        else if (TryCopyOnStaThread(text))
        {
            return true;
        }

        if (TryCopyClipExe(text)) return true;
        return TryCopyPowerShell(text);
    }

    /// <summary>
    /// Copia para o clipboard da sessão média (não elevada).
    /// App com requireAdministrator grava no clipboard admin — o Steam/user não vê.
    /// </summary>
    public static bool TryCopyUnelevated(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "GuttyTECH-clip");
            Directory.CreateDirectory(dir);
            string ps1 = Path.Combine(dir, "setclip.ps1");
            // Script curto: Set-Clipboard Unicode.
            string b64 = Convert.ToBase64String(Encoding.Unicode.GetBytes(text));
            File.WriteAllText(
                ps1,
                "$t=[Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('" + b64 + "')); Set-Clipboard -Value $t; exit 0"
                + Environment.NewLine,
                Encoding.UTF8);

            // explorer.exe inicia o .ps1 em IL médio (mesmo user, sem elevação).
            // Usamos um .cmd intermediário porque explorer associa .ps1 de forma inconsistente.
            string cmd = Path.Combine(dir, "setclip.cmd");
            File.WriteAllText(
                cmd,
                "@echo off" + Environment.NewLine
                + "powershell.exe -NoProfile -NonInteractive -STA -ExecutionPolicy Bypass -File \"" + ps1 + "\""
                + Environment.NewLine,
                Encoding.ASCII);

            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"),
                Arguments = "\"" + cmd + "\"",
                UseShellExecute = true,
            });

            // Dar tempo ao processo médio gravar o clipboard do user.
            Thread.Sleep(900);
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Clipboard unelevated: " + ex.Message);
            return false;
        }
    }

    private static bool TryCopyOnStaThread(string text)
    {
        bool ok = false;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { ok = TryCopyWin32(text); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        if (!thread.Join(5000))
            return false;
        if (error is not null)
            AppMeta.Log("Clipboard STA: " + error.Message);
        return ok;
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
        for (int i = 0; i < 12; i++)
        {
            if (!OpenClipboard(0))
            {
                Thread.Sleep(25);
                continue;
            }

            try
            {
                if (!EmptyClipboard())
                    continue;
                if (SetClipboardData(CfUnicodeText, hGlobal) == 0)
                    continue;
                // Ownership transferida: nao liberar hGlobal.
                hGlobal = 0;
                return true;
            }
            finally
            {
                CloseClipboard();
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
                // ASCII puro (flags de launch) funciona em qualquer code page.
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

    private static bool TryCopyPowerShell(string text)
    {
        try
        {
            // Ultimo recurso Unicode — Set-Clipboard na PowerShell 5+.
            string encoded = Convert.ToBase64String(Encoding.Unicode.GetBytes(text));
            string ps = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                @"WindowsPowerShell\v1.0\powershell.exe");
            if (!File.Exists(ps)) ps = "powershell.exe";

            var psi = new ProcessStartInfo
            {
                FileName = ps,
                Arguments =
                    "-NoProfile -NonInteractive -STA -Command \"Set-Clipboard -Value ([Text.Encoding]::Unicode.GetString([Convert]::FromBase64String('" +
                    encoded + "')))\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            if (!p.WaitForExit(8000)) return false;
            return p.ExitCode == 0;
        }
        catch { return false; }
    }
}
