using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>
/// Reparo do Easy Anti-Cheat EOS (erro 30005 / CreateService 1072).
/// 1072 = ERROR_SERVICE_MARKED_FOR_DELETE — servico marcado para apagar;
/// reinicio limpa, mas instalador EAC costuma recuperar sem reboot.
/// O otimizador NAO causa isto (so INI/save); isto e falha Windows/EAC.
/// </summary>
internal static class EacRepairService
{
    private const string ServiceName = "EasyAntiCheat_EOS";
    private const string DefaultProductId = "e6bcca5b37d0457ca881aec508205542"; // Rocket League Settings.json

    public static (bool Healthy, string Detail) Assess()
    {
        string? setup = FindSetupExe();
        if (setup is null)
            return (false, "EasyAntiCheat_EOS_Setup.exe nao encontrado (verifique instalacao RL).");

        int query = RunScCapture($"query {ServiceName}", out string output);
        if (query != 0 && output.IndexOf("1060", StringComparison.Ordinal) >= 0)
            return (false, $"servico {ServiceName} ausente (1060) — tipico apos 1072.");

        if (output.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) >= 0
            || output.IndexOf("1072", StringComparison.Ordinal) >= 0)
            return (false, $"servico {ServiceName} marcado para apagar (1072) — precisa repair/reboot.");

