using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace GuttyRL;

/// <summary>
/// Empacota logs + diagnóstico num zip para o cliente mandar ao Gutty.
/// </summary>
internal static class SupportLogService
{
    public sealed record PackResult(bool Success, string ZipPath, string Summary, bool HasIssues);

    public static PackResult CreateSupportPack(string? cfg, Func<string?> detectMode, OptimizerStatus status)
    {
        string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string desktop = ResolveDesktop();
        string zipPath = Path.Combine(desktop, $"GuttyTECH-RL-Logs-{stamp}.zip");
        string stage = Path.Combine(Path.GetTempPath(), "GuttyTECH-RL-Support-" + stamp);
        var included = new List<string>();

        try
        {
            if (Directory.Exists(stage))
                Directory.Delete(stage, recursive: true);
            Directory.CreateDirectory(stage);

            (IReadOnlyList<string> diagLines, bool issues) = ErrorRepair.CollectDiagnosticReport(cfg, detectMode);

            WriteManifest(stage, status, diagLines, issues, included);
            included.Add("README.txt");

            WriteText(Path.Combine(stage, "diagnostico.txt"), diagLines);
            included.Add("diagnostico.txt");

            WriteText(Path.Combine(stage, "launch-command.txt"),
                Program.LaunchCommandForGui + Environment.NewLine
                + "Steam/Epic → propriedades → opções de inicialização" + Environment.NewLine);
            included.Add("launch-command.txt");

            WriteEacStatus(stage, included);
            WriteSystemSnapshot(stage, status, included);
            WriteModeFingerprint(stage, cfg, detectMode, included);
            WriteHowToSend(stage, included);

            CopyIfExists(AppMeta.LogFile, Path.Combine(stage, "log.txt"), included);
            TruncateLogIfHuge(Path.Combine(stage, "log.txt"));
            CopyIfExists(AppMeta.CrashLog, Path.Combine(stage, "crash.log"), included);
            CopyIfExists(cfg, Path.Combine(stage, "TASystemSettings.ini"), included);
            CopyIfExists(AppMeta.OrigBackup, Path.Combine(stage, "TASystemSettings.original.ini"), included);
            CopyIfExists(Path.Combine(AppMeta.GuttyDir, "watcher.lock"), Path.Combine(stage, "watcher.lock"), included);
            CopyIfExists(Path.Combine(AppMeta.GuttyDir, "applied-mode.tag"), Path.Combine(stage, "applied-mode.tag"), included);
            CopyIfExists(AppMeta.UpdateDismissedFile, Path.Combine(stage, "update-dismissed.tag"), included);

            CopyRecentIniBackups(stage, included);
            WriteSaveInventory(stage, cfg, included);
            WriteBackupIndex(stage, included);

            // Reescreve README com lista real do que entrou.
            WriteManifest(stage, status, diagLines, issues, included);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(stage, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            if (!File.Exists(zipPath) || new FileInfo(zipPath).Length < 64)
                throw new InvalidOperationException("Zip vazio ou nao criado.");

            // Validacao minima: tem de ter README + diagnostico.
            using (ZipArchive zip = ZipFile.OpenRead(zipPath))
            {
                bool hasReadme = zip.GetEntry("README.txt") is not null;
                bool hasDiag = zip.GetEntry("diagnostico.txt") is not null;
                if (!hasReadme || !hasDiag)
                    throw new InvalidOperationException("Zip incompleto (falta README/diagnostico).");
            }

            ClipboardUtil.TryCopy(zipPath);
            if (ElevationService.IsAdministrator())
                ClipboardUtil.TryCopyUnelevated(zipPath);
            RevealInExplorer(zipPath);

            AppMeta.Log($"SUPPORT-PACK: {zipPath} files={included.Count} issues={issues}");
            string summary = issues
                ? $"Zip no Desktop com alertas ({included.Count} ficheiros). Manda o ZIP pro Gutty: {Path.GetFileName(zipPath)}"
                : $"Zip no Desktop pronto ({included.Count} ficheiros). Manda o ZIP pro Gutty: {Path.GetFileName(zipPath)}";

            return new PackResult(true, zipPath, summary, issues);
        }
        catch (Exception ex)
        {
            AppMeta.Log("SUPPORT-PACK falhou: " + ex.Message);
            return new PackResult(
                false,
                zipPath,
                "Não consegui gerar o pacote de logs (" + ex.Message + "). Tenta de novo ou manda o log.txt da pasta GuttyTECH.",
                true);
        }
        finally
        {
            try
            {
                if (Directory.Exists(stage))
                    Directory.Delete(stage, recursive: true);
            }
            catch { /* best effort */ }
        }
    }

    private static string ResolveDesktop()
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (!string.IsNullOrWhiteSpace(desktop) && Directory.Exists(desktop))
            return desktop;

        string oneDrive = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "OneDrive", "Desktop");
        if (Directory.Exists(oneDrive))
            return oneDrive;

