using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

internal sealed record ChangelogRelease(
    string Tag,
    string? Name,
    string? Body,
    bool Draft = false,
    bool Prerelease = false);

/// <summary>Junta as notas de todas as versões entre a instalada e a mais nova.</summary>
internal static class ChangelogRange
{
    public static IReadOnlyList<ChangelogRelease> SelectRange(
        IEnumerable<ChangelogRelease> releases,
        string afterVersion,
        string? untilVersion = null)
    {
        if (!TryParse(Normalize(afterVersion), out Version after))
            return [];

        Version? until = null;
        if (!string.IsNullOrWhiteSpace(untilVersion) && TryParse(Normalize(untilVersion), out Version untilParsed))
            until = untilParsed;

        var selected = new List<(ChangelogRelease Release, Version Version)>();
        foreach (ChangelogRelease release in releases)
        {
            if (release.Draft || release.Prerelease)
                continue;
            if (string.IsNullOrWhiteSpace(release.Tag))
                continue;
            if (!TryParse(Normalize(release.Tag), out Version version))
                continue;
            if (version <= after)
                continue;
            if (until is not null && version > until)
                continue;

            selected.Add((release, version));
        }

        return selected
            .OrderByDescending(item => item.Version)
            .Select(item => item.Release)
            .ToList();
    }

    public static string Format(IReadOnlyList<ChangelogRelease> selected)
    {
        if (selected.Count == 0)
            return "";

        var sb = new StringBuilder();
        foreach (ChangelogRelease release in selected)
        {
            if (sb.Length > 0)
                sb.AppendLine().AppendLine();

            string label = Label(release.Tag);
            sb.AppendLine(label);

            string section = ReleaseNotesFormatter.ToUiStyle(release.Body, release.Tag);
            section = ReleaseNotesFormatter.StripMarkdown(section);
            section = StripProductHeader(section);

            if (!string.IsNullOrWhiteSpace(section))
            {
                sb.AppendLine();
                sb.Append(section.Trim());
            }
        }

        return sb.ToString().Trim() + Environment.NewLine;
    }

    public static string Normalize(string tag)
    {
        tag = tag.Trim();
        if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            tag = tag[1..];
        return tag;
    }

    public static string Label(string tag)
    {
        string version = tag.Trim();
        if (!version.StartsWith('v') && !version.StartsWith('V'))
            version = "v" + version;
        return version;
    }

    internal static bool IsNewer(string latest, string current)
    {
        if (TryParse(Normalize(latest), out Version latestV) && TryParse(Normalize(current), out Version currentV))
            return latestV > currentV;

        return !string.Equals(Normalize(latest), Normalize(current), StringComparison.OrdinalIgnoreCase);
    }

    private static string StripProductHeader(string text)
    {
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        int start = 0;
        while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
            start++;

        if (start < lines.Length
            && lines[start].StartsWith("GUTTYTECH RL Optimizer", StringComparison.OrdinalIgnoreCase))
        {
            start++;
            while (start < lines.Length && string.IsNullOrWhiteSpace(lines[start]))
                start++;
        }

        return string.Join('\n', lines.Skip(start)).Trim();
    }

    private static bool TryParse(string text, out Version version)
    {
        Match match = Regex.Match(text, @"^\d+(\.\d+){0,3}");
        if (match.Success && Version.TryParse(match.Value, out version!))
            return true;

        version = new Version(0, 0);
        return false;
    }
}