        bool running = output.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) >= 0;
        bool stopped = output.IndexOf("STOPPED", StringComparison.OrdinalIgnoreCase) >= 0;
        if (running || stopped)
            return (true, $"servico {ServiceName}: {(running ? "RUNNING" : "STOPPED")}");

        return (query == 0, $"servico {ServiceName}: query exit={query}");
    }

    /// <summary>Para RL/EAC, reinstala o servico EOS e valida.</summary>
    public static (bool Ok, bool NeedsReboot, string Detail) Repair()
    {
        var log = new List<string>();
        try
        {
            ForceCloseGameAndEac();
            log.Add("processos RL/EAC fechados");

            string? setup = FindSetupExe();
            if (setup is null)
                return (false, false, "Setup EAC nao encontrado. Verifique ficheiros na Epic/Steam.");

            string productId = ReadProductId(Path.GetDirectoryName(setup)!) ?? DefaultProductId;
            log.Add("productid=" + productId[..Math.Min(8, productId.Length)] + "…");

            // Parar servico se existir (ignora falhas — pode ja estar delete-pending).
            RunSc($"stop {ServiceName}", log);
            Thread.Sleep(1500);

            // Tentar limpar entrada morta (1072). Se falhar com pending delete → reboot.
            int delCode = RunSc($"delete {ServiceName}", log);
            if (delCode == 1072 || delCode == 1058)
            {
                // 1072 marked for delete; 1058 disabled — install pode ainda funcionar apos reboot
                // mas tentamos install na mesma.
                log.Add($"sc delete code={delCode} (pending/disabled)");
            }

            Thread.Sleep(800);

            int installCode = RunProcess(
                setup,
                "install " + productId,
                Path.GetDirectoryName(setup)!,
                log);

            if (installCode != 0)
            {
                // Segunda tentativa: so install sem delete previo.
                Thread.Sleep(1000);
                installCode = RunProcess(setup, "install " + productId, Path.GetDirectoryName(setup)!, log);
            }

            RunSc($"config {ServiceName} start= demand", log);

            var (healthy, assess) = Assess();
            log.Add(assess);

            if (healthy && installCode == 0)
            {
                AppMeta.Log("EAC-REPAIR OK: " + string.Join("; ", log));
                return (true, false, string.Join(" · ", log));
            }

            // Servico ainda inexistente / delete-pending → precisa reboot.
            bool needsReboot = !healthy
                               || delCode == 1072
                               || log.Any(l => l.Contains("1072", StringComparison.Ordinal));
            AppMeta.Log("EAC-REPAIR parcial: " + string.Join("; ", log));
            return (false, needsReboot, string.Join(" · ", log));
        }
        catch (Exception ex)
        {
            AppMeta.Log("EAC-REPAIR falhou: " + ex.Message);
            return (false, true, "Falha EAC: " + ex.Message + " — reinicie o PC e tente de novo.");
        }
    }

    public static string? FindSetupExe()
    {
        foreach (string root in CandidateGameRoots())
        {
            string p = Path.Combine(root, "Binaries", "Win64", "EasyAntiCheat", "EasyAntiCheat_EOS_Setup.exe");
            if (File.Exists(p)) return p;
            p = Path.Combine(root, "EasyAntiCheat", "EasyAntiCheat_EOS_Setup.exe");
            if (File.Exists(p)) return p;
        }

        try
        {
            foreach (string drive in Environment.GetLogicalDrives())
            {
                string epic = Path.Combine(drive, "Program Files", "Epic Games", "rocketleague",
                    "Binaries", "Win64", "EasyAntiCheat", "EasyAntiCheat_EOS_Setup.exe");
                if (File.Exists(epic)) return epic;
            }
        }
        catch { }

        return null;
    }

    private static IEnumerable<string> CandidateGameRoots()
    {
        string? fromIni = TryGameRootFromIni();
        if (fromIni is not null) yield return fromIni;

        yield return @"C:\Program Files\Epic Games\rocketleague";
        yield return @"C:\Program Files (x86)\Steam\steamapps\common\rocketleague";
        yield return @"D:\Program Files\Epic Games\rocketleague";
        yield return @"D:\SteamLibrary\steamapps\common\rocketleague";
        yield return @"E:\SteamLibrary\steamapps\common\rocketleague";
    }

    private static string? TryGameRootFromIni()
    {
        try
        {
            string ini = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "My Games", "Rocket League", "TAGame", "Config", "TASystemSettings.ini");
            // Heuristica: nao ha path do jogo no INI; usa InstallLocation se existir via Epic manifest — skip.
            _ = ini;
        }
        catch { }
        return null;
    }

    private static string? ReadProductId(string eacDir)
    {
        try
        {
            string settings = Path.Combine(eacDir, "Settings.json");
            if (!File.Exists(settings)) return null;
            string json = File.ReadAllText(settings);
            var m = Regex.Match(json, "\"productid\"\\s*:\\s*\"([a-fA-F0-9]+)\"", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }
        catch
        {
            return null;
        }
    }

    private static void ForceCloseGameAndEac()
    {
        foreach (string name in new[] { "RocketLeague", "EasyAntiCheat_EOS", "EasyAntiCheat", "Launcher" })
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        // Nao matar Launcher generico da Epic Games Launcher — so o do RL.
                        if (name.Equals("Launcher", StringComparison.OrdinalIgnoreCase)
                            && p.MainModule?.FileName is string path
                            && path.IndexOf("rocketleague", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;
                        p.Kill(entireProcessTree: true);
                    }
                    catch { }
                    finally { try { p.Dispose(); } catch { } }
                }
            }
            catch { }
        }
        Thread.Sleep(800);
    }

    private static int RunSc(string args, List<string> log) =>
        RunProcess("sc.exe", args, Environment.SystemDirectory, log);

    private static int RunScCapture(string args, out string output)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sc.exe",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi);
            if (p is null) { output = ""; return -1; }
            output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
            p.WaitForExit(15_000);
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            output = ex.Message;
            return -1;
        }
    }

    private static int RunProcess(string file, string args, string workDir, List<string> log)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                WorkingDirectory = workDir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi);
            if (p is null)
            {
                log.Add("falha start " + Path.GetFileName(file));
                return -1;
            }
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(120_000);
            int code = p.ExitCode;
            string snippet = (stdout + " " + stderr).Replace("\r", " ").Replace("\n", " ").Trim();
            if (snippet.Length > 160) snippet = snippet[..160] + "…";
            log.Add($"{Path.GetFileName(file)} [{args.Split(' ')[0]}] exit={code}"
                    + (string.IsNullOrWhiteSpace(snippet) ? "" : " " + snippet));
            return code;
        }
        catch (Exception ex)
        {
            log.Add(Path.GetFileName(file) + " erro: " + ex.Message);
            return -1;
        }
    }
}