        return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    private static void WriteManifest(
        string stage,
        OptimizerStatus status,
        IReadOnlyList<string> diagLines,
        bool issues,
        List<string> included)
    {
        var sb = new StringBuilder();
        sb.AppendLine("GUTTYTECH RL OPTIMIZER — PACOTE DE SUPORTE");
        sb.AppendLine("Manda este ZIP INTEIRO pro Gutty (chat / Discord / WhatsApp / email).");
        sb.AppendLine("Nao precisas de abrir nem editar nada — so anexar o ficheiro.");
        sb.AppendLine();
        sb.AppendLine("Gerado: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("App: " + AppMeta.Version);
        sb.AppendLine("Exe: " + (Environment.ProcessPath ?? "(desconhecido)"));
        sb.AppendLine("OS: " + Environment.OSVersion);
        sb.AppendLine("64bit OS: " + Environment.Is64BitOperatingSystem);
        sb.AppendLine("Admin: " + status.IsAdministrator);
        sb.AppendLine("Home: " + AppMeta.GuttyDir);
        sb.AppendLine();
        sb.AppendLine("=== ESTADO GUI ===");
        sb.AppendLine("Modo: " + status.AppliedMode);
        sb.AppendLine("Estado: " + status.StateLabel);
        sb.AppendLine("INI: " + status.ConfigPath);
        sb.AppendLine("INI existe: " + status.ConfigExists);
        sb.AppendLine("Gravavel: " + status.IsWritable);
        sb.AppendLine("Protecao (modo): " + status.IsProtected);
        sb.AppendLine("Watcher: " + status.IsWatcherActive);
        sb.AppendLine("Rocket League aberto: " + status.IsRocketLeagueOpen);
        sb.AppendLine("Diagnostico com alertas: " + issues);
        sb.AppendLine();
        sb.AppendLine("=== DIAGNOSTICO ===");
        foreach (string line in diagLines)
            sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine("=== FICHEIROS NESTE ZIP ===");
        foreach (string name in included.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x))
            sb.AppendLine("- " + name);

        WriteText(Path.Combine(stage, "README.txt"), sb.ToString());
    }

    private static void WriteHowToSend(string stage, List<string> included)
    {
        WriteText(Path.Combine(stage, "COMO-ENVIAR.txt"),
            "COMO MANDAR PRO GUTTY" + Environment.NewLine
            + "====================" + Environment.NewLine
            + Environment.NewLine
            + "1) Este ficheiro .zip ja esta no Desktop." + Environment.NewLine
            + "2) Anexa o ZIP completo na conversa com o Gutty (Cursor chat, Discord, WhatsApp ou email)." + Environment.NewLine
            + "3) Nao precisas de extrair, editar nem mandar ficheiros soltos." + Environment.NewLine
            + "4) Se o chat recusar o tamanho, usa WeTransfer/Google Drive e manda o link." + Environment.NewLine
            + Environment.NewLine
            + "O que o Gutty ve neste pacote:" + Environment.NewLine
            + "- diagnostico + estado do modo (COMPLETO/CRIADOR)" + Environment.NewLine
            + "- TASystemSettings.ini (+ backups recentes)" + Environment.NewLine
            + "- log/crash do otimizador" + Environment.NewLine
            + "- EAC, watcher, inventario de saves (sem binarios grandes)" + Environment.NewLine
            + "- comando de inicializacao Steam/Epic" + Environment.NewLine);
        included.Add("COMO-ENVIAR.txt");
    }

