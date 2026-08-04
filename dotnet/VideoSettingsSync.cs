using System.Diagnostics;
using System.Text;

namespace GuttyRL;

/// <summary>Backup + patch seguro do .save (Epic e Steam) — so video/FPS.
/// Nao apaga save nem RLSettingsData.</summary>
internal static class VideoSettingsSync
{
    /// <summary>Repara VideoOptions + reclampa INI (arranque / pos-jogo).</summary>
    public static bool HealIfNeeded(string iniPath, string mode)
    {
        try
        {
            if (GetRl().Length > 0) return false;
            // Snapshot antes do patch — se o assemble encolher, o reforco recupera.
            try { SaveRecovery.BackupGaragePresets(iniPath); } catch { }
            bool ok = SyncVideoSave(iniPath, mode, interactive: false);
            ok = ReclampIni(iniPath, mode) && ok;
            return ok;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Heal video falhou: " + ex.Message);
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

            // Se ja ha watcher vivo do mesmo modo, nao matar/reiniciar (evita buraco
            // de protecao enquanto o RL esta aberto).
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
            File.WriteAllText(WatcherLockPath, pid + Environment.NewLine + mode + Environment.NewLine);
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
                    AppMeta.Log("WATCH: modo removido/trocado — a sair.");
                    return 0;
                }

                // Espera pela proxima sessao; se o INI afrouxou sem RL aberto
                // (fecho anterior sem watcher / cloud), reclampa preventivamente.
                while (GetRl().Length == 0)
                {
                    if (!StillCurrentWatcher()) return 0;
                    if (!IsModeStillActive(iniPath, mode))
                    {
                        AppMeta.Log("WATCH: modo removido enquanto aguardava — a sair.");
                        return 0;
                    }

                    idleTicks++;
                    if (idleTicks == 1 || idleTicks % 15 == 0) // ~0s e a cada ~30s
                        TryPreventiveReclamp(iniPath, mode);

                    Thread.Sleep(2000);
                }

                idleTicks = 0;
                session++;
                AppMeta.Log($"WATCH: RL detetado (sessao {session}) — a aguardar fecho...");
                // Nao escrever no INI com o jogo aberto: o RL regrava no exit e
                // mid-session write so gera corrida (historico: attrib +r = boot hang).
                while (GetRl().Length > 0)
                {
                    if (!StillCurrentWatcher()) return 0;
                    Thread.Sleep(2000);
                }

                Thread.Sleep(3000); // cloud/EOS a gravar
                if (!StillCurrentWatcher()) return 0;

                AppMeta.Log($"WATCH: sessao {session} fechou — a reparar INI+save...");
                bool ok = HealUntilStable(iniPath, mode, passes: 2);

                // 3o passe atrasado: Epic cloud por vezes regrava depois.
                Thread.Sleep(10000);
                if (StillCurrentWatcher() && GetRl().Length == 0)
                {
                    bool ok3 = HealUntilStable(iniPath, mode, passes: 1);
                    ok = ok && ok3;
                    AppMeta.Log(ok3 ? "WATCH: passe cloud OK." : "WATCH: passe cloud falhou.");
                }
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

    private static void TryPreventiveReclamp(string iniPath, string mode)
    {
        try
        {
            if (GetRl().Length > 0) return;
            if (!mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)) return;
            if (!File.Exists(iniPath)) return;
            string text = File.ReadAllText(iniPath);
            if (!CompletoForce.HasDrift(text)) return;
            var sample = CompletoForce.DescribeDrift(text);
            AppMeta.Log("WATCH: drift preventivo — " + string.Join("; ", sample.Take(4)));
            HealUntilStable(iniPath, mode, passes: 1);
        }
        catch (Exception ex)
        {
            AppMeta.Log("WATCH preventivo: " + ex.Message);
        }
    }

