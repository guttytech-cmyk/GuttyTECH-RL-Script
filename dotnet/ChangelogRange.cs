using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

internal sealed record ChangelogRelease(
    string Tag,
    string? Name,
    string? Body,
    bool Draft = false,
    bool Prerelease = false);

internal sealed class ChangelogSectionCard
{
    public string Heading { get; init; } = "";
    public IReadOnlyList<string> Lines { get; init; } = [];
}

internal sealed class ChangelogVersionCard
{
    public string VersionLabel { get; init; } = "";
    public bool IsLatest { get; init; }
    public IReadOnlyList<ChangelogSectionCard> Sections { get; init; } = [];
}

internal sealed class ChangelogWindowModel
{
    public string TitleText { get; init; } = "O QUE MUDOU";
    public string SubtitleText { get; init; } = "";
    public IReadOnlyList<ChangelogVersionCard> Versions { get; init; } = [];
}

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

    public static IReadOnlyList<ChangelogVersionCard> BuildCards(IReadOnlyList<ChangelogRelease> selected)
    {
        var cards = new List<ChangelogVersionCard>(selected.Count);
        for (int i = 0; i < selected.Count; i++)
        {
            ChangelogRelease release = selected[i];
            string body = ReleaseNotesFormatter.ToUiStyle(release.Body, release.Tag);
            body = ReleaseNotesFormatter.StripMarkdown(body);
            body = StripProductHeader(body);
            cards.Add(new ChangelogVersionCard
            {
                VersionLabel = Label(release.Tag),
                IsLatest = i == 0,
                Sections = ParseSections(body),
            });
        }

        return cards;
    }

    public static IReadOnlyList<ChangelogVersionCard> ParseCards(string notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
            return [];

        var cards = new List<ChangelogVersionCard>();
        MatchCollection headers = Regex.Matches(notes, @"(?m)^(v\d+(?:\.\d+)+)\s*$");
        if (headers.Count == 0)
        {
            IReadOnlyList<ChangelogSectionCard> sections = ParseSections(notes);
            if (sections.Count == 0)
                return [];

            cards.Add(new ChangelogVersionCard
            {
                VersionLabel = "O que mudou",
                IsLatest = true,
                Sections = sections,
            });
            return cards;
        }

        for (int i = 0; i < headers.Count; i++)
        {
            int start = headers[i].Index + headers[i].Length;
            int end = i + 1 < headers.Count ? headers[i + 1].Index : notes.Length;
            string body = notes[start..end].Trim();
            cards.Add(new ChangelogVersionCard
            {
                VersionLabel = Label(headers[i].Groups[1].Value),
                IsLatest = i == 0,
                Sections = ParseSections(body),
            });
        }

        return cards;
    }

    private static IReadOnlyList<ChangelogSectionCard> ParseSections(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return [];

        var sections = new List<ChangelogSectionCard>();
        string heading = "";
        var lines = new List<string>();

        void Flush()
        {
            if (lines.Count == 0 && string.IsNullOrWhiteSpace(heading))
                return;
            sections.Add(new ChangelogSectionCard
            {
                Heading = string.IsNullOrWhiteSpace(heading) ? "O que mudou:" : heading,
                Lines = lines.ToList(),
            });
            heading = "";
            lines = new List<string>();
        }

        foreach (string raw in body.Replace("\r\n", "\n").Split('\n'))
        {
            string trimmed = raw.Trim();
            if (trimmed.Length == 0)
                continue;

            if (trimmed.StartsWith("• ", StringComparison.Ordinal)
                || trimmed.StartsWith("- ", StringComparison.Ordinal)
                || trimmed.StartsWith("— ", StringComparison.Ordinal))
            {
                lines.Add(trimmed[2..].Trim());
                continue;
            }

            if (trimmed.EndsWith(':') && trimmed.Length <= 48 && !trimmed.Contains("  ", StringComparison.Ordinal))
            {
                Flush();
                heading = trimmed;
                continue;
            }

            lines.Add(trimmed);
        }

        Flush();
        return sections;
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
