namespace GuttyRL;

/// <summary>
/// Regras puras de caminho do RL (sem disco/Win32).
/// Caso MENĐONÇA: perfil atual com RL no OneDrive + perfil Windows antigo
/// (Gustavo) com Documentos local — o restore nao pode preferir o perfil errado.
/// </summary>
internal static class RlPathPolicy
{
    public static bool IsOneDrivePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return path.IndexOf("OneDrive", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static bool IsUnderProfile(string? path, string userProfile)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(userProfile))
            return false;

        string p = NormalizeRoot(path);
        string u = NormalizeRoot(userProfile);
        return p.Equals(u, StringComparison.OrdinalIgnoreCase)
            || p.StartsWith(u + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || p.StartsWith(u + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsForeignProfile(string? path, string userProfile) =>
        !string.IsNullOrWhiteSpace(path) && !IsUnderProfile(path, userProfile);

    public static bool IsPinnedUsable(string? pinned, string userProfile) =>
        !string.IsNullOrWhiteSpace(pinned)
        && !IsOneDrivePath(pinned)
        && IsUnderProfile(pinned, userProfile);

    /// <summary>
    /// So migra quando o INI do usuario atual esta no OneDrive.
    /// Perfil Windows alheio (mesmo com Documentos local) nao dispara
    /// unredirect — senao um PC com dois usuarios perdia a pasta Documentos.
    /// </summary>
    public static bool NeedsRelocation(string? iniPath, string userProfile) =>
        CanRelocateOffOneDrive(iniPath, userProfile);

    public static bool CanRelocateOffOneDrive(string? sourceIni, string userProfile) =>
        !string.IsNullOrWhiteSpace(sourceIni)
        && IsOneDrivePath(sourceIni)
        && IsUnderProfile(sourceIni, userProfile);

    public static bool ShouldScanOtherProfiles(IEnumerable<string> currentUserIniPaths) =>
        !currentUserIniPaths.Any(p => !string.IsNullOrWhiteSpace(p));

    public static int Score(string ini, string userProfile)
    {
        int score = 0;
        if (!IsOneDrivePath(ini)) score += 100;
        else score -= 80;
        if (IsUnderProfile(ini, userProfile)) score += 40;
        else score -= 50;
        return score;
    }

    public static IReadOnlyList<string> RankInis(
        IEnumerable<string> currentUserInis,
        IEnumerable<string> otherProfileInis,
        string userProfile)
    {
        var mine = currentUserInis.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        var list = new List<string>(mine);
        if (ShouldScanOtherProfiles(mine))
            list.AddRange(otherProfileInis.Where(p => !string.IsNullOrWhiteSpace(p)));

        return list
            .OrderByDescending(p => Score(p, userProfile))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static string? ChooseCopySourceIni(
        string chosenIni,
        IEnumerable<string> currentUserInis,
        string userProfile)
    {
        var mine = currentUserInis.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        string? onedrive = mine.FirstOrDefault(IsOneDrivePath);
        if (onedrive is not null) return onedrive;

        string? local = mine.FirstOrDefault(p => !IsOneDrivePath(p) && IsUnderProfile(p, userProfile));
        if (local is not null) return local;

        if (!string.IsNullOrWhiteSpace(chosenIni) && IsUnderProfile(chosenIni, userProfile))
            return chosenIni;

        return null;
    }

    public static string PickLocalDocuments(string userProfile, IEnumerable<string> candidates)
    {
        foreach (string raw in candidates)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            if (IsOneDrivePath(raw)) continue;
            if (!IsUnderProfile(raw, userProfile)) continue;
            return raw;
        }

        return Path.Combine(userProfile, "Documents");
    }

    private static string NormalizeRoot(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
