using System.Diagnostics;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>
/// Backup/restauro de saves Epic/Steam (presets/garagem) + purge RLSettingsData.
/// Cofre sticky Best: nunca deixa um save grande ser substituido por um pequenino.
/// </summary>
internal static class SaveRecovery
{
    private static readonly Regex BackupName = new(
        @"^\d{8}_\d{6}_(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Saves com presets/garagem costumam ficar acima disto.
    /// 1.5MB era demasiado alto — contas com poucos presets nunca entravam no backup.
    /// </summary>
    public const long GarageMinBytes = 400_000;

    /// <summary>Abaixo disto quase sempre e so video/menu stub.</summary>
    public const long SoftGarageMinBytes = 250_000;

    public const long GarageMaxBytes = 16_000_000;

    public static string BackupRoot => Path.Combine(AppMeta.BackupDir, "SaveDataEpic");
    public static string PresetsRoot => Path.Combine(AppMeta.BackupDir, "Presets");
    public static string BestRoot => Path.Combine(PresetsRoot, "Best");

    public static string? SaveDirFromIni(string iniPath, bool epic = true)
    {
        if (string.IsNullOrWhiteSpace(iniPath)) return null;
        string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
        if (tagame is null) return null;
        return Path.Combine(tagame, epic ? "SaveDataEpic" : "SaveData", "DBE_Production");
    }

    public static bool RestoreEpicSave(string iniPath, bool preferNewest = false) =>
        RestoreInto(SaveDirFromIni(iniPath, epic: true), preferNewest, preferGarage: preferNewest);

    public static bool RestoreSteamSave(string iniPath, bool preferNewest = false)
    {
        string? dir = SaveDirFromIni(iniPath, epic: false);
        if (dir is null || !Directory.Exists(dir))
            return false;
        return RestoreInto(dir, preferNewest, preferGarage: preferNewest, createIfMissing: false);
    }

    public const string CleanSaveTagName = "v25_0_31_clean_save.tag";

    /// <summary>Uma vez na v25.0.31. O watcher não chama isto.</summary>
    public static void MigrateCleanSaveOnce(string iniPath)
    {
        string tag = Path.Combine(AppMeta.GuttyDir, CleanSaveTagName);
        if (File.Exists(tag)) return;
        AppMeta.Log("Migracao v25.0.31: esvaziando o save local contaminado.");
        if (CleanGarageReset(iniPath, out string summary, reapplyMode: true))
        {
            try
            {
                Directory.CreateDirectory(AppMeta.GuttyDir);
                File.WriteAllText(tag, AppMeta.Version + Environment.NewLine + summary);
            }
            catch { }
            AppMeta.Log("Migracao v25.0.31 OK: " + summary);
        }
        else
            AppMeta.Log("Migracao v25.0.31 incompleta: " + summary);
    }

    /// <summary>RESTAURAR PRESETS: esvazia o save local. O jogo cria um .save novo da Psyonix.</summary>
    public static bool RestoreLatestBackup(string iniPath) =>
        RestorePresets(iniPath, out _);

    public static bool RestorePresets(string iniPath, out string summary)
    {
        try
        {
            string relocated = RlPathResolver.RelocateOffOneDriveIfNeeded(iniPath);
            if (!string.Equals(relocated, iniPath, StringComparison.OrdinalIgnoreCase))
                iniPath = relocated;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Relocate OneDrive: " + ex.Message);
        }

        bool ok = CleanGarageReset(iniPath, out summary, reapplyMode: true);
        if (ok)
        {
            try
            {
                Directory.CreateDirectory(AppMeta.GuttyDir);
                File.WriteAllText(
                    Path.Combine(AppMeta.GuttyDir, CleanSaveTagName),
                    AppMeta.Version + Environment.NewLine + summary);
            }
            catch { }
        }
        AppMeta.Log("RESTAURAR-PRESETS: " + summary + (ok ? " OK" : " FALHOU"));
        return ok;
    }

    /// <summary>
    /// Backups .23–.30 estão contaminados. Vão para Deprecated e o DBE_Production fica vazio.
    /// </summary>
    public static bool CleanGarageReset(string iniPath, out string summary, bool reapplyMode)
    {
        var parts = new List<string>();
        foreach (var p in Process.GetProcessesByName("RocketLeague"))
        {
            try { p.Kill(entireProcessTree: true); }
            catch { try { p.Kill(); } catch { } }
        }
        for (int i = 0; i < 10 && Process.GetProcessesByName("RocketLeague").Length > 0; i++)
            Thread.Sleep(300);
        if (Process.GetProcessesByName("RocketLeague").Length > 0)
        {
            summary = "Rocket League ainda esta aberto. Feche o jogo e tente de novo.";
            return false;
        }
        parts.Add("RL fechado");

        try { VideoSettingsSync.StopExistingWatchers(); parts.Add("watcher parado"); }
        catch (Exception ex) { AppMeta.Log("Stop watcher no reset: " + ex.Message); }

        string deprecated = Path.Combine(AppMeta.BackupDir, "Deprecated_Pre25_0_31");
        RetireDirectory(PresetsRoot, deprecated, "Presets");
        RetireDirectory(BackupRoot, deprecated, "SaveDataEpic");
        parts.Add("backups antigos aposentados");

        string quarantine = Path.Combine(
            AppMeta.BackupDir,
            "Quarantine_CleanReset_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        int moved = 0;
        int stuck = 0;
        foreach (string dir in EnumerateProductionDirs(iniPath))
        {
            foreach (string file in Directory.GetFiles(dir, "*.save*"))
            {
                try
                {
                    Directory.CreateDirectory(quarantine);
                    string dest = Path.Combine(quarantine, Path.GetFileName(file));
                    if (File.Exists(dest))
                        dest = Path.Combine(quarantine, Path.GetFileName(dir) + "_" + Path.GetFileName(file));
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Move(file, dest);
                    moved++;
                }
                catch (Exception ex)
                {
                    stuck++;
                    AppMeta.Log("Save preso " + file + ": " + ex.Message);
                }
            }
            try
            {
                string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(dir));
                if (tagame is not null)
                {
                    string cache = Path.Combine(tagame, "Cache");
                    if (Directory.Exists(cache))
                    {
                        foreach (string f in Directory.EnumerateFiles(cache, "*", SearchOption.AllDirectories))
                        {
                            try { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); } catch { }
                        }
                    }
                }
            }
            catch { }
        }
        try { PurgeRlSettingsData(); } catch { }
        parts.Add($"saves locais movidos={moved}");
        if (stuck > 0)
        {
            summary = string.Join("; ", parts) + $"; {stuck} save(s) preso(s) — feche o jogo e tente de novo";
            return false;
        }
        parts.Add("DBE_Production vazia");

        if (reapplyMode)
        {
            string? mode = ModeDetect.Detect(iniPath);
            if (mode is "COMPLETO" or "CRIADOR" && File.Exists(iniPath))
            {
                bool iniOk = VideoSettingsSync.ReclampIni(iniPath, mode);
                parts.Add(iniOk ? "INI " + mode : "INI parcial");
            }
        }

        summary = string.Join("; ", parts);
        return true;
    }

