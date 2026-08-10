using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>Formata notas da release no estilo changelog Discord (pt-BR, legível).</summary>
internal static class ReleaseNotesFormatter
{
    public static string FormatForUi(string? body, string? tag, string? releaseName)
    {
        string version = string.IsNullOrWhiteSpace(tag) ? AppMeta.Version : tag.Trim();
        if (!version.StartsWith('v') && !version.StartsWith('V'))
            version = "v" + version;

        var sb = new StringBuilder();
        sb.AppendLine("GUTTYTECH RL Optimizer " + version);
        if (!string.IsNullOrWhiteSpace(releaseName)
            && !releaseName.Contains(version, StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine(releaseName.Trim());
        }

        sb.AppendLine();

        string cleaned = CleanMarkdown(body);
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            sb.AppendLine("O que mudou:");
            sb.AppendLine("- Correções e melhorias nesta versão.");
            sb.AppendLine("- Baixe o .exe novo, feche este app e abra o arquivo do Desktop.");
            return sb.ToString().TrimEnd() + Environment.NewLine;
        }

        sb.Append(cleaned.TrimEnd());
        sb.AppendLine();
        return sb.ToString();
    }

    public static string CleanMarkdown(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "";

        string text = body.Replace("\r\n", "\n").Trim();
        var sb = new StringBuilder(text.Length);
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd();

            // Ignora HTML / imagens / links crus de badge
            if (line.StartsWith('<') || line.Contains("![", StringComparison.Ordinal))
                continue;

            // ## Título → Título:
            Match h = Regex.Match(line, @"^#{1,3}\s+(.*)$");
            if (h.Success)
            {
                string title = StripInline(h.Groups[1].Value).Trim();
                if (title.Length == 0) continue;
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(title + (title.EndsWith(':') ? "" : ":"));
                continue;
            }

            // --- separador
            if (Regex.IsMatch(line, @"^\s*-{3,}\s*$"))
            {
                sb.AppendLine();
                continue;
            }

            // Lista
            Match bullet = Regex.Match(line, @"^\s*[-*+]\s+(.*)$");
            if (bullet.Success)
            {
                sb.AppendLine("- " + StripInline(bullet.Groups[1].Value).Trim());
                continue;
            }

            Match numbered = Regex.Match(line, @"^\s*(\d+)[.)]\s+(.*)$");
            if (numbered.Success)
            {
                sb.AppendLine(numbered.Groups[1].Value + ". " + StripInline(numbered.Groups[2].Value).Trim());
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (sb.Length > 0 && !sb.ToString().EndsWith("\n\n", StringComparison.Ordinal))
                    sb.AppendLine();
                continue;
            }

            sb.AppendLine(StripInline(line).Trim());
        }

        return Regex.Replace(sb.ToString(), @"\n{3,}", "\n\n").Trim();
    }

    private static string StripInline(string line)
    {
        // `code` → code
        line = Regex.Replace(line, "`([^`]+)`", "$1");
        // **bold** / *italic*
        line = Regex.Replace(line, @"\*\*([^*]+)\*\*", "$1");
        line = Regex.Replace(line, @"\*([^*]+)\*", "$1");
        // [text](url) → text
        line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]+\)", "$1");
        return line;
    }
}
