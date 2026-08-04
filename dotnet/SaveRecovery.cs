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

    public static bool RestoreSteamSave(string iniPath, bool preferNewest = false) =>
        RestoreInto(SaveDirFromIni(iniPath, epic: false), preferNewest, preferGarage: preferNewest);

    /// <summary>RESTAURAR PRESETS: prioriza saves grandes (garagem) de todos os backups.</summary>
    public static bool RestoreLatestBackup(string iniPath) =>
        RestorePresets(iniPath, out _);

    public static bool RestorePresets(string iniPath, out string summary)
    {
        var parts = new List<string>();

        // 0) Promove backups antigos grandes para o cofre Best (pcs que ja tinham historico)
        int seeded = SeedBestVaultFromArchives();
        if (seeded > 0) parts.Add($"best herdado={seeded}");

        // 1) Snapshot do que ainda esta live (pode ser a unica copia grande)
        int snapped = SnapshotLiveGarage(iniPath);
        if (snapped > 0) parts.Add($"snapshot live={snapped}");

        // 2) Restaura Epic + Steam a partir de todos os cofres
        bool epic = RestoreInto(SaveDirFromIni(iniPath, epic: true), preferNewest: true, preferGarage: true, parts);
        bool steam = RestoreInto(SaveDirFromIni(iniPath, epic: false), preferNewest: true, preferGarage: true, parts);

        // 3) Reforca contas live pequeninas com o Best sticky da mesma conta
        int reinforced = ReinforceLiveAccounts(iniPath);
        if (reinforced > 0) parts.Add($"reforco contas={reinforced}");

        // 4) Limpa cache Epic (cloud costuma regravar o menu) + Steam Cloud remote
        bool purge = PurgeRlSettingsData();
        if (purge) parts.Add("cache limpo");
        int cloud = QuarantineSteamCloudRemote();
        if (cloud > 0) parts.Add($"Steam Cloud remote={cloud}");

        // 5) 2o passe — cloud/Defender por vezes reescreve nos primeiros ms
        Thread.Sleep(700);
        int secondPass = ReinforceLiveAccounts(iniPath) + RestoreBestVaultInto(iniPath);
        if (secondPass > 0) parts.Add($"2o passe={secondPass}");

        bool ok = epic || steam || reinforced > 0 || secondPass > 0 || cloud > 0;
        summary = parts.Count > 0 ? string.Join("; ", parts) : "sem backups de garagem";
        AppMeta.Log("RESTAURAR-PRESETS: " + summary + (ok ? " OK" : " FALHOU"));
        return ok;
    }

    /// <summary>
    /// Apos patch de video: se a conta live ficou stub (&lt; SoftGarageMin),
    /// restaura o Best/backup de garagem — Apply nao deve apagar presets.
    /// </summary>
    public static int ReinforceGarageAfterVideoSync(string? iniPath)
    {
        if (iniPath is null) return 0;
        try
        {
            int n = ReinforceLiveAccounts(iniPath);
            n += RestoreBestVaultInto(iniPath);
            if (n > 0)
                AppMeta.Log($"Pos-sync video: reforco garagem/presets={n}");
            return n;
        }
        catch (Exception ex)
        {
            AppMeta.Log("ReinforceGarageAfterVideoSync: " + ex.Message);
            return 0;
        }
    }

    /// <summary>Copia saves de garagem (grandes) — so file copy, sem decrypt.</summary>
    public static int BackupGaragePresets(string? iniPath)
    {
        if (iniPath is null) return 0;
        try { SeedBestVaultFromArchives(); } catch { }
        int n = 0;
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: true));
        n += BackupGarageFromDir(SaveDirFromIni(iniPath, epic: false));
        return n;
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
                string name = $"{ts}_{fi.Name}";
                string destA = Path.Combine(BackupRoot, name);
                string destB = Path.Combine(PresetsRoot, name);
                if (!File.Exists(destA))
                {
                    SafeCopy(fi.FullName, destA, overwrite: false);
                    n++;
                }

                if (!File.Exists(destB))
                {
                    try { SafeCopy(fi.FullName, destB, overwrite: false); } catch { }
                }

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
            if (File.Exists(bestPath))
            {
                long bestLen = new FileInfo(bestPath).Length;
                if (sourceLength <= bestLen)
                    return false;
            }

            SafeCopy(sourcePath, bestPath, overwrite: true);
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
                        if (UpdateBestVault(account, fi.FullName, fi.Length))
                            n++;
                    }
                    catch { }
                }
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

    private static bool RestoreInto(string? saveDir, bool preferNewest, bool preferGarage, List<string>? parts = null)
    {
        if (saveDir is null) return false;

        var groups = CollectBackupGroups();
        if (groups.Count == 0)
            return preferNewest ? false : QuarantineSaves(saveDir);

        try
        {
            Directory.CreateDirectory(saveDir);
            int restored = 0;
            long bytes = 0;
            int garageHits = 0;
            string tag = saveDir.Contains("SaveDataEpic", StringComparison.OrdinalIgnoreCase) ? "Epic" : "Steam";

            foreach (var g in groups)
            {
                FileInfo pick = preferGarage
                    ? PickGaragePreferred(g)
                    : preferNewest
                        ? g.OrderByDescending(x => x.File.LastWriteTimeUtc).First().File
                        : g.OrderBy(x => x.File.LastWriteTimeUtc).First().File;

                // Em modo presets, nao vale a pena repor stubs pequeninos por cima do live
                if (preferGarage && pick.Length < SoftGarageMinBytes)
                    continue;
                if (LooksCorruptHeader(pick))
                {
                    AppMeta.Log($"Save ignorado (header mau): {pick.Name}");
                    continue;
                }

                string dest = Path.Combine(saveDir, g.Key);
                if (!SafeCopy(pick.FullName, dest, overwrite: true))
                    continue;

                restored++;
                bytes += pick.Length;
                if (pick.Length >= SoftGarageMinBytes) garageHits++;
                AppMeta.Log($"Save restaurado: {g.Key} <- {pick.Name} ({pick.Length / 1024}KB, {tag})");
            }

            if (restored > 0)
                parts?.Add($"{tag}:{restored} ficheiros ({bytes / 1024}KB, {garageHits} garagem)");

            return preferGarage ? garageHits > 0 : restored > 0;
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
                Directory.CreateDirectory(saveDir);

            var groups = CollectBackupGroups()
                .ToDictionary(g => g.Key, g => PickGaragePreferred(g), StringComparer.OrdinalIgnoreCase);

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
                if (live.Exists && live.Length >= best.Length)
                    continue;

                if (SafeCopy(best.FullName, livePath, overwrite: true))
                {
                    n++;
                    AppMeta.Log($"Reforco conta live: {name} {live.Length / 1024}KB -> {best.Length / 1024}KB");
                }
            }

            // Contas so no Best/backup que ainda nao existem no live
            foreach (var kv in groups)
            {
                if (kv.Value.Length < SoftGarageMinBytes) continue;
                string dest = Path.Combine(saveDir, kv.Key);
                if (File.Exists(dest))
                {
                    long cur = new FileInfo(dest).Length;
                    if (cur >= kv.Value.Length) continue;
                }

                if (SafeCopy(kv.Value.FullName, dest, overwrite: true))
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
            try
            {
                Directory.CreateDirectory(dir);
                foreach (string best in Directory.EnumerateFiles(BestRoot, "*.save"))
                {
                    var fi = new FileInfo(best);
                    if (fi.Length < SoftGarageMinBytes) continue;
                    string dest = Path.Combine(dir, fi.Name);
                    bool need = !File.Exists(dest) || new FileInfo(dest).Length < fi.Length;
                    if (need && SafeCopy(best, dest, overwrite: true))
                        n++;
                }
            }
            catch { }
        }
        return n;
    }

    private static FileInfo PickGaragePreferred(IGrouping<string, (string Orig, FileInfo File)> g)
    {
        // 1) Maior save de garagem valido, preferindo o Best vault e depois o mais recente
        var big = g.Where(x => x.File.Length >= SoftGarageMinBytes && !LooksCorruptHeader(x.File))
            .OrderByDescending(x => IsBestVaultPath(x.File.FullName) ? 1 : 0)
            .ThenByDescending(x => x.File.Length)
            .ThenByDescending(x => x.File.LastWriteTimeUtc)
            .Select(x => x.File)
            .FirstOrDefault();
        if (big is not null) return big;

        // 2) Sem garagem: o MAIOR disponivel nao-corrompido
        var any = g.Where(x => !LooksCorruptHeader(x.File))
            .OrderByDescending(x => x.File.Length)
            .ThenByDescending(x => x.File.LastWriteTimeUtc)
            .Select(x => x.File)
            .FirstOrDefault();
        return any ?? g.OrderByDescending(x => x.File.Length).First().File;
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
        AppMeta.Log($"UNBREAK-SAVES: epicQ={epicQ} steamQ={steamQ} purge={purge} steamCloud={cloudQ}");
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

            lines.Add($"{tag}: {files.Count} save(s) | garagem={garage} stub={tiny} zero={zero} enorme={huge}");
            if (zero > 0 || corruptHeader > 0)
                lines.Add($"  !! {tag}: LOAD FAILURE provavel (zero={zero}, header mau={corruptHeader}) — use CORRIGIR SAVE");
            else if (tiny > 0 && garage == 0)
                lines.Add($"  !! {tag}: so stubs — Steam Cloud pode estar a esmagar a garagem");
        }

        Score("Steam", steam);
        Score("Epic", epic);

        int cloudDirs = CountSteamCloudRemoteDirs();
        if (cloudDirs > 0)
            lines.Add($"Steam Cloud remote: {cloudDirs} pasta(s) userdata\\252950 — risco de conflito");
        else
            lines.Add("Steam Cloud remote: nao detetado / vazio");

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

                AppMeta.Log($"Steam Cloud remote quarentenado: {remote} ({files.Count} ficheiros)");
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
                "   → RETRY nao resolve se o ficheiro esta partido — ignora." + Environment.NewLine +
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
