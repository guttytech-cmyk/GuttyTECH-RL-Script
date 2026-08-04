using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace GuttyRL;

/// <summary>Detecta e tenta corrigir bloqueio de escrita (Defender / ACL) na pasta do RL.</summary>
[SupportedOSPlatform("windows")]
internal static class FolderAccess
{
    private static readonly string[] ProtectedRoots =
    {
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
    };

    public static bool CanWriteToDirectory(string dir) => ProbeWrite(dir);

    public static bool EnsureWriteAccess(string iniPath, bool interactive)
    {
        string dir = Path.GetDirectoryName(iniPath)!;

        if (RunAutoHeal(dir, iniPath, interactive))
            return true;

        ShowManualRecoveryGuide(dir, iniPath, interactive);
        return false;
    }

    /// <summary>Modo HEAL elevado: libera Defender + ACL e testa gravacao.</summary>
    public static bool RunHealMode(string iniPath, bool interactive, bool showBanner = true)
    {
        string dir = Path.GetDirectoryName(iniPath)!;
        if (interactive && showBanner)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("DESBLOQUEANDO PASTA DO JOGO", Ui.MAmber);
        }

        if (RunAutoHeal(dir, iniPath, interactive, forceVisibleSteps: true))
        {
            if (interactive)
                Ui.CompletionMessage(Ui.OkGreen, "PASTA LIBERADA", new[]
                {
                    "Consegui gravar na pasta do jogo.",
                    "Feche esta janela e aplique o modo de novo (COMPLETO/CRIADOR)."
                });
            return true;
        }