    private static void RetireDirectory(string source, string destParent, string name)
    {
        if (!Directory.Exists(source)) return;
        try
        {
            Directory.CreateDirectory(destParent);
            string dest = Path.Combine(destParent, name);
            if (Directory.Exists(dest))
                dest = Path.Combine(destParent, name + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.Move(source, dest);
            AppMeta.Log("Aposentado: " + source + " -> " + dest);
        }
        catch (Exception ex)
        {
            AppMeta.Log("Nao aposentei " + source + ": " + ex.Message);
        }
    }

    private static IEnumerable<string> EnumerateProductionDirs(string iniPath)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
            set.Add(Path.GetFullPath(path));
        }

        Add(SaveDirFromIni(iniPath, epic: true));
        Add(SaveDirFromIni(iniPath, epic: false));

        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] docs =
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Path.Combine(profile, "Documents"),
            Path.Combine(profile, "Documentos"),
            Path.Combine(profile, "OneDrive", "Documents"),
            Path.Combine(profile, "OneDrive", "Documentos"),
        };
        foreach (string doc in docs)
        {
            Add(Path.Combine(doc, @"My Games\Rocket League\TAGame\SaveDataEpic\DBE_Production"));
            Add(Path.Combine(doc, @"My Games\Rocket League\TAGame\SaveData\DBE_Production"));
        }
        return set;
    }


    /// <summary>
    /// Apos sync de video: so devolve o Best se a conta live encolheu para stub
    /// (&lt; SoftGarageMin). Nao roda HealDamagedSlots em garagem grande: o jogador
    /// acabou de trocar carro/cor e o watcher nao pode cobrir isso com backup.
    /// </summary>
    public static int ReinforceGarageAfterVideoSync(string? iniPath)
    {
        if (iniPath is null) return 0;
        try
        {
            int n = RestoreCollapsedStubs(iniPath);
            if (n > 0)
                AppMeta.Log($"Sync video: stub restaurado={n}");
            return n;
        }
        catch (Exception ex)
        {
            AppMeta.Log("ReinforceGarageAfterVideoSync: " + ex.Message);
            return 0;
        }
    }

    /// <summary>
    /// REPAIR nos backups e no live antes de escolher qual garagem restaurar.
    /// Cada pasta entra uma vez. Subpastas de Quarantine entram tambem.
    /// </summary>
    private static void RepairBeforeRestore(string? epicDir, string? steamDir)
    {
        if (Process.GetProcessesByName("RocketLeague").Length > 0)
        {
            AppMeta.Log("REPAIR presets: RL aberto, backups ficam como estao.");
            return;
        }

        var dirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string? root in new[]
        {
            BestRoot,
            PresetsRoot,
            Path.Combine(AppMeta.BackupDir, "Quarantine"),
            epicDir,
            steamDir,
        })
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
                continue;
            foreach (string file in Directory.EnumerateFiles(root, "*.save", SearchOption.AllDirectories))
            {
                string? dir = Path.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir))
                    dirs.Add(dir);
            }
        }

        foreach (string dir in dirs)
            SaveVideoPatcher.PatchSaveDirectory(dir, "REPAIR");
    }

    /// <summary>Copia o Best por cima apenas de .save que encolheram abaixo do stub.</summary>
    private static int RestoreCollapsedStubs(string iniPath)
    {
        int n = 0;
        foreach (bool epic in new[] { true, false })
        {
            string? saveDir = SaveDirFromIni(iniPath, epic);
            if (saveDir is null || !Directory.Exists(saveDir)) continue;

            var groups = IntactGarageBackups();

            foreach (string livePath in Directory.EnumerateFiles(saveDir, "*.save"))
            {
                var live = new FileInfo(livePath);
                if (!live.Exists || live.Length <= 0 || live.Length >= SoftGarageMinBytes)
                    continue;
                if (!groups.TryGetValue(live.Name, out var best))
                    continue;
                if (best.Length < SoftGarageMinBytes)
                    continue;
                if (CopyToLive(best.FullName, livePath))
                {
                    n++;
                    AppMeta.Log($"Stub restaurado: {live.Name} {live.Length / 1024}KB -> {best.Length / 1024}KB");
                }
            }
        }
        return n;
    }

    /// <summary>
    /// Save regravado pela v25.0.22 perde a tinta. Com o jogo fechado, troca o
    /// arquivo live pela copia integra. REMOVER sozinho nao fazia isso.
    /// </summary>
    public static int RestorePaintIfDamaged(string? iniPath)
    {
        AppMeta.Log("Copia de backup para o save desligada (v25.0.31).");
        return 0;
    }

    public static int BackupGaragePresets(string? iniPath)
    {
        AppMeta.Log("Backup de .save desligado (v25.0.31).");
        return 0;
    }

    private static int SnapshotLiveGarage(string iniPath)
    {
        int n = 0;
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: true));
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: false));
        return n;
    }

    private static int BackupGarageFromDir(string? saveDir)
    {
        try
        {
            if (saveDir is null || !Directory.Exists(saveDir)) return 0;

            Directory.CreateDirectory(BackupRoot);
            Directory.CreateDirectory(PresetsRoot);
            Directory.CreateDirectory(BestRoot);
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            var heavy = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Exists && f.Length >= SoftGarageMinBytes && f.Length <= GarageMaxBytes)
                .OrderByDescending(f => f.Length)
                .ThenByDescending(f => f.LastWriteTimeUtc)
                .Take(16)
                .ToList();

            int n = 0;
            foreach (var fi in heavy)
            {
                if (ArchiveSave(fi, PresetsRoot, ts))
                    n++;

                // Cofre sticky: so sobe, nunca desce.
                if (UpdateBestVault(fi.Name, fi.FullName, fi.Length))
                    AppMeta.Log($"Best vault atualizado: {fi.Name} ({fi.Length / 1024}KB)");
            }

            if (n > 0)
                AppMeta.Log($"Backup garagem/presets: {n} save(s) ({ts}).");
            return n;
        }
        catch (Exception ex)
        {
            AppMeta.Log("BackupGarage: " + ex.Message);
            return 0;
        }
    }

    /// <summary>Atualiza Best/{conta}.save apenas se o novo for maior.</summary>
    public static bool UpdateBestVault(string accountFileName, string sourcePath, long sourceLength)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(accountFileName) || !File.Exists(sourcePath))
                return false;
            if (sourceLength < SoftGarageMinBytes || sourceLength > GarageMaxBytes)
                return false;

            Directory.CreateDirectory(BestRoot);
            string bestPath = Path.Combine(BestRoot, accountFileName);
            bool replaceDamaged = false;
            if (File.Exists(bestPath))
            {
                long bestLen = new FileInfo(bestPath).Length;
                if (sourceLength <= bestLen)
                {
                    if (!SaveGarageGuard.IsReserializeDamage(bestPath))
                        return false;
                    replaceDamaged = true;
                }
            }

            if (SaveGarageGuard.IsReserializeDamage(sourcePath))
            {
                AppMeta.Log($"Best ignorado (regravacao quebra preset): {accountFileName}");
                return false;
            }

            SafeCopy(sourcePath, bestPath, overwrite: true);
            if (replaceDamaged)
                AppMeta.Log($"Best trocado: save regravado saiu, entrou copia integra ({accountFileName}).");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("UpdateBestVault: " + ex.Message);
            return false;
        }
    }

    /// <summary>Varre backups antigos e enche o Best com o maior save de cada conta.</summary>
    public static int SeedBestVaultFromArchives()
    {
        int n = 0;
        try
        {
            Directory.CreateDirectory(BestRoot);
            var byAccount = new Dictionary<string, List<FileInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (string root in new[] { BackupRoot, PresetsRoot, Path.Combine(AppMeta.BackupDir, "Quarantine") })
            {
                if (!Directory.Exists(root)) continue;
                foreach (string path in Directory.EnumerateFiles(root, "*.save", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (string.Equals(Path.GetDirectoryName(path), BestRoot, StringComparison.OrdinalIgnoreCase))
                            continue;
                        var fi = new FileInfo(path);
                        if (fi.Length < SoftGarageMinBytes || fi.Length > GarageMaxBytes) continue;
                        var m = BackupName.Match(fi.Name);
                        string account = m.Success ? m.Groups[1].Value : fi.Name;
                        if (!byAccount.TryGetValue(account, out var list))
                        {
                            list = new List<FileInfo>();
                            byAccount[account] = list;
                        }
                        list.Add(fi);
                    }
                    catch { }
                }
            }

            foreach (var kv in byAccount)
            {
                var ranked = kv.Value
                    .OrderByDescending(f => f.Length)
                    .ThenByDescending(f => f.LastWriteTimeUtc)
                    .ToList();
                if (ranked.Count == 0) continue;

                string bestPath = Path.Combine(BestRoot, kv.Key);
                if (File.Exists(bestPath))
                {
                    var bestFi = new FileInfo(bestPath);
                    if (bestFi.Length >= ranked[0].Length
                        && !SaveGarageGuard.IsReserializeDamage(bestPath))
                        continue;
                }

                int damaged = 0;
                foreach (var fi in ranked)
                {
                    if (SaveGarageGuard.IsReserializeDamage(fi.FullName))
                    {
                        damaged++;
                        continue;
                    }

                    if (UpdateBestVault(kv.Key, fi.FullName, fi.Length))
                        n++;
                    break;
                }
                if (damaged > 0)
                    AppMeta.Log($"Backups regravados ignorados: {kv.Key} ({damaged}).");
            }
            if (n > 0) AppMeta.Log($"Best vault herdado de arquivos: {n}.");
        }
        catch (Exception ex)
        {
            AppMeta.Log("SeedBestVault: " + ex.Message);
        }
        return n;
    }

    public static (int files, int garage, long bytes) CountBackups()
    {
        int files = 0, garage = 0;
        long bytes = 0;
        foreach (string root in EnumerateBackupRoots())
        {
            if (!Directory.Exists(root)) continue;
            foreach (var f in Directory.EnumerateFiles(root, "*.save", SearchOption.AllDirectories))
            {
                try
                {
                    var fi = new FileInfo(f);
                    files++;
                    bytes += fi.Length;
                    if (fi.Length >= SoftGarageMinBytes) garage++;
                }
                catch { }
            }
        }
        return (files, garage, bytes);
    }

    private static IEnumerable<string> EnumerateBackupRoots()
    {
        yield return BestRoot;
        yield return BackupRoot;
        yield return PresetsRoot;
        string q = Path.Combine(AppMeta.BackupDir, "Quarantine");
        if (Directory.Exists(q))
            yield return q;
    }

    private static bool RestoreInto(string? saveDir, bool preferNewest, bool preferGarage, List<string>? parts = null, bool createIfMissing = true)
    {
        if (saveDir is null) return false;

        var groups = CollectBackupGroups();
        if (groups.Count == 0)
            return preferNewest ? false : QuarantineSaves(saveDir);

        try
        {
            if (!Directory.Exists(saveDir))
            {
                if (!createIfMissing)
                    return false;
                if (RlPathResolver.IsOneDrivePath(saveDir))
                {
                    AppMeta.Log("Recusa criar pasta de save no OneDrive: " + saveDir);
                    return false;
                }
                Directory.CreateDirectory(saveDir);
            }
            int restored = 0;
            long bytes = 0;
            int garageHits = 0;
            int keptIntact = 0;
            string tag = saveDir.Contains("SaveDataEpic", StringComparison.OrdinalIgnoreCase) ? "Epic" : "Steam";

            foreach (var g in groups)
            {
                if (!AccountFitsDir(g.Key, saveDir))
                    continue;
                FileInfo? pick = preferGarage
                    ? PickGaragePreferred(g)
                    : FirstIntact(g, preferNewest);

                if (pick is null)
                {
                    AppMeta.Log($"Sem save integro para {g.Key}.");
                    continue;
                }

                // Em modo presets, nao vale a pena repor stubs pequeninos por cima do live
                if (preferGarage && pick.Length < SoftGarageMinBytes)
                    continue;
                if (LooksCorruptHeader(pick))
                {
                    AppMeta.Log($"Save ignorado (header mau): {pick.Name}");
                    continue;
                }

                string dest = Path.Combine(saveDir, g.Key);
                // Live integro e do mesmo porte: um backup mais velho so traria
                // inventario/presets antigos por cima dos atuais.
                if (preferGarage && File.Exists(dest))
                {
                    var cur = new FileInfo(dest);
                    if (LiveHoldsUp(cur, pick))
                    {
                        keptIntact++;
                        AppMeta.Log($"Save live integro mantido: {g.Key} ({cur.Length / 1024}KB, {tag})");
                        continue;
                    }
                }
                if (!CopyToLive(pick.FullName, dest))
                    continue;

                restored++;
                bytes += pick.Length;
                if (pick.Length >= SoftGarageMinBytes) garageHits++;
                AppMeta.Log($"Save restaurado: {g.Key} <- {pick.Name} ({pick.Length / 1024}KB, {tag})");
            }

            if (restored > 0)
                parts?.Add($"{tag}:{restored} arquivos ({bytes / 1024}KB, {garageHits} garagem)");
            if (keptIntact > 0)
                parts?.Add($"{tag}:{keptIntact} integro(s) mantido(s)");

            return preferGarage ? garageHits > 0 || keptIntact > 0 : restored > 0;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao restaurar save: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Se a conta live esta pequena e existe Best/backup maior com o MESMO nome, forca a copia.
    /// </summary>
    private static int ReinforceLiveAccounts(string iniPath)
    {
        int n = 0;
        n += ReinforceDir(SaveDirFromIni(iniPath, epic: true));
        n += ReinforceDir(SaveDirFromIni(iniPath, epic: false));
        return n;
    }

    private static int ReinforceDir(string? saveDir)
    {
        if (saveDir is null) return 0;
        try
        {
            if (!Directory.Exists(saveDir))
                return 0;

            var groups = IntactGarageBackups();

            int n = 0;

            // Contas ja presentes no live
            foreach (string livePath in Directory.EnumerateFiles(saveDir, "*.save"))
            {
                var live = new FileInfo(livePath);
                string name = live.Name;
                if (!groups.TryGetValue(name, out var best))
                    continue;
                if (best.Length < SoftGarageMinBytes)
                    continue;
                if (LiveHoldsUp(live, best))
                    continue;

                if (CopyToLive(best.FullName, livePath))
                {
                    n++;
                    AppMeta.Log($"Reforco conta live: {name} {live.Length / 1024}KB -> {best.Length / 1024}KB");
                }
            }

            // Contas so no Best/backup que ainda nao existem no live
            foreach (var kv in groups)
            {
                if (kv.Value.Length < SoftGarageMinBytes) continue;
                if (!AccountFitsDir(kv.Key, saveDir)) continue;
                string dest = Path.Combine(saveDir, kv.Key);
                if (File.Exists(dest) && LiveHoldsUp(new FileInfo(dest), kv.Value))
                    continue;

                if (CopyToLive(kv.Value.FullName, dest))
                {
                    n++;
                    AppMeta.Log($"Injetado save ausente/fraco: {kv.Key} ({kv.Value.Length / 1024}KB)");
                }
            }

            return n;
        }
        catch (Exception ex)
        {
            AppMeta.Log("ReinforceDir: " + ex.Message);
            return 0;
        }
    }

    private static int RestoreBestVaultInto(string iniPath)
    {
        if (!Directory.Exists(BestRoot)) return 0;
        int n = 0;
        foreach (bool epic in new[] { true, false })
        {
            string? dir = SaveDirFromIni(iniPath, epic);
            if (dir is null) continue;
            if (!Directory.Exists(dir))
            {
                if (!epic || RlPathResolver.IsOneDrivePath(dir))
                    continue;
                Directory.CreateDirectory(dir);
            }
            try
            {
                foreach (string best in Directory.EnumerateFiles(BestRoot, "*.save"))
                {
                    var fi = new FileInfo(best);
                    if (fi.Length < SoftGarageMinBytes) continue;
                    if (SaveGarageGuard.IsReserializeDamage(fi.FullName))
                    {
                        AppMeta.Log($"Best ignorado na restauracao (regravacao): {fi.Name}");
                        continue;
                    }
                    if (!AccountFitsDir(fi.Name, dir)) continue;
                    string dest = Path.Combine(dir, fi.Name);
                    if (File.Exists(dest) && LiveHoldsUp(new FileInfo(dest), fi))
                        continue;
                    if (CopyToLive(best, dest))
                        n++;
                }
            }
            catch { }
        }
        return n;
    }

    private static Dictionary<string, FileInfo> IntactGarageBackups()
    {
        var map = new Dictionary<string, FileInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in CollectBackupGroups())
        {
            FileInfo? pick = PickGaragePreferred(g);
            if (pick is not null)
                map[g.Key] = pick;
        }
        return map;
    }

    private static FileInfo? FirstIntact(IGrouping<string, (string Orig, FileInfo File)> g, bool newest)
    {
        var ranked = g.Select(x => x.File)
            .Where(f => !LooksCorruptHeader(f) && !SaveGarageGuard.IsReserializeDamage(f.FullName));
        return newest
            ? ranked.OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault()
            : ranked.OrderBy(f => f.LastWriteTimeUtc).FirstOrDefault();
    }

    private static FileInfo? PickGaragePreferred(IGrouping<string, (string Orig, FileInfo File)> g)
    {
        // So copias integras. Entre as de porte parecido com a maior (>=85%), vence a
        // com mais presets e depois a mais recente — o maior absoluto pode ser de meses
        // atras e devolver inventario velho.
        var intact = g.Select(x => x.File)
            .Where(f => f.Length >= SoftGarageMinBytes && f.Length <= GarageMaxBytes
                        && !LooksCorruptHeader(f)
                        && !SaveGarageGuard.IsReserializeDamage(f.FullName))
            .ToList();
        if (intact.Count == 0)
            return null;
        long floor = intact.Max(f => f.Length) * 85 / 100;
        return intact
            .Where(f => f.Length >= floor)
            .OrderByDescending(f => SaveGarageGuard.LoadoutCount(f.FullName))
            .ThenByDescending(f => f.LastWriteTimeUtc)
            .ThenByDescending(f => IsBestVaultPath(f.FullName) ? 1 : 0)
            .First();
    }

    private static bool IsBestVaultPath(string path) =>
        path.IndexOf($"{Path.DirectorySeparatorChar}Best{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0
        || path.IndexOf("/Best/", StringComparison.OrdinalIgnoreCase) >= 0
        || path.IndexOf("\\Best\\", StringComparison.OrdinalIgnoreCase) >= 0;

    private static List<IGrouping<string, (string Orig, FileInfo File)>> CollectBackupGroups()
    {
        var all = new List<(string Orig, FileInfo File)>();
        foreach (string root in EnumerateBackupRoots())
        {
            if (!Directory.Exists(root)) continue;
            foreach (string path in Directory.EnumerateFiles(root, "*.save", SearchOption.AllDirectories))
            {
                try
                {
                    var fi = new FileInfo(path);
                    if (!fi.Exists || fi.Length <= 0) continue;
                    string name = fi.Name;
                    // Best vault guarda o nome original da conta (sem prefixo timestamp)
                    bool fromBest = string.Equals(fi.DirectoryName, BestRoot, StringComparison.OrdinalIgnoreCase);
                    if (fromBest)
                    {
                        all.Add((name, fi));
                        continue;
                    }

                    var m = BackupName.Match(name);
                    string orig = m.Success ? m.Groups[1].Value : name;
                    all.Add((orig, fi));
                }
                catch { }
            }
        }

        return all
            .GroupBy(x => x.Orig, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Copia para a pasta live do jogo com mtime = agora. File.Copy herda o mtime
    /// do backup; o RL/Epic cloud preferem o slot/copia mais novo e ignoravam o restauro.
    /// </summary>
    private static bool CopyToLive(string source, string dest)
    {
        if (!SafeCopy(source, dest, overwrite: true)) return false;
        try { File.SetLastWriteTimeUtc(dest, DateTime.UtcNow); } catch { }
        return true;
    }

    /// <summary>
    /// Live integro e do mesmo porte. Se o live e mais novo e ainda tem preset,
    /// o jogador pode ter apagado um no jogo: o backup antigo nao cobre.
    /// </summary>
    private static bool LiveHoldsUp(FileInfo live, FileInfo backup)
    {
        if (!live.Exists
            || live.Length < SoftGarageMinBytes
            || live.Length < backup.Length * 85 / 100
            || LooksCorruptHeader(live)
            || SaveGarageGuard.IsReserializeDamage(live.FullName))
            return false;
        int liveLoads = SaveGarageGuard.LoadoutCount(live.FullName);
        if (live.LastWriteTimeUtc >= backup.LastWriteTimeUtc && liveLoads > 0)
            return true;
        return liveLoads >= SaveGarageGuard.LoadoutCount(backup.FullName);
    }

    private static readonly Regex SlotSuffix = new(@"_\d+$", RegexOptions.Compiled);
    private static readonly Regex EpicId = new(@"^[0-9a-f]{32}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex SteamId = new(@"^\d{17}$", RegexOptions.Compiled);

    /// <summary>"abc_2.save" → "abc". Slots _1/_2/_3 sao a mesma conta.</summary>
    internal static string AccountKey(string fileName) =>
        SlotSuffix.Replace(Path.GetFileNameWithoutExtension(fileName), "");

    /// <summary>Id Epic (32 hex) so em SaveDataEpic; SteamID64 so em SaveData.</summary>
    internal static bool AccountFitsDir(string fileName, string saveDir)
    {
        string id = AccountKey(fileName);
        bool epicDir = saveDir.Contains("SaveDataEpic", StringComparison.OrdinalIgnoreCase);
        if (EpicId.IsMatch(id)) return epicDir;
        if (SteamId.IsMatch(id)) return !epicDir;
        return true;
    }

    /// <summary>
    /// Slot regravado (preset/tinta quebrados) recebe a copia integra da mesma conta:
    /// primeiro um slot live integro, senao o melhor backup.
    /// </summary>
    private static int HealDamagedSlots(string? saveDir, Dictionary<string, FileInfo>? backups = null)
    {
        if (saveDir is null || !Directory.Exists(saveDir)) return 0;
        int n = 0;
        try
        {
            var live = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Exists && f.Length >= SoftGarageMinBytes && f.Length <= GarageMaxBytes)
                .GroupBy(f => AccountKey(f.Name), StringComparer.OrdinalIgnoreCase);
            foreach (var acct in live)
            {
                var damaged = acct.Where(f => SaveGarageGuard.IsReserializeDamage(f.FullName)).ToList();
                if (damaged.Count == 0) continue;
                FileInfo? source = acct
                    .Where(f => !damaged.Contains(f) && !LooksCorruptHeader(f))
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .FirstOrDefault();
                if (source is null)
                {
                    backups ??= IntactGarageBackups();
                    source = backups
                        .Where(kv => string.Equals(AccountKey(kv.Key), acct.Key, StringComparison.OrdinalIgnoreCase))
                        .Select(kv => kv.Value)
                        .OrderByDescending(f => f.LastWriteTimeUtc)
                        .FirstOrDefault();
                }
                if (source is null)
                {
                    AppMeta.Log($"Slot regravado sem copia integra: {acct.Key} ({damaged.Count})");
                    continue;
                }
                foreach (var d in damaged)
                {
                    if (CopyToLive(source.FullName, d.FullName))
                    {
                        n++;
                        AppMeta.Log($"Slot regravado reposto: {d.Name} <- {source.Name} ({source.Length / 1024}KB)");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AppMeta.Log("HealDamagedSlots: " + ex.Message);
        }
        return n;
    }

    /// <summary>Pasta esvaziada pela limpeza: devolve so a copia que ainda tem tinta.</summary>
    private static int RestoreMissingIntact(string? saveDir)
    {
        if (saveDir is null || !Directory.Exists(saveDir)) return 0;
        int n = 0;
        try
        {
            foreach (var kv in IntactGarageBackups())
            {
                if (!AccountFitsDir(kv.Key, saveDir)) continue;
                if (SaveGarageGuard.IsReserializeDamage(kv.Value.FullName)) continue;
                string dest = Path.Combine(saveDir, kv.Key);
                if (File.Exists(dest)) continue;
                if (CopyToLive(kv.Value.FullName, dest))
                {
                    n++;
                    AppMeta.Log($"Garagem devolvida: {kv.Key} ({kv.Value.Length / 1024}KB)");
                }
            }
        }
        catch (Exception ex)
        {
            AppMeta.Log("RestoreMissingIntact: " + ex.Message);
        }
        return n;
    }

    /// <summary>
    /// Arquiva so se o conteudo mudou desde o ultimo backup da mesma conta e poda
    /// o historico (8 mais recentes + 2 maiores). Antes cada fecho do RL duplicava
    /// todos os saves — GBs em poucas semanas.
    /// </summary>
    internal static bool ArchiveSave(FileInfo src, string root, string ts)
    {
        try
        {
            Directory.CreateDirectory(root);
            var history = Directory.EnumerateFiles(root, "*_" + src.Name)
                .Select(f => new FileInfo(f))
                .Where(f => BackupName.Match(f.Name) is { Success: true } m
                            && string.Equals(m.Groups[1].Value, src.Name, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => f.Name, StringComparer.Ordinal)
                .ToList();
            var last = history.FirstOrDefault();
            if (last is not null && last.Length == src.Length && SameBytes(last.FullName, src.FullName))
                return false;

            string dest = Path.Combine(root, $"{ts}_{src.Name}");
            if (File.Exists(dest) || !SafeCopy(src.FullName, dest, overwrite: false))
                return false;

            history.Insert(0, new FileInfo(dest));
            var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in history.Take(8)) keep.Add(f.FullName);
            foreach (var f in history.OrderByDescending(f => f.Length).Take(2)) keep.Add(f.FullName);
            foreach (var f in history.Where(f => !keep.Contains(f.FullName)))
            {
                try { f.Delete(); } catch { }
            }
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log($"ArchiveSave ({src.Name}): {ex.Message}");
            return false;
        }
    }

    private static int _pruned;

    /// <summary>
    /// Poda o historico herdado das versoes antigas (uma vez por processo, depois
    /// do Seed do Best). Best e Quarantine nao entram.
    /// </summary>
    private static void PruneArchivesOnce()
    {
        if (Interlocked.Exchange(ref _pruned, 1) == 1) return;
        long freed = 0;
        int removed = 0;
        foreach (string root in new[] { BackupRoot, PresetsRoot })
        {
            if (!Directory.Exists(root)) continue;
            var groups = Directory.EnumerateFiles(root, "*.save", SearchOption.TopDirectoryOnly)
                .Select(f => new FileInfo(f))
                .Select(f => (File: f, M: BackupName.Match(f.Name)))
                .Where(x => x.M.Success)
                .GroupBy(x => x.M.Groups[1].Value, StringComparer.OrdinalIgnoreCase);
            foreach (var g in groups)
            {
                var files = g.Select(x => x.File).OrderByDescending(f => f.Name, StringComparer.Ordinal).ToList();
                if (files.Count <= 13) continue;
                var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var f in files.Take(10)) keep.Add(f.FullName);
                foreach (var f in files.OrderByDescending(f => f.Length).Take(3)) keep.Add(f.FullName);
                foreach (var f in files.Where(f => !keep.Contains(f.FullName)))
                {
                    try
                    {
                        long len = f.Length;
                        f.Delete();
                        freed += len;
                        removed++;
                    }
                    catch { }
                }
            }
        }
        if (removed > 0)
            AppMeta.Log($"Backups antigos podados: {removed} arquivo(s), {freed / (1024 * 1024)} MB liberados.");
    }

    private static bool SameBytes(string a, string b)
    {
        try
        {
            byte[] x = File.ReadAllBytes(a);
            byte[] y = File.ReadAllBytes(b);
            return x.AsSpan().SequenceEqual(y);
        }
        catch { return false; }
    }

    private static bool SafeCopy(string source, string dest, bool overwrite)
    {
        try
        {
            string? dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrWhiteSpace(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(dest))
            {
                if (!overwrite) return false;
                try { File.SetAttributes(dest, FileAttributes.Normal); } catch { }
            }

            // Copia para temp na mesma pasta e depois replace — mais resistente a locks
            string tmp = dest + ".guttytmp";
            try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            File.Copy(source, tmp, true);
            try { File.SetAttributes(tmp, FileAttributes.Normal); } catch { }

            if (File.Exists(dest))
            {
                try { File.Replace(tmp, dest, null); }
                catch
                {
                    File.Copy(tmp, dest, true);
                    try { File.Delete(tmp); } catch { }
                }
            }
            else
            {
                File.Move(tmp, dest);
            }

            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log($"SafeCopy falhou ({Path.GetFileName(dest)}): {ex.Message}");
            try
            {
                File.Copy(source, dest, overwrite);
                return true;
            }
            catch (Exception ex2)
            {
                AppMeta.Log($"SafeCopy fallback falhou: {ex2.Message}");
                return false;
            }
        }
    }

    public static bool QuarantineSaves(string saveDir, bool promoteToBest = true)
    {
        try
        {
            if (!Directory.Exists(saveDir)) return true;

            var saves = Directory.EnumerateFiles(saveDir, "*.save").ToList();
            if (saves.Count == 0) return true;

            // Preserva grandes na Best antes de quarentenar (exceto heal LOAD FAILURE)
            if (promoteToBest)
            {
                foreach (string f in saves)
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        if (fi.Length >= SoftGarageMinBytes && !LooksCorruptHeader(fi))
                            UpdateBestVault(fi.Name, fi.FullName, fi.Length);
                    }
                    catch { }
                }
            }

            string q = Path.Combine(AppMeta.BackupDir, "Quarantine",
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(q);

            foreach (var f in saves)
            {
                string dest = Path.Combine(q, Path.GetFileName(f));
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(f, dest);
            }

            AppMeta.Log($"Saves movidos para quarentena ({saves.Count}): {q}");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha na quarentena de saves: " + ex.Message);
            return false;
        }
    }

    public static bool PurgeRlSettingsData()
    {
        try
        {
            string root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Rocket League", "datastorage");
            if (!Directory.Exists(root)) return true;

            int n = 0;
            foreach (var f in Directory.EnumerateFiles(root, "RLSettingsData", SearchOption.AllDirectories))
            {
                try { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); n++; } catch { }
            }

            AppMeta.Log($"RLSettingsData purgado ({n} arquivo(s)).");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao purgar RLSettingsData: " + ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Boot recovery: preserva Best, quarentena saves live e limpa cache.
    /// NAO reinsere Best/garagem — save grande corrompido e uma causa tipica de "jogo nao abre".
    /// O RL recria saves limpos na 1ª abertura; presets voltam via RestorePresets depois.
    /// </summary>
    public static bool UnbreakSaves(string iniPath)
    {
        try { BackupGaragePresets(iniPath); } catch { }

        bool epicQ = true;
        bool steamQ = true;
        string? epic = SaveDirFromIni(iniPath, epic: true);
        string? steam = SaveDirFromIni(iniPath, epic: false);
        if (epic is not null && Directory.Exists(epic))
            epicQ = QuarantineSaves(epic);
        if (steam is not null && Directory.Exists(steam))
            steamQ = QuarantineSaves(steam);

        bool purge = PurgeRlSettingsData();
        int cloudQ = QuarantineSteamCloudRemote();
        // A limpeza esvazia a pasta. Sem devolver a copia integra, o jogo cria
        // um save novo e a cor/preset somem. Save regravado nao volta.
        int back = RestorePaintIfDamaged(iniPath);
        AppMeta.Log($"UNBREAK-SAVES: epicQ={epicQ} steamQ={steamQ} purge={purge} steamCloud={cloudQ} devolvidos={back}");
        return epicQ && steamQ && purge;
    }

    /// <summary>Alias legado — mesma logica nuclear que UnbreakSaves (sem reforco Best).</summary>
    public static bool FullRecovery(string iniPath) => UnbreakSaves(iniPath);

    /// <summary>
    /// LOAD FAILURE (Save Data failed to load) — tipico Steam.
    /// LIMPEZA PURA: nao reinsere Best (era a causa de o aviso voltar).
    /// Fecha Steam, desliga Cloud no localconfig, quarentena SaveData + remote.
    /// Cliente: NEW SAVE (recomendado) ou DISABLE AUTOSAVE; tutorial e normal; inventario e online.
    /// </summary>
    public static bool HealLoadFailure(string iniPath, out string summary)
    {
        var parts = new List<string>();

        // Snapshot SEGURO: so copia para archive, sem promover suspeitos ao Best
        int archived = SnapshotLiveToQuarantineArchive(iniPath);
        if (archived > 0) parts.Add($"arquivo={archived}");

        ErrorRepair.ForceCloseRocketLeague();
        bool steamClosed = ForceCloseSteam();
        if (steamClosed) parts.Add("Steam fechado");
        Thread.Sleep(1500);

        string? steam = SaveDirFromIni(iniPath, epic: false);
        string? epic = SaveDirFromIni(iniPath, epic: true);

        // Wipe completo Steam DBE_Production (todos os ficheiros, nao so .save)
        int wipedSteam = WipeProductionDir(steam, "Steam");
        if (wipedSteam > 0) parts.Add($"Steam limpo={wipedSteam}");

        // Epic: so suspeitos — nao destruir conta Epic boa
        int badEpic = QuarantineSuspectSaves(epic, "Epic");
        if (badEpic > 0) parts.Add($"Epic suspeitos={badEpic}");

        int cloud = QuarantineSteamCloudRemote();
        if (cloud > 0) parts.Add($"Cloud remote={cloud}");

        int cloudOff = SoftDisableSteamCloudForRl();
        if (cloudOff > 0) parts.Add($"CloudEnabled=0 ({cloudOff})");

        bool purge = PurgeRlSettingsData();
        if (purge) parts.Add("cache limpo");

        // Limpa Cache local do TAGame (lixo de sync)
        int cacheN = PurgeTagameSaveCache(iniPath);
        if (cacheN > 0) parts.Add($"TAGame Cache={cacheN}");

        // NAO RestoreInto / Reinforce — Best podia ter o save corrompido.
        // Presets so depois: NEW SAVE / DISABLE → menu → RESTAURAR PRESETS.

        WriteSteamLoadFailureGuide();

        // Sucesso = pasta Steam vazia de .save
        bool steamClean = steam is null
            || !Directory.Exists(steam)
            || !Directory.EnumerateFiles(steam, "*.save").Any();
        if (steamClean) parts.Add("Steam DBE_Production limpo");
        else parts.Add("!! Steam ainda tem .save — fecha Steam e corre outra vez");

        bool ok = steamClean || wipedSteam > 0 || cloud > 0 || cloudOff > 0;
        summary = parts.Count > 0 ? string.Join("; ", parts) : "nada a reparar";
        AppMeta.Log("HEAL-LOAD-FAILURE: " + summary + (ok ? " OK" : " FALHOU"));
        return ok;
    }

    /// <summary>Copia live para Quarantine\Archive sem UpdateBestVault.</summary>
    private static int SnapshotLiveToQuarantineArchive(string iniPath)
    {
        int n = 0;
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        foreach (bool epic in new[] { true, false })
        {
            string? dir = SaveDirFromIni(iniPath, epic);
            if (dir is null || !Directory.Exists(dir)) continue;
            string destRoot = Path.Combine(AppMeta.BackupDir, "Quarantine", "Archive_" + stamp + (epic ? "_Epic" : "_Steam"));
            try
            {
                Directory.CreateDirectory(destRoot);
                foreach (string f in Directory.EnumerateFiles(dir, "*.save"))
                {
                    try
                    {
                        File.Copy(f, Path.Combine(destRoot, Path.GetFileName(f)), overwrite: true);
                        n++;
                    }
                    catch { }
                }
            }
            catch { }
        }
        return n;
    }

    private static int WipeProductionDir(string? saveDir, string tag)
    {
        if (saveDir is null || !Directory.Exists(saveDir)) return 0;
        try
        {
            var files = Directory.EnumerateFiles(saveDir, "*", SearchOption.TopDirectoryOnly).ToList();
            if (files.Count == 0) return 0;

            string q = Path.Combine(AppMeta.BackupDir, "Quarantine",
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + tag + "_wipe");
            Directory.CreateDirectory(q);
            int n = 0;
            foreach (string f in files)
            {
                try
                {
                    string dest = Path.Combine(q, Path.GetFileName(f));
                    if (File.Exists(dest)) File.Delete(dest);
                    File.SetAttributes(f, FileAttributes.Normal);
                    File.Move(f, dest);
                    n++;
                }
                catch
                {
                    try { File.Delete(f); n++; } catch { }
                }
            }

            AppMeta.Log($"Wipe {tag} DBE_Production: {n} -> {q}");
            return n;
        }
        catch (Exception ex)
        {
            AppMeta.Log("WipeProductionDir: " + ex.Message);
            return 0;
        }
    }

    private static int PurgeTagameSaveCache(string iniPath)
    {
        try
        {
            string? tagame = Path.GetDirectoryName(Path.GetDirectoryName(iniPath));
            if (tagame is null) return 0;
            string cache = Path.Combine(tagame, "Cache");
            if (!Directory.Exists(cache)) return 0;
            int n = 0;
            foreach (string f in Directory.EnumerateFiles(cache, "*", SearchOption.AllDirectories))
            {
                try { File.SetAttributes(f, FileAttributes.Normal); File.Delete(f); n++; } catch { }
            }
            return n;
        }
        catch { return 0; }
    }

    public static bool ForceCloseSteam()
    {
        bool any = false;
        string[] names = { "steam", "steamwebhelper", "steamservice" };
        try
        {
            foreach (string name in names)
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    any = true;
                    try { p.Kill(entireProcessTree: true); } catch { try { p.Kill(); } catch { } }
                }
            }
        }
        catch { }

        for (int i = 0; i < 10 && any; i++)
        {
            Thread.Sleep(400);
            if (Process.GetProcessesByName("steam").Length == 0)
                break;
            foreach (var p in Process.GetProcessesByName("steam"))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
            }
        }

        if (any) AppMeta.Log("Steam fechado para editar Cloud / userdata.");
        return any;
    }

    /// <summary>
    /// Força CloudEnabled=0 no localconfig.vdf de cada conta Steam (app 252950).
    /// Steam tem de estar fechado — senao regrava o ficheiro.
    /// </summary>
    public static int SoftDisableSteamCloudForRl()
    {
        int touched = 0;
        try
        {
            foreach (string steamRoot in EnumerateSteamRoots())
            {
                string userdata = Path.Combine(steamRoot, "userdata");
                if (!Directory.Exists(userdata)) continue;
                foreach (string userDir in Directory.EnumerateDirectories(userdata))
                {
                    string cfg = Path.Combine(userDir, "config", "localconfig.vdf");
                    if (!File.Exists(cfg)) continue;
                    if (PatchLocalConfigCloudEnabled(cfg))
                        touched++;
                }
            }
        }
        catch (Exception ex)
        {
            AppMeta.Log("SoftDisableSteamCloudForRl: " + ex.Message);
        }

        return touched;
    }

    private static bool PatchLocalConfigCloudEnabled(string localConfigPath)
    {
        try
        {
            string original = File.ReadAllText(localConfigPath);
            if (string.IsNullOrWhiteSpace(original)) return false;

            string bak = localConfigPath + ".guttybak";
            if (!File.Exists(bak))
                File.Copy(localConfigPath, bak, overwrite: false);

            // Ja desligado
            if (Regex.IsMatch(original, @"""252950""\s*\{[^\}]{0,800}?""CloudEnabled""\s*""0""", RegexOptions.Singleline | RegexOptions.IgnoreCase))
                return false;

            string text = original;
            // CloudEnabled=1 → 0 dentro do bloco 252950 (aproximacao)
            text = Regex.Replace(
                text,
                @"(""252950""\s*\{)(.*?)(""CloudEnabled""\s*"")(\d+)("")",
                m => m.Groups[1].Value + m.Groups[2].Value + m.Groups[3].Value + "0" + m.Groups[5].Value,
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (string.Equals(text, original, StringComparison.Ordinal))
            {
                // Bloco 252950 sem CloudEnabled — injeta apos a abertura
                text = Regex.Replace(
                    text,
                    @"""252950""\s*\{",
                    "\"252950\"\n\t\t\t\t{\n\t\t\t\t\t\"CloudEnabled\"\t\t\"0\"",
                    RegexOptions.IgnoreCase);
            }

            if (string.Equals(text, original, StringComparison.Ordinal))
                return false;

            File.WriteAllText(localConfigPath, text);
            AppMeta.Log("Steam Cloud OFF (252950): " + localConfigPath);
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("PatchLocalConfigCloudEnabled: " + ex.Message);
            return false;
        }
    }

    public static IReadOnlyList<string> AssessSaveHealth(string? iniPath)
    {
        var lines = new List<string>();
        string? steam = SaveDirFromIni(iniPath ?? "", epic: false);
        string? epic = SaveDirFromIni(iniPath ?? "", epic: true);

        void Score(string tag, string? dir)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                lines.Add($"{tag}: pasta ausente");
                return;
            }

            var files = Directory.EnumerateFiles(dir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Exists)
                .ToList();
            if (files.Count == 0)
            {
                lines.Add($"{tag}: 0 saves (limpo — no LOAD FAILURE usa NEW SAVE)");
                return;
            }

            int zero = files.Count(f => f.Length == 0);
            int tiny = files.Count(f => f.Length > 0 && f.Length < SoftGarageMinBytes);
            int garage = files.Count(f => f.Length >= SoftGarageMinBytes && f.Length <= GarageMaxBytes);
            int huge = files.Count(f => f.Length > GarageMaxBytes);
            int corruptHeader = files.Count(LooksCorruptHeader);
            int reserialize = files.Count(f => f.Length >= SoftGarageMinBytes && SaveGarageGuard.IsReserializeDamage(f.FullName));

            lines.Add($"{tag}: {files.Count} save(s) | garagem={garage} stub={tiny} zero={zero} enorme={huge}");
            if (reserialize > 0)
                lines.Add($"  !! {tag}: {reserialize} save(s) regravados — preset e tinta quebrados. RESTAURAR PRESETS com o jogo fechado");
            if (zero > 0 || corruptHeader > 0)
                lines.Add($"  !! {tag}: LOAD FAILURE provavel (zero={zero}, header mau={corruptHeader}) — use CORRIGIR SAVE");
            else if (tiny > 0 && garage == 0)
                lines.Add($"  !! {tag}: so stubs — Steam Cloud pode estar a esmagar a garagem");
            else if (garage > 0 && tiny == 0)
                lines.Add($"  {tag}: {garage} save(s) de garagem — o menu de video entra no .save, preset nao e regravado");
        }

        Score("Steam", steam);
        Score("Epic", epic);

        int cloudDirs = CountSteamCloudRemoteDirs();
        if (cloudDirs > 0)
            lines.Add($"Steam Cloud remote: {cloudDirs} pasta(s) userdata\\252950 — risco de conflito");
        else
            lines.Add("Steam Cloud remote: nao detectado / vazio");

        return lines;
    }

    private static int QuarantineSuspectSaves(string? saveDir, string tag)
    {
        if (saveDir is null || !Directory.Exists(saveDir)) return 0;
        try
        {
            var suspects = Directory.EnumerateFiles(saveDir, "*.save")
                .Select(f => new FileInfo(f))
                .Where(f => f.Exists && (f.Length == 0 || f.Length > GarageMaxBytes || LooksCorruptHeader(f)))
                .ToList();
            if (suspects.Count == 0) return 0;

            string q = Path.Combine(AppMeta.BackupDir, "Quarantine",
                DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + tag + "_suspect");
            Directory.CreateDirectory(q);
            int n = 0;
            foreach (var fi in suspects)
            {
                try
                {
                    string dest = Path.Combine(q, fi.Name);
                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(fi.FullName, dest);
                    n++;
                }
                catch { }
            }

            AppMeta.Log($"Suspect {tag}: {n} -> {q}");
            return n;
        }
        catch (Exception ex)
        {
            AppMeta.Log("QuarantineSuspectSaves: " + ex.Message);
            return 0;
        }
    }

    private static bool LooksCorruptHeader(FileInfo fi)
    {
        try
        {
            if (fi.Length == 0) return true;
            if (fi.Length < 64) return true;
            Span<byte> buf = stackalloc byte[16];
            using var fs = fi.OpenRead();
            int read = fs.Read(buf);
            if (read < 4) return true;
            // UE3 saves RL costumam comecar com bytes nao-ASCII lixo; 0x00*16 e tipico truncado
            bool allZero = true;
            for (int i = 0; i < read; i++)
            {
                if (buf[i] != 0) { allZero = false; break; }
            }
            return allZero;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>Rocket League Steam AppID 252950 — remote cloud cache em userdata.</summary>
    public static int QuarantineSteamCloudRemote()
    {
        int moved = 0;
        try
        {
            foreach (string remote in EnumerateSteamCloudRemoteDirs())
            {
                if (!Directory.Exists(remote)) continue;
                var files = Directory.EnumerateFiles(remote, "*", SearchOption.AllDirectories).ToList();
                if (files.Count == 0) continue;

                string q = Path.Combine(AppMeta.BackupDir, "Quarantine",
                    "SteamCloud_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Path.GetFileName(Path.GetDirectoryName(remote)));
                Directory.CreateDirectory(q);

                foreach (string f in files)
                {
                    try
                    {
                        string rel = Path.GetRelativePath(remote, f);
                        string dest = Path.Combine(q, rel);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(f, dest);
                        moved++;
                    }
                    catch { }
                }

                // remotecache.vdf ao lado de remote/
                string? appRoot = Path.GetDirectoryName(remote);
                if (appRoot is not null)
                {
                    string cache = Path.Combine(appRoot, "remotecache.vdf");
                    if (File.Exists(cache))
                    {
                        try
                        {
                            string dest = Path.Combine(q, "remotecache.vdf");
                            if (File.Exists(dest)) File.Delete(dest);
                            File.Move(cache, dest);
                            moved++;
                        }
                        catch { }
                    }
                }

                AppMeta.Log($"Steam Cloud remote quarentenado: {remote} ({files.Count} arquivos)");
            }
        }
        catch (Exception ex)
        {
            AppMeta.Log("QuarantineSteamCloudRemote: " + ex.Message);
        }

        return moved;
    }

    private static int CountSteamCloudRemoteDirs() =>
        EnumerateSteamCloudRemoteDirs().Count(Directory.Exists);

    private static IEnumerable<string> EnumerateSteamCloudRemoteDirs()
    {
        foreach (string steamRoot in EnumerateSteamRoots())
        {
            string userdata = Path.Combine(steamRoot, "userdata");
            if (!Directory.Exists(userdata)) continue;
            foreach (string userDir in Directory.EnumerateDirectories(userdata))
            {
                string remote = Path.Combine(userDir, "252950", "remote");
                yield return remote;
            }
        }
    }

    private static IEnumerable<string> EnumerateSteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            string? path = key?.GetValue("SteamPath") as string;
            if (!string.IsNullOrWhiteSpace(path))
                roots.Add(path.Replace('/', '\\'));
        }
        catch { }

        string[] guesses =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
            @"D:\Steam",
            @"E:\Steam",
        };
        foreach (string g in guesses)
            if (Directory.Exists(g)) roots.Add(g);

        return roots;
    }

    private static void WriteSteamLoadFailureGuide()
    {
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
                desktop = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            string path = Path.Combine(desktop, "GuttyTECH-RL-LOAD-FAILURE.txt");
            string text =
                "GUTTYTECH — LOAD FAILURE (Steam)" + Environment.NewLine +
                "=================================" + Environment.NewLine +
                "O Gutty FECHOU a Steam, desligou CloudEnabled no localconfig," + Environment.NewLine +
                "limpou SaveData\\DBE_Production e o remote Cloud (252950)." + Environment.NewLine +
                "NAO reinstalou presets nesta passagem (isso fazia o aviso voltar)." + Environment.NewLine +
                Environment.NewLine +
                "AGORA FAZ ISTO (ordem importa):" + Environment.NewLine +
                "1) Abre a Steam (Cloud do RL ja deve estar OFF — confirma em Propriedades)" + Environment.NewLine +
                "2) Abre o Rocket League" + Environment.NewLine +
                "3) Se aparecer LOAD FAILURE:" + Environment.NewLine +
                "   → clica NEW SAVE (recomendado pela Epic) " + Environment.NewLine +
                "     Rank/itens ONLINE nao se perdem. Tutorial as vezes aparece — normal." + Environment.NewLine +
                "   → DISABLE AUTOSAVE tambem entra, mas pode ir a tutorial OU ao menu" + Environment.NewLine +
                "     (depende se a conta ja tinha tutorial feito no servidor)." + Environment.NewLine +
                "   → RETRY nao resolve se o arquivo esta quebrado — ignore." + Environment.NewLine +
                "4) Fecha o RL > no Gutty: RESTAURAR PRESETS" + Environment.NewLine +
                "5) Abre OFFLINE 1x, confirma garagem, so depois reativa Steam Cloud" + Environment.NewLine +
                Environment.NewLine +
                "Backups: " + AppMeta.BackupDir + Environment.NewLine;

            File.WriteAllText(path, text);
            AppMeta.Log("Guia LOAD FAILURE: " + path);
        }
        catch (Exception ex)
        {
            AppMeta.Log("WriteSteamLoadFailureGuide: " + ex.Message);
        }
    }
}
