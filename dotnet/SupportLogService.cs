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
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string zipPath = Path.Combine(desktop, $"GuttyTECH-RL-Logs-{stamp}.zip");
        string stage = Path.Combine(Path.GetTempPath(), "GuttyTECH-RL-Support-" + stamp);

        try
        {
            if (Directory.Exists(stage))
                Directory.Delete(stage, recursive: true);
            Directory.CreateDirectory(stage);

            (IReadOnlyList<string> diagLines, bool issues) = ErrorRepair.CollectDiagnosticReport(cfg, detectMode);
            WriteManifest(stage, status, diagLines, issues);
            WriteText(Path.Combine(stage, "diagnostico.txt"), diagLines);
            TryCopy(AppMeta.LogFile, Path.Combine(stage, "log.txt"));
            TryCopy(AppMeta.CrashLog, Path.Combine(stage, "crash.log"));
            TryCopy(cfg, Path.Combine(stage, "TASystemSettings.ini"));
            TryCopy(AppMeta.OrigBackup, Path.Combine(stage, "TASystemSettings.original.ini"));
            TryCopy(Path.Combine(AppMeta.GuttyDir, "watcher.lock"), Path.Combine(stage, "watcher.lock"));
            WriteSaveInventory(stage, cfg);

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            ZipFile.CreateFromDirectory(stage, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);

            ClipboardUtil.TryCopy(zipPath);
            RevealInExplorer(zipPath);

            AppMeta.Log($"SUPPORT-PACK: {zipPath} issues={issues}");
            string summary = issues
                ? $"Zip no Desktop com alertas no diagnóstico. Caminho copiado. Manda pro Gutty: {Path.GetFileName(zipPath)}"
                : $"Zip no Desktop pronto. Caminho copiado. Manda pro Gutty: {Path.GetFileName(zipPath)}";

            return new PackResult(true, zipPath, summary, issues);
        }
        catch (Exception ex)
        {
            AppMeta.Log("SUPPORT-PACK falhou: " + ex.Message);
            return new PackResult(
                false,
                zipPath,
                "Não consegui gerar o pacote de logs. Tenta de novo ou manda o arquivo log.txt da pasta GuttyTECH.",
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

    private static void WriteManifest(
        string stage,
        OptimizerStatus status,
        IReadOnlyList<string> diagLines,
        bool issues)
    {
        var sb = new StringBuilder();
        sb.AppendLine("GUTTYTECH RL OPTIMIZER — PACOTE DE SUPORTE");
        sb.AppendLine("Manda este ZIP pro Gutty pra ele resolver o erro.");
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
        sb.AppendLine("Protecao CFA: " + status.IsProtected);
        sb.AppendLine("Rocket League aberto: " + status.IsRocketLeagueOpen);
        sb.AppendLine("Diagnostico com alertas: " + issues);
        sb.AppendLine();
        sb.AppendLine("=== DIAGNOSTICO ===");
        foreach (string line in diagLines)
            sb.AppendLine(line);
        sb.AppendLine();
        sb.AppendLine("=== CONTEUDO DO ZIP ===");
        sb.AppendLine("- README.txt (este arquivo)");
        sb.AppendLine("- diagnostico.txt");
        sb.AppendLine("- log.txt / crash.log (se existirem)");
        sb.AppendLine("- TASystemSettings.ini (+ original se houver backup)");
        sb.AppendLine("- saves-inventory.txt (nomes/tamanhos, sem binarios)");
        sb.AppendLine("- watcher.lock (se ativo)");

        WriteText(Path.Combine(stage, "README.txt"), sb.ToString());
    }

    private static void WriteSaveInventory(string stage, string? cfg)
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
    }

    private static void WriteText(string path, IEnumerable<string> lines) =>
        WriteText(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);

    private static void WriteText(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void TryCopy(string? source, string dest)
    {
        if (string.IsNullOrWhiteSpace(source) || !File.Exists(source))
            return;

        try
        {
            File.Copy(source, dest, overwrite: true);
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
