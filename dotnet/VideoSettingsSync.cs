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

    /// <summary>Mata o watcher anterior (um so processo WATCH de cada vez).</summary>
    public static void StopExistingWatchers()
    {
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            if (!File.Exists(WatcherLockPath)) return;

            string[] lines = File.ReadAllLines(WatcherLockPath);
            if (lines.Length > 0 && int.TryParse(lines[0].Trim(), out int pid)
                && pid > 0 && pid != Environment.ProcessId)
            {
                try
                {
                    using var p = Process.GetProcessById(pid);
                    string name = p.ProcessName;
                    if (name.Contains("GuttyTECH_RL", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("GuttyRL", StringComparison.OrdinalIgnoreCase))
                    {
                        AppMeta.Log($"Watcher antigo a terminar (pid={pid}).");
                        p.Kill(entireProcessTree: true);
                        p.WaitForExit(4000);
                    }
                }
                catch (ArgumentException) { /* ja morto */ }
                catch (Exception ex) { AppMeta.Log("Stop watcher: " + ex.Message); }
            }

            try { File.Delete(WatcherLockPath); } catch { }
        }
        catch (Exception ex)
        {
            AppMeta.Log("StopExistingWatchers: " + ex.Message);
        }
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

    /// <summary>Espera o RL abrir (opcional) e fechar; depois reclampa INI+save.</summary>
    public static int RunWatch(string iniPath, string mode)
    {
        WriteWatcherLock(Environment.ProcessId, mode);
        AppMeta.Log($"WATCH {mode} iniciado (pid={Environment.ProcessId}).");
        try
        {
            var waitStart = DateTime.UtcNow;
            bool sawRl = GetRl().Length > 0;
            while (!sawRl && (DateTime.UtcNow - waitStart).TotalMinutes < 10)
            {
                if (!StillCurrentWatcher())
                {
                    AppMeta.Log("WATCH: substituido por outro watcher — a sair.");
                    return 0;
                }
                Thread.Sleep(2000);
                sawRl = GetRl().Length > 0;
            }
            if (!sawRl)
            {
                AppMeta.Log("WATCH: RL nao abriu — heal preventivo.");
                HealIfNeeded(iniPath, mode);
                return 0;
            }

            AppMeta.Log("WATCH: RL detetado — a aguardar fecho...");
            while (GetRl().Length > 0)
            {
                if (!StillCurrentWatcher())
                {
                    AppMeta.Log("WATCH: substituido por outro watcher — a sair.");
                    return 0;
                }
                Thread.Sleep(2000);
            }

            Thread.Sleep(2500); // cloud/EOS a gravar
            if (!StillCurrentWatcher())
            {
                AppMeta.Log("WATCH: substituido antes do heal — a sair.");
                return 0;
            }

            AppMeta.Log("WATCH: RL fechou — a reparar INI+save...");
            bool ok = HealIfNeeded(iniPath, mode);
            // 2o pass: cloud Epic por vezes regrava apos o 1o heal
            Thread.Sleep(8000);
            if (StillCurrentWatcher())
            {
                bool ok2 = HealIfNeeded(iniPath, mode);
                ok = ok && ok2;
                AppMeta.Log(ok2 ? "WATCH: 2o pass OK." : "WATCH: 2o pass falhou.");
            }
            AppMeta.Log(ok ? "WATCH: heal OK." : "WATCH: heal falhou.");
            return ok ? 0 : 1;
        }
        finally
        {
            ClearWatcherLockIfOurs();
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

    /// <summary>Reaplica CompletoForce/CriadorForce no INI sem REMOVER (pos-boot).</summary>
    public static bool ReclampIni(string iniPath, string mode)
    {
        try
        {
            if (!File.Exists(iniPath)) return false;
            try { File.SetAttributes(iniPath, FileAttributes.Normal); } catch { }

            string text = File.ReadAllText(iniPath);
            string forced = mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
                ? CompletoForce.Apply(text)
                : CriadorForce.Apply(text);

            forced = EnsureModeLine(forced, mode);
            File.WriteAllText(iniPath, forced);
            AppMeta.Log($"INI reclamp {mode} OK.");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("ReclampIni falhou: " + ex.Message);
            return false;
        }
    }

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
            if (SaveVideoPatcher.PatchSaveDirectory(saveDir, mode, progress))
                anyOk = true;
            else if (tag == "Steam")
                AppMeta.Log("Steam: save invalido/corrompido ignorado (Epic e a fonte principal).");
            else
                AppMeta.Log("Patch parcial/falhou em: " + saveDir);
        }

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

            var files = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Length <= 1_200_000)
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(6);

            foreach (var fi in files)
            {
                string backup = Path.Combine(dest, $"{ts}_{fi.Name}");
                if (!File.Exists(backup))
                    fi.CopyTo(backup, false);
            }

            AppMeta.Log($"Backup save ({ts}) dos recentes leves.");
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