        ShowManualRecoveryGuide(dir, iniPath, interactive);
        return false;
    }

    /// <summary>Todas as tentativas automaticas antes de pedir ajuda manual.</summary>
    private static bool RunAutoHeal(string dir, string iniPath, bool interactive, bool forceVisibleSteps = false)
    {
        if (ProbeWrite(dir))
            return true;

        // 1) Silencioso: ACL + Defender allow (se ja for admin)
        if (Heal(dir, iniPath))
            return true;

        if (interactive || forceVisibleSteps)
        {
            // 2) Com feedback na tela
            if (Ui.StepAnimated("Desbloqueando pasta do jogo", () => { UnlockDirectory(dir); UnlockFile(iniPath); return true; })
                && Ui.StepAnimated("Testando gravacao", () => ProbeWrite(dir)))
                return true;

            if (Ui.StepAnimated("Liberando no Defender (Acesso Controlado)", () => TryAllowInDefender(Environment.ProcessPath))
                && Ui.StepAnimated("Testando gravacao novamente", () => ProbeWrite(dir)))
                return true;

            if (IsAdmin()
                && Ui.StepAnimated("Adicionando excecao de pasta no Defender", () => TryExclusionPath(dir))
                && Ui.StepAnimated("Testando gravacao", () => ProbeWrite(dir)))
                return true;
        }
        else if (IsAdmin())
        {
            TryExclusionPath(dir);
            if (ProbeWrite(dir))
                return true;
        }

        // 3) Eleva sozinho (UAC do Windows — usuario so clica Sim)
        if (!IsAdmin() && TryElevateHeal())
        {
            if (interactive)
            {
                Ui.Gap();
                Ui.PanelTop("ADMINISTRADOR");
                Ui.PanelLine(Ui.C("Abri uma janela pedindo permissao de administrador.", Ui.Amber));
                Ui.PanelLine(Ui.C("Clique SIM no UAC, aguarde liberar, e aplique o modo de novo.", Ui.Gray));
                Ui.PanelBottom();
            }
            return false;
        }

        // 4) Se ja e admin, ultima rodada completa
        if (IsAdmin() && Heal(dir, iniPath))
            return true;

        // 5) Abre Defender automaticamente se pasta for Documentos etc.
        if (IsLikelyControlledFolderAccess(dir))
            OpenDefenderControlledFolders();

        // 6) Reteste apos abrir Defender (usuario pode ja ter liberado)
        if (interactive)
        {
            Ui.Gap();
            Ui.Prompt("Se voce ja liberou no Defender, testar de novo agora? (S/N)");
            if (IsYes(Console.ReadLine()) && ProbeWrite(dir))
                return true;
        }

        return ProbeWrite(dir);
    }

    private static void ShowManualRecoveryGuide(string dir, string iniPath, bool interactive)
    {
        string? exe = Environment.ProcessPath;
        bool cfa = IsLikelyControlledFolderAccess(dir);
        bool copied = CopyToClipboard(exe ?? dir);

        Ui.Gap();
        Ui.PanelTop("SEM ACESSO A PASTA");
        Ui.PanelLine(Ui.C("Tentei desbloquear sozinho, mas o Windows ainda bloqueia.", Ui.Red));
        Ui.PanelLine(Ui.C("Siga um dos caminhos abaixo:", Ui.White));
        Ui.PanelBottom();

        var lines = new List<string>();
        if (cfa)
        {
            lines.Add("OPCAO A — Windows Defender (mais comum):");
            lines.Add("  1. Seguranca do Windows");
            lines.Add("  2. Protecao contra virus > Protecao contra ransomware");
            lines.Add("  3. Acesso controlado a pastas > Permitir um app");
            lines.Add("  4. Adicione o GuttyTECH_RL.exe" + (copied ? " (ja copiei o caminho!)" : ""));
            lines.Add("  5. Rode o otimizador de novo");
            lines.Add("");
        }

        lines.Add("OPCAO B — Executar como administrador:");
        lines.Add("  1. Clique direito no GuttyTECH_RL.exe");
        lines.Add("  2. Executar como administrador");
        lines.Add("  3. Aplique COMPLETO ou CRIADOR de novo");
        lines.Add("");
        lines.Add("OPCAO C — Antivirus de terceiros:");
        lines.Add("  Desative protecao de pasta ou exclua o GuttyRL.");
        lines.Add("");
        if (!string.IsNullOrWhiteSpace(exe))
            lines.Add("Exe: " + FitPath(exe, 56));
        lines.Add("Pasta: " + FitPath(dir, 56));

        Ui.CompletionMessage(Ui.MAmber, "O QUE FAZER AGORA", lines.ToArray());

        if (interactive)
        {
            Ui.Gap();
            Ui.Prompt("Abrir tela do Defender agora? (S/N)");
            if (IsYes(Console.ReadLine()))
                OpenDefenderControlledFolders();
            Ui.EnterButton();
        }
    }

    private static bool Heal(string dir, string iniPath)
    {
        UnlockDirectory(dir);
        if (File.Exists(iniPath))
            UnlockFile(iniPath);
        TryAllowInDefender(Environment.ProcessPath);
        if (IsAdmin())
            TryExclusionPath(dir);
        return ProbeWrite(dir);
    }

    private static bool ProbeWrite(string dir)
    {
        try
        {
            string t = Path.Combine(dir, "gutty_wtest.tmp");
            File.WriteAllText(t, "test");
            File.Delete(t);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void UnlockFile(string path)
    {
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
        Run("takeown.exe", $"/f \"{path}\"");
        Run("icacls.exe", $"\"{path}\" /reset");
        Run("icacls.exe", $"\"{path}\" /grant \"{Environment.UserName}:(F)\" /c /q");
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
    }

    private static void UnlockDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Run("takeown.exe", $"/f \"{dir}\" /r /d y");
        }
        catch { }

        Run("icacls.exe", $"\"{dir}\" /reset /t /c /q");
        Run("icacls.exe", $"\"{dir}\" /grant \"{Environment.UserName}:(OI)(CI)F\" /t /c /q");
    }

    private static bool TryAllowInDefender(string? exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            return false;

        string escaped = exePath.Replace("'", "''");
        string ps = $"try {{ Add-MpPreference -ControlledFolderAccessAllowedApplications '{escaped}' -ErrorAction Stop; exit 0 }} catch {{ exit 1 }}";
        return RunPowerShell(ps) == 0;
    }

    private static bool TryExclusionPath(string dir)
    {
        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            return false;

        string escaped = dir.Replace("'", "''");
        string ps = $"try {{ Add-MpPreference -ExclusionPath '{escaped}' -ErrorAction Stop; exit 0 }} catch {{ exit 1 }}";
        return RunPowerShell(ps) == 0;
    }

    private static bool TryElevateHeal()
    {
        string? exe = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exe))
            return false;

        try
        {
            var psi = new ProcessStartInfo(exe, "CORRIGIR /keepopen")
            {
                UseShellExecute = true,
                Verb = "runas"
            };
            Process.Start(psi);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void OpenDefenderControlledFolders()
    {
        foreach (string uri in new[] { "windowsdefender://protectedfolders", "ms-settings:windowsdefender" })
        {
            try
            {
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
                return;
            }
            catch { }
        }
    }

    private static bool CopyToClipboard(string text) => ClipboardUtil.TryCopy(text);

    private static bool IsLikelyControlledFolderAccess(string dir)
    {
        try
        {
            string full = Path.GetFullPath(dir);
            foreach (string root in ProtectedRoots)
            {
                if (string.IsNullOrWhiteSpace(root)) continue;
                string r = Path.GetFullPath(root);
                if (full.StartsWith(r, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch { }
        return false;
    }

    private static bool IsAdmin() => ElevationService.IsAdministrator();

    private static void Run(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            p?.WaitForExit(8000);
        }
        catch { }
    }

    private static int RunPowerShell(string command)
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            using var p = Process.Start(psi);
            if (p is null) return 1;
            p.WaitForExit(15000);
            return p.ExitCode;
        }
        catch
        {
            return 1;
        }
    }

    private static string FitPath(string p, int max)
        => p.Length <= max ? p : "..." + p[^(max - 3)..];

    private static bool IsYes(string? s)
        => string.Equals(s?.Trim(), "S", StringComparison.OrdinalIgnoreCase);
}