    private static void WriteModeFingerprint(string stage, string? cfg, Func<string?> detectMode, List<string> included)
    {
        var sb = new StringBuilder();
        string? mode = detectMode();
        sb.AppendLine("ModeDetect: " + (mode ?? "(nenhum)"));
        sb.AppendLine("Tag file: " + (File.Exists(Path.Combine(AppMeta.GuttyDir, "applied-mode.tag"))
            ? File.ReadAllText(Path.Combine(AppMeta.GuttyDir, "applied-mode.tag")).Trim()
            : "(ausente)"));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(cfg) && File.Exists(cfg))
        {
            try
            {
                string text = File.ReadAllText(cfg);
                if (mode == "COMPLETO" || text.Contains("GuttyTechMode=COMPLETO", StringComparison.OrdinalIgnoreCase))
                {
                    var drift = CompletoForce.DescribeDrift(text);
                    sb.AppendLine("CompletoForce.HasDrift: " + (drift.Count > 0));
                    if (drift.Count == 0)
                        sb.AppendLine("Contrato COMPLETO: OK (sem drift)");
                    else
                    {
                        sb.AppendLine("Drift COMPLETO:");
                        foreach (string d in drift.Take(20))
                            sb.AppendLine("  - " + d);
                    }
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Falha a ler INI: " + ex.Message);
            }
        }

        WriteText(Path.Combine(stage, "modo-fingerprint.txt"), sb.ToString());
        included.Add("modo-fingerprint.txt");
    }

    /// <summary>Evita ZIPs gigantes se o log crescer sem controlo.</summary>
    private static void TruncateLogIfHuge(string path)
    {
        try
        {
            if (!File.Exists(path)) return;
            var fi = new FileInfo(path);
            const long maxBytes = 512 * 1024; // 512 KB
            if (fi.Length <= maxBytes) return;

            string all = File.ReadAllText(path);
            // Fica com o final (erros recentes).
            int keepChars = (int)Math.Min(all.Length, maxBytes / 2);
            string tail = all[^keepChars..];
            File.WriteAllText(
                path,
                "[log truncado — so as ultimas linhas]" + Environment.NewLine + tail,
                new UTF8Encoding(false));
        }
        catch { }
    }

    private static void WriteEacStatus(string stage, List<string> included)
    {
        try
        {
            var (ok, detail) = EacRepairService.Assess();
            string setup = EacRepairService.FindSetupExe() ?? "(nao encontrado)";
            WriteText(Path.Combine(stage, "eac-status.txt"),
                "Healthy: " + ok + Environment.NewLine
                + "Detail: " + detail + Environment.NewLine
                + "Setup: " + setup + Environment.NewLine);
            included.Add("eac-status.txt");
        }
        catch (Exception ex)
        {
            WriteText(Path.Combine(stage, "eac-status.txt"), "Falha: " + ex.Message);
            included.Add("eac-status.txt");
        }
    }