    /// <summary>Heal + verifica contrato Completo; repete se ainda houver drift.</summary>
    private static bool HealUntilStable(string iniPath, string mode, int passes)
    {
        bool ok = true;
        for (int i = 0; i < passes; i++)
        {
            ok = HealIfNeeded(iniPath, mode) && ok;
            if (!mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase))
                continue;
            try
            {
                string after = File.ReadAllText(iniPath);
                if (!CompletoForce.HasDrift(after))
                    return ok;
                AppMeta.Log("WATCH: drift pos-heal, a repetir — "
                            + string.Join("; ", CompletoForce.DescribeDrift(after).Take(3)));
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
            if (mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
                && CompletoForce.HasDrift(text))
            {
                AppMeta.Log("INI drift detetado: "
                            + string.Join("; ", CompletoForce.DescribeDrift(text).Take(5)));
            }

            string forced = mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
                ? CompletoForce.Apply(text)
                : CriadorForce.Apply(text);

            forced = EnsureModeLine(forced, mode);

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

            if (mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase))
            {
                string verify = File.ReadAllText(iniPath);
                if (CompletoForce.HasDrift(verify))
                {
                    AppMeta.Log("INI reclamp verify FALHOU: "
                                + string.Join("; ", CompletoForce.DescribeDrift(verify).Take(4)));
                    // Uma segunda aplicacao cobre corrida rara de escrita.
                    File.WriteAllText(iniPath, CompletoForce.Apply(verify));
                    ErrorRepair.ForceBootSafeIni(iniPath);
                    verify = File.ReadAllText(iniPath);
                    if (CompletoForce.HasDrift(verify))
                        return false;
                }
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
        if (!EnsureGameClosed(interactive)) return false;

        // Preserva presets ANTES do patch UE3 (pode encolher save de garagem).
        try { SaveRecovery.BackupGaragePresets(iniPath); } catch { }

        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return false;

        string[] saveDirs =
        {
            Path.Combine(tagame, "SaveDataEpic", "DBE_Production"),
            Path.Combine(tagame, "SaveData", "DBE_Production"),
        };

        bool anyDir = false;
        bool anyOk = false;
        foreach (string saveDir in saveDirs)
        {
            if (!Directory.Exists(saveDir)) continue;
            anyDir = true;
            string tag = saveDir.Contains("SaveDataEpic", StringComparison.OrdinalIgnoreCase) ? "Epic" : "Steam";
            progress?.Invoke(0, 1, tag);
            BackupSaves(saveDir);
            if (SaveVideoPatcher.PatchSaveDirectory(saveDir, mode, (cur, tot, detail) =>
                {
                    progress?.Invoke(cur, tot, string.IsNullOrWhiteSpace(detail) ? tag : $"{tag} · {detail}");
                }))
                anyOk = true;
            else if (tag == "Steam")
                AppMeta.Log("Steam: save invalido/corrompido ignorado (Epic e a fonte principal).");
            else
                AppMeta.Log("Patch parcial/falhou em: " + saveDir);
        }

        // Sempre reforca garagem apos patch — Apply nao deve obrigar CORRIGIR ERROS.
        try { SaveRecovery.ReinforceGarageAfterVideoSync(iniPath); } catch { }

        if (!anyDir)
        {
            AppMeta.Log("Nenhum SaveDataEpic/SaveData encontrado; menu in-game nao sincronizado.");
            return true;
        }

        return anyOk;
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

            string dest = Path.Combine(AppMeta.BackupDir, "SaveDataEpic");
            Directory.CreateDirectory(dest);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Leves: video sync (rapido).
            var light = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Length > 0 && f.Length < SaveRecovery.SoftGarageMinBytes)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(8);

            // Pesados: presets/garagem — so copia, sem decrypt.
            var heavy = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Length >= SaveRecovery.SoftGarageMinBytes && f.Length <= SaveRecovery.GarageMaxBytes)
                .OrderByDescending(f => f.Length)
                .ThenByDescending(f => f.LastWriteTimeUtc)
                .Take(12);

            int n = 0;
            foreach (var fi in light.Concat(heavy).GroupBy(f => f.FullName).Select(g => g.First()))
            {
                string backup = Path.Combine(dest, $"{ts}_{fi.Name}");
                if (!File.Exists(backup))
                {
                    fi.CopyTo(backup, false);
                    n++;
                }

                // Cofre sticky Best — nunca perde o maior save da conta
                if (fi.Length >= SaveRecovery.SoftGarageMinBytes)
                    SaveRecovery.UpdateBestVault(fi.Name, fi.FullName, fi.Length);
            }

            AppMeta.Log($"Backup save ({ts}): {n} ficheiro(s) leves+garagem.");
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
