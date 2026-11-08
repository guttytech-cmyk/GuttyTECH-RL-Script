using System.Diagnostics;
using System.Text;

namespace GuttyRL;

/// <summary>Backup + patch seguro do .save (Epic e Steam) — so video/FPS.
/// Nao apaga save nem RLSettingsData.</summary>
internal static class VideoSettingsSync
{
    /// <summary>Reclampa só o INI. Desde a v25.0.31 o .save não é lido nem gravado.</summary>
    public static bool HealIfNeeded(string iniPath, string mode)
    {
        try
        {
            if (GetRl().Length > 0) return false;
            return ReclampIni(iniPath, mode);
        }
        catch (Exception ex)
        {
            AppMeta.Log("Heal INI falhou: " + ex.Message);
            return false;
        }
    }

    private static string WatcherLockPath => Path.Combine(AppMeta.GuttyDir, "watcher.lock");

    /// <summary>
    /// O RL no boot reescreve INI (Uncapped=False, shafts ON) e esvazia VideoOptions.
    /// Apos Apply, arranca UM watcher (mata o anterior) que reaplica quando o jogo fechar.
    /// </summary>
    public static void StartExitWatcher(string mode)
    {
        try
        {
            string? exe = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) return;

            // So reaproveita watcher da MESMA versao. Um processo velho (ex.: 25.0.23)
            // continuava regravando o save e o reforco desfazia o menu.
            if (IsHealthyWatcherRunning(mode))
            {
                AppMeta.Log($"Watcher {mode} ja ativo — reuse.");
                return;
            }

            StopExistingWatchers();

            // Pasta de extract SEPARADA — o single-file .NET partilha mutex de extracao
            // com o pai; sem isto o Apply/CORRIGIR-PERFIL nao termina enquanto o WATCH vive.
            string extractDir = Path.Combine(AppMeta.GuttyDir, "watch-extract");
            try { Directory.CreateDirectory(extractDir); } catch { }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = "WATCH " + mode,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            psi.Environment["DOTNET_BUNDLE_EXTRACT_BASE_DIR"] = extractDir;

            var child = Process.Start(psi);
            if (child is not null)
                WriteWatcherLock(child.Id, mode);
            AppMeta.Log("Watcher pos-jogo arrancado (" + mode + ", pid=" + (child?.Id.ToString() ?? "?") + ").");
        }
        catch (Exception ex)
        {
            AppMeta.Log("Watcher falhou a arrancar: " + ex.Message);
        }
    }

    /// <summary>True se o lock aponta para um GuttyTECH_RL vivo no mesmo modo.</summary>
    public static bool IsHealthyWatcherRunning(string mode)
    {
        try
        {
            if (!File.Exists(WatcherLockPath)) return false;
            string[] lines = File.ReadAllLines(WatcherLockPath);
            if (lines.Length == 0 || !int.TryParse(lines[0].Trim(), out int pid) || pid <= 0)
                return false;
            if (lines.Length > 1
                && !lines[1].Trim().Equals(mode, StringComparison.OrdinalIgnoreCase))
                return false;
            if (lines.Length < 3
                || !lines[2].Trim().Equals(AppMeta.Version, StringComparison.OrdinalIgnoreCase))
                return false;
            using var p = Process.GetProcessById(pid);
            return !p.HasExited
                   && p.ProcessName.Contains("GuttyTECH_RL", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Mata watchers WATCH (lock + orfaos) para o otimizador nao voltar sozinho.</summary>
    public static void StopExistingWatchers()
    {
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);

            int lockPid = 0;
            if (File.Exists(WatcherLockPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(WatcherLockPath);
                    if (lines.Length > 0)
                        int.TryParse(lines[0].Trim(), out lockPid);
                }
                catch { }
            }

            if (lockPid > 0 && lockPid != Environment.ProcessId)
                TryKillGuttyProcess(lockPid, "lock");

            // Orfaos: processos GuttyTECH_RL com argumento WATCH (lock apagado / crash).
            foreach (int pid in FindWatchPids())
            {
                if (pid == Environment.ProcessId) continue;
                TryKillGuttyProcess(pid, "WATCH orphan");
            }

            try { File.Delete(WatcherLockPath); } catch { }
        }
        catch (Exception ex)
        {
            AppMeta.Log("StopExistingWatchers: " + ex.Message);
        }
    }

    /// <summary>Remove artefactos do watcher (lock + extract). Nao apaga backups/presets.</summary>
    public static void CleanWatcherRuntime()
    {
        StopExistingWatchers();
        try
        {
            string extract = Path.Combine(AppMeta.GuttyDir, "watch-extract");
            if (Directory.Exists(extract))
            {
                try { Directory.Delete(extract, recursive: true); } catch { }
            }
            try { File.Delete(WatcherLockPath); } catch { }
            AppMeta.Log("Watcher runtime limpo.");
        }
        catch (Exception ex)
        {
            AppMeta.Log("CleanWatcherRuntime: " + ex.Message);
        }
    }

    private static void TryKillGuttyProcess(int pid, string reason)
    {
        try
        {
            using var p = Process.GetProcessById(pid);
            string name = p.ProcessName;
            if (!name.Contains("GuttyTECH_RL", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("GuttyRL", StringComparison.OrdinalIgnoreCase))
                return;

            AppMeta.Log($"A terminar watcher ({reason}, pid={pid}).");
            p.Kill(entireProcessTree: true);
            p.WaitForExit(4000);
        }
        catch (ArgumentException) { /* ja morto */ }
        catch (Exception ex) { AppMeta.Log("Stop watcher: " + ex.Message); }
    }

    private static List<int> FindWatchPids()
    {
        var ids = new List<int>();
        try
        {
            // Sem System.Management: PowerShell CIM (sem wmic).
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments =
                    "-NoProfile -NonInteractive -Command \"" +
                    "Get-CimInstance Win32_Process -ErrorAction SilentlyContinue | " +
                    "Where-Object { $_.Name -match 'GuttyTECH_RL|GuttyRL' -and $_.CommandLine -match '\\bWATCH\\b' } | " +
                    "ForEach-Object { $_.ProcessId }\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return ids;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(6000);
            foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line.Trim(), out int pid) && pid > 0)
                    ids.Add(pid);
            }
        }
        catch (Exception ex)
        {
            AppMeta.Log("FindWatchPids: " + ex.Message);
        }
        return ids;
    }

    private static void WriteWatcherLock(int pid, string mode)
    {
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(WatcherLockPath,
                pid + Environment.NewLine + mode + Environment.NewLine + AppMeta.Version + Environment.NewLine);
        }
        catch { }
    }

    private static void ClearWatcherLockIfOurs()
    {
        try
        {
            if (!File.Exists(WatcherLockPath)) return;
            string[] lines = File.ReadAllLines(WatcherLockPath);
            if (lines.Length > 0 && int.TryParse(lines[0].Trim(), out int pid) && pid == Environment.ProcessId)
                File.Delete(WatcherLockPath);
        }
        catch { }
    }

    /// <summary>
    /// Vigia todas as sessoes do RL enquanto o modo estiver ativo. O watcher antigo
    /// morria apos 10 minutos/um unico fecho e o jogo afrouxava o INI na sessao seguinte.
    /// </summary>
    public static int RunWatch(string iniPath, string mode)
    {
        WriteWatcherLock(Environment.ProcessId, mode);
        AppMeta.Log($"WATCH {mode} iniciado (pid={Environment.ProcessId}).");
        try
        {
            int session = 0;
            int idleTicks = 0;
            while (StillCurrentWatcher())
            {
                if (!IsModeStillActive(iniPath, mode))
                {
                    AppMeta.Log("WATCH: modo removido/trocado — saindo.");
                    return 0;
                }

                // Espera pela proxima sessao; se o INI afrouxou sem RL aberto
                // (fecho anterior sem watcher / cloud), reclampa preventivamente.
                while (GetRl().Length == 0)
                {
                    if (!StillCurrentWatcher()) return 0;
                    if (!IsModeStillActive(iniPath, mode))
                    {
                        AppMeta.Log("WATCH: modo removido enquanto aguardava — saindo.");
                        return 0;
                    }

                    idleTicks++;
                    if (idleTicks == 1 || idleTicks % 15 == 0) // ~0s e a cada ~30s
                    {
                        if (RlBuildStamp.TryDetectUpdate(out string buildChange))
                        {
                            AppMeta.Log($"WATCH: RL atualizou ({buildChange}). Reaplicando {mode}.");
                            if (HealUntilStable(iniPath, mode, passes: 2))
                                RlBuildStamp.RememberInstalled();
                        }
                        else
                            TryPreventiveReclamp(iniPath, mode);
                    }

                    Thread.Sleep(2000);
                }

                idleTicks = 0;
                session++;
                AppMeta.Log($"WATCH: RL detectado (sessao {session}) — aguardando fechar...");
                // O jogo reescreve textura/particula/shader cerca de 1 min depois de abrir.
                // Sem arquivo travado (attrib +r ja impediu o boot). No maximo 2 repos
                // por sessao, depois que a reescrita do jogo estabiliza.
                int watchTicks = 0;
                int driftSeen = 0;
                int driftStreak = 0;
                int reclampAttempts = 0;
                while (GetRl().Length > 0)
                {
                    if (!StillCurrentWatcher()) return 0;
                    watchTicks++;
                    if (watchTicks % 5 == 0)
                    {
                        int n = SampleSessionDrift(iniPath, mode);
                        if (n >= 3)
                        {
                            driftStreak++;
                            if (driftSeen < 3)
                                AppMeta.Log($"WATCH: {n} chaves fora do {mode} durante a sessao.");
                            if (driftStreak >= 2 && reclampAttempts < 2)
                            {
                                reclampAttempts++;
                                AppMeta.Log($"WATCH: repondo o INI com o jogo aberto ({n} chaves).");
                                ReclampIni(iniPath, mode);
                                driftStreak = 0;
                            }
                        }
                        else
                            driftStreak = 0;
                        driftSeen = Math.Max(driftSeen, n);
                    }
                    Thread.Sleep(2000);
                }

                Thread.Sleep(3000); // cloud/EOS a gravar
                if (!StillCurrentWatcher()) return 0;

                if (driftSeen >= 3)
                    AppMeta.Log($"WATCH: sessao {session} saiu com {driftSeen} chaves fora — repondo {mode}.");
                else
                    AppMeta.Log($"WATCH: sessao {session} fechou — repondo só o INI.");

                if (RlBuildStamp.TryDetectUpdate(out string buildOnClose))
                    AppMeta.Log($"WATCH: build mudou durante a sessao ({buildOnClose}).");

                bool ok = HealUntilStable(iniPath, mode, passes: 2);

                // 3o passe atrasado: Epic cloud por vezes regrava depois.
                Thread.Sleep(10000);
                if (StillCurrentWatcher() && GetRl().Length == 0)
                {
                    bool ok3 = HealUntilStable(iniPath, mode, passes: 1);
                    ok = ok && ok3;
                    AppMeta.Log(ok3 ? "WATCH: passe cloud OK." : "WATCH: passe cloud falhou.");
                }
                if (ok)
                    RlBuildStamp.RememberInstalled();
                AppMeta.Log(ok
                    ? $"WATCH: sessao {session} protegida."
                    : $"WATCH: sessao {session} com reparo parcial; continuando monitor.");
            }

            return 0;
        }
        finally
        {
            ClearWatcherLockIfOurs();
        }
    }

    private static List<string> DescribeModeDrift(string iniText, string mode) =>
        mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
            ? CompletoForce.DescribeDrift(iniText)
            : CriadorForce.DescribeDrift(iniText);

    private static int SampleSessionDrift(string iniPath, string mode)
    {
        try
        {
            if (!File.Exists(iniPath))
                return 0;
            return DescribeModeDrift(File.ReadAllText(iniPath), mode).Count;
        }
        catch
        {
            return 0;
        }
    }

    private static void TryPreventiveReclamp(string iniPath, string mode)
    {
        try
        {
            if (GetRl().Length > 0) return;
            if (!File.Exists(iniPath)) return;
            string text = File.ReadAllText(iniPath);
            var drift = DescribeModeDrift(text, mode);
            if (drift.Count == 0) return;
            AppMeta.Log(drift.Count >= 3
                ? $"WATCH: {drift.Count} chaves fora — repondo {mode}."
                : "WATCH: drift preventivo — " + string.Join("; ", drift.Take(4)));
            if (drift.Count >= 3)
                HealUntilStable(iniPath, mode, passes: 1);
            else
                ReclampIni(iniPath, mode);
        }
        catch (Exception ex)
        {
            AppMeta.Log("WATCH preventivo: " + ex.Message);
        }
    }

    /// <summary>Heal + verifica contrato do modo; repete se ainda houver drift.</summary>
    private static bool HealUntilStable(string iniPath, string mode, int passes)
    {
        bool ok = true;
        for (int i = 0; i < passes; i++)
        {
            ok = HealIfNeeded(iniPath, mode) && ok;
            try
            {
                string after = File.ReadAllText(iniPath);
                var drift = DescribeModeDrift(after, mode);
                if (drift.Count == 0)
                    return ok;
                AppMeta.Log("WATCH: drift pos-heal, repetindo — "
                            + string.Join("; ", drift.Take(3)));
                Thread.Sleep(1500);
            }
            catch { }
        }
        return ok;
    }

    private static bool IsModeStillActive(string iniPath, string mode)
    {
        try
        {
            string? detected = ModeDetect.Detect(iniPath);
            if (detected is null) return false;
            return detected.Equals(mode, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            // Erro transitorio de leitura nao deve matar a protecao.
            return true;
        }
    }

    private static bool StillCurrentWatcher()
    {
        try
        {
            // Sem lock ou PID diferente = fomos substituidos / StopExistingWatchers.
            if (!File.Exists(WatcherLockPath)) return false;
            string[] lines = File.ReadAllLines(WatcherLockPath);
            if (lines.Length == 0) return false;
            if (!int.TryParse(lines[0].Trim(), out int pid)) return false;
            return pid == Environment.ProcessId;
        }
        catch { return false; }
    }

    /// <summary>Reaplica CompletoForce/CriadorForce no INI sem REMOVER (pos-boot).
    /// Nunca usa attrib +r — isso ja travou boot no passado.</summary>
    public static bool ReclampIni(string iniPath, string mode)
    {
        try
        {
            if (!File.Exists(iniPath)) return false;
            try { File.SetAttributes(iniPath, FileAttributes.Normal); } catch { }

            string text = File.ReadAllText(iniPath);
            bool userCap = FpsChoice.TryRead(text, out int cap);
            var drift = DescribeModeDrift(text, mode);
            if (drift.Count > 0)
            {
                AppMeta.Log("INI drift detectado: "
                            + string.Join("; ", drift.Take(5)));
            }

            int dupKeys = IniHygiene.CountDuplicateKeyLines(text);
            bool bloated = text.Length >= IniHygiene.SoftBloatBytes || dupKeys > 0;
            int driftN = drift.Count;
            bool many = driftN >= 3;
            string forced;
            if (bloated || many)
            {
                AppMeta.Log(many
                    ? $"INI reclamp {mode}: {driftN} chaves fora do contrato — reescreve template."
                    : $"INI reclamp {mode}: inchado ({text.Length} bytes, dups={dupKeys}) — reescreve template limpo.");
                forced = IniHygiene.RebuildModeFromTemplate(iniPath, mode);
            }
            else
            {
                forced = mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
                    ? CompletoForce.Apply(text)
                    : CriadorForce.Apply(text);
                forced = IniHygiene.DeduplicateSectionKeys(forced);
            }

            forced = EnsureModeLine(forced, mode);
            if (userCap)
            {
                forced = FpsChoice.Stamp(forced, cap);
                AppMeta.Log($"Limite de FPS do jogador mantido: {cap}.");
            }

            // Skip write se ja esta no contrato (menos thrash / menos antivirus).
            if (string.Equals(NormalizeIni(text), NormalizeIni(forced), StringComparison.Ordinal))
            {
                ErrorRepair.ForceBootSafeIni(iniPath);
                AppMeta.Log($"INI reclamp {mode} idempotente (sem mudanca).");
                return true;
            }

            File.WriteAllText(iniPath, forced);
            // Nunca deixar boot-killers apos reclamp (HealIfNeeded / RepararPerfil).
            ErrorRepair.ForceBootSafeIni(iniPath);

            string verify = File.ReadAllText(iniPath);
            var afterDrift = DescribeModeDrift(verify, mode);
            if (afterDrift.Count > 0)
            {
                AppMeta.Log("INI reclamp verify FALHOU: "
                            + string.Join("; ", afterDrift.Take(4)));
                // Uma segunda aplicacao cobre corrida rara de escrita.
                string retry = mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
                    ? CompletoForce.Apply(verify)
                    : CriadorForce.Apply(verify);
                retry = EnsureModeLine(retry, mode);
                if (userCap)
                    retry = FpsChoice.Stamp(retry, cap);
                File.WriteAllText(iniPath, retry);
                ErrorRepair.ForceBootSafeIni(iniPath);
                verify = File.ReadAllText(iniPath);
                if (DescribeModeDrift(verify, mode).Count > 0)
                    return false;
            }

            AppMeta.Log($"INI reclamp {mode} OK.");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("ReclampIni falhou: " + ex.Message);
            return false;
        }
    }

    private static string NormalizeIni(string text) =>
        text.Replace("\r\n", "\n").TrimEnd('\n') + "\n";

    private static string EnsureModeLine(string content, string mode)
    {
        string key = "GuttyTechMode=" + mode;
        if (content.Contains("GuttyTechMode=", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            foreach (var raw in content.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.StartsWith("GuttyTechMode=", StringComparison.OrdinalIgnoreCase))
                    sb.Append(key).Append("\r\n");
                else
                    sb.Append(raw).Append("\r\n");
            }
            return sb.ToString();
        }
        const string hdr = "[SystemSettings]";
        int idx = content.IndexOf(hdr, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return key + "\r\n" + content;
        int insert = idx + hdr.Length;
        if (insert < content.Length && content[insert] == '\r') insert++;
        if (insert < content.Length && content[insert] == '\n') insert++;
        return content[..insert] + key + "\r\n" + content[insert..];
    }

    public static bool SyncVideoSave(string iniPath, string mode, bool interactive, Action<int, int, string>? progress = null)
    {
        AppMeta.Log("Sync de .save desligado (v25.0.31). Grafico e FPS ficam so no INI.");
        progress?.Invoke(1, 1, "INI");
        return true;
    }

    /// <summary>REMOVER não regrava o .save. O INI stock é o que volta.</summary>
    public static bool RevertVideoSave(string iniPath)
    {
        AppMeta.Log("REMOVER nao regrava .save (v25.0.31).");
        return true;
    }

    private static string[] SaveDirs(string iniPath)
    {
        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return Array.Empty<string>();
        return new[]
        {
            Path.Combine(tagame, "SaveDataEpic", "DBE_Production"),
            Path.Combine(tagame, "SaveData", "DBE_Production"),
        };
    }

    private static bool EnsureGameClosed(bool interactive)
    {
        var procs = GetRl();
        if (procs.Length == 0) return true;

        AppMeta.Log("RL ainda aberto no sync — encerrando automaticamente.");
        foreach (var p in procs)
        {
            try { p.Kill(); } catch { }
        }
        Thread.Sleep(1500);
        if (GetRl().Length == 0) return true;

        if (interactive)
            AppMeta.Log("Nao consegui fechar o RL; sync abortado.");
        return false;
    }

    private static bool BackupSaves(string saveDir)
    {
        try
        {
            if (!Directory.Exists(saveDir)) return true;

            string dest = SaveRecovery.BackupRoot;
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Garagem (>= SoftGarageMin) ja vai para Presets/Best em BackupGaragePresets.
            var light = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Length > 0 && f.Length < SaveRecovery.SoftGarageMinBytes)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(8);

            int n = 0;
            foreach (var fi in light)
            {
                if (SaveRecovery.ArchiveSave(fi, dest, ts))
                    n++;
            }

            if (n > 0)
                AppMeta.Log($"Backup save ({ts}): {n} arquivo(s) de menu.");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha no backup save Epic: " + ex.Message);
            return false;
        }
    }

    private static Process[] GetRl()
    {
        try { return Process.GetProcessesByName("RocketLeague"); }
        catch { return Array.Empty<Process>(); }
    }
}