    private static void WriteSystemSnapshot(string stage, OptimizerStatus status, List<string> included)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Machine: " + Environment.MachineName);
        sb.AppendLine("User: " + Environment.UserName);
        sb.AppendLine("CLR: " + Environment.Version);
        sb.AppendLine("Proc count: " + Environment.ProcessorCount);
        sb.AppendLine("Working set MB: " + (Environment.WorkingSet / (1024 * 1024)));
        sb.AppendLine("Launch cmd: " + Program.LaunchCommandForGui);
        sb.AppendLine("AppliedMode: " + status.AppliedMode);
        try
        {
            foreach (var p in Process.GetProcessesByName("RocketLeague"))
                sb.AppendLine("RL pid=" + p.Id);
            foreach (var p in Process.GetProcessesByName("GuttyTECH_RL"))
                sb.AppendLine("GuttyTECH_RL pid=" + p.Id + " sess=" + p.SessionId);
        }
        catch { }
        WriteText(Path.Combine(stage, "system.txt"), sb.ToString());
        included.Add("system.txt");
    }

    private static void CopyRecentIniBackups(string stage, List<string> included)
    {
        try
        {
            if (!Directory.Exists(AppMeta.BackupDir)) return;
            string bakDir = Path.Combine(stage, "ini-backups");
            Directory.CreateDirectory(bakDir);
            foreach (string file in Directory.EnumerateFiles(AppMeta.BackupDir, "TASystemSettings*.bak")
                         .OrderByDescending(File.GetLastWriteTimeUtc)
                         .Take(5))
            {
                string dest = Path.Combine(bakDir, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
                included.Add("ini-backups/" + Path.GetFileName(file));
            }
        }
        catch (Exception ex)
        {
            AppMeta.Log("SUPPORT-PACK backups: " + ex.Message);
        }
    }

    private static void WriteBackupIndex(string stage, List<string> included)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Indice da pasta GuttyTECH (sem binarios grandes)");
        sb.AppendLine();
        try
        {
            if (!Directory.Exists(AppMeta.GuttyDir))
            {
                sb.AppendLine("(pasta ausente)");
            }
            else
            {
                foreach (string file in Directory.EnumerateFiles(AppMeta.GuttyDir, "*", SearchOption.AllDirectories)
                             .OrderByDescending(File.GetLastWriteTimeUtc)
                             .Take(80))
                {
                    var fi = new FileInfo(file);
                    string rel = Path.GetRelativePath(AppMeta.GuttyDir, file);
                    sb.AppendLine($"{fi.Length,12}  {fi.LastWriteTime:yyyy-MM-dd HH:mm}  {rel}");
                }
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine("falha: " + ex.Message);
        }

        WriteText(Path.Combine(stage, "gutty-folder-index.txt"), sb.ToString());
        included.Add("gutty-folder-index.txt");
    }

    private static void WriteSaveInventory(string stage, string? cfg, List<string> included)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Inventario de saves (sem copiar binarios pesados)");
        sb.AppendLine();

        void Dump(string label, string? dir)
        {
            sb.AppendLine("[" + label + "]");
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                sb.AppendLine("  (pasta ausente)");
                sb.AppendLine();
                return;
            }

            sb.AppendLine("  Path: " + dir);
            try
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
                             .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                             .Take(200))
                {
                    var fi = new FileInfo(file);
                    sb.AppendLine($"  {fi.Length,12}  {fi.LastWriteTime:yyyy-MM-dd HH:mm}  {fi.Name}");
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("  (falha ao listar: " + ex.Message + ")");
            }

            sb.AppendLine();
        }

        Dump("Epic", SaveRecovery.SaveDirFromIni(cfg ?? "", epic: true));
        Dump("Steam", SaveRecovery.SaveDirFromIni(cfg ?? "", epic: false));
        WriteText(Path.Combine(stage, "saves-inventory.txt"), sb.ToString());
        included.Add("saves-inventory.txt");
    }

    private static void WriteText(string path, IEnumerable<string> lines) =>
        WriteText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);

    private static void WriteText(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void CopyIfExists(string? source, string dest, List<string> included)
    {
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            return;

        try
        {
            File.Copy(source, dest, overwrite: true);
            included.Add(Path.GetFileName(dest));
        }
        catch (Exception ex)
        {
            AppMeta.Log($"SUPPORT-PACK copy {Path.GetFileName(source)}: {ex.Message}");
        }
    }

    private static void RevealInExplorer(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + path + "\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            AppMeta.Log("SUPPORT-PACK explorer: " + ex.Message);
        }
    }
}
