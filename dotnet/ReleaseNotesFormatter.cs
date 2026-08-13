using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>
/// Formata notas da release para a UI WPF (texto limpo, sem Markdown).
/// </summary>
internal static class ReleaseNotesFormatter
{
    public static string FormatForUi(string? body, string? tag, string? releaseName)
    {
        string version = NormalizeVersionLabel(tag);
        string cleaned = ToUiStyle(body, version);

        if (!string.IsNullOrWhiteSpace(cleaned))
            return StripMarkdown(cleaned).TrimEnd() + Environment.NewLine;

        // Fallback curto se a release nao tiver body.
        var sb = new StringBuilder();
        sb.AppendLine("GUTTYTECH RL Optimizer " + version);
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(releaseName))
            sb.AppendLine(releaseName.Trim());
        sb.AppendLine();
        sb.AppendLine("O que mudou:");
        sb.AppendLine("- Correções e melhorias nesta versão.");
        sb.AppendLine("- Baixe o .exe novo, feche este app e abra o arquivo do Desktop.");
        return sb.ToString();
    }

    /// <summary>Converte body GitHub → texto limpo para o popup.</summary>
    public static string ToUiStyle(string? body, string versionLabel)
    {
        if (string.IsNullOrWhiteSpace(body))
            return "";

        string text = body.Replace("\r\n", "\n").Trim();
        var sb = new StringBuilder(text.Length + 64);
        bool wroteHeader = false;
        bool skipDownloadBlock = false;

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd();
            string trimmed = line.Trim();

            if (trimmed.StartsWith('<') || trimmed.Contains("![", StringComparison.Ordinal))
                continue;

            // Pula bloco de download / link cru (o app ja baixa).
            if (Regex.IsMatch(trimmed, @"^\*{0,2}Download\*{0,2}\s*:?\s*$", RegexOptions.IgnoreCase)
                || Regex.IsMatch(trimmed, @"^#{1,3}\s*Download", RegexOptions.IgnoreCase)
                || trimmed.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
            {
                skipDownloadBlock = true;
                continue;
            }

            if (skipDownloadBlock)
            {
                if (trimmed.StartsWith('#') || trimmed.StartsWith("**") || string.IsNullOrWhiteSpace(trimmed))
                {
                    if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        skipDownloadBlock = false;
                    else
                        continue;
                }
                else
                    continue;
            }

            // ## / ### Titulo → Titulo:
            Match h = Regex.Match(trimmed, @"^#{1,3}\s+(.*)$");
            if (h.Success)
            {
                string title = SoftInline(h.Groups[1].Value).Trim().Trim('*');
                if (title.Length == 0) continue;

                if (Regex.IsMatch(title, @"^(GUTTYTECH|v?\d+\.\d+)", RegexOptions.IgnoreCase))
                {
                    if (!wroteHeader)
                    {
                        sb.AppendLine("GUTTYTECH RL Optimizer " + versionLabel);
                        sb.AppendLine();
                        wroteHeader = true;
                    }

                    continue;
                }

                title = HumanizeSection(title);
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine(title);
                continue;
            }

            if (Regex.IsMatch(trimmed, @"^\s*-{3,}\s*$"))
            {
                sb.AppendLine();
                continue;
            }

            Match bullet = Regex.Match(trimmed, @"^[-*+]\s+(.*)$");
            if (bullet.Success)
            {
                EnsureHeader(sb, versionLabel, ref wroteHeader);
                sb.AppendLine("• " + SoftInline(bullet.Groups[1].Value).Trim());
                continue;
            }

            Match numbered = Regex.Match(trimmed, @"^(\d+)[.)]\s+(.*)$");
            if (numbered.Success)
            {
                EnsureHeader(sb, versionLabel, ref wroteHeader);
                sb.AppendLine(numbered.Groups[1].Value + ". " + SoftInline(numbered.Groups[2].Value).Trim());
                continue;
            }

            if (string.IsNullOrWhiteSpace(trimmed))
            {
                if (sb.Length > 0 && !sb.ToString().EndsWith("\n\n", StringComparison.Ordinal))
                    sb.AppendLine();
                continue;
            }

            EnsureHeader(sb, versionLabel, ref wroteHeader);

            // Titulo Discord (**O que mudou:**) → sem asteriscos
            if (trimmed.StartsWith("**", StringComparison.Ordinal))
            {
                if (sb.Length > 0 && !sb.ToString().EndsWith("\n\n", StringComparison.Ordinal)
                    && !sb.ToString().EndsWith("\n", StringComparison.Ordinal))
                    sb.AppendLine();
                sb.AppendLine(SoftInline(trimmed));
                continue;
            }

            sb.AppendLine(SoftInline(trimmed));
        }

        if (!wroteHeader && sb.Length > 0)
        {
            var withHeader = new StringBuilder();
            withHeader.AppendLine("GUTTYTECH RL Optimizer " + versionLabel);
            withHeader.AppendLine();
            withHeader.Append(sb);
            return Regex.Replace(withHeader.ToString(), @"\n{3,}", "\n\n").Trim();
        }

        return Regex.Replace(sb.ToString(), @"\n{3,}", "\n\n").Trim();
    }

    /// <summary>Compat: nome antigo usado em testes/chamadas.</summary>
    public static string ToDiscordStyle(string? body, string versionLabel) =>
        ToUiStyle(body, versionLabel);

    /// <summary>Remove Markdown residual (**bold**, `code`, links, etc.).</summary>
    public static string StripMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // **bold** / __bold__
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");
        text = Regex.Replace(text, @"__(.+?)__", "$1");
        // `code`
        text = Regex.Replace(text, "`([^`]+)`", "$1");
        // [text](url) → text
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]+\)", "$1");
        // # headers sobrando
        text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);
        // Asteriscos/underscores soltos de bold quebrado
        text = Regex.Replace(text, @"\*{1,2}", "");
        return text;
    }

    private static void EnsureHeader(StringBuilder sb, string versionLabel, ref bool wroteHeader)
    {
        if (wroteHeader) return;
        sb.AppendLine("GUTTYTECH RL Optimizer " + versionLabel);
        sb.AppendLine();
        wroteHeader = true;
    }

    private static string HumanizeSection(string title)
    {
        string t = title.Trim().TrimEnd(':');
        if (t.Equals("Corrigido", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Fixed", StringComparison.OrdinalIgnoreCase))
            return "O que foi corrigido:";
        if (t.Equals("Melhorado", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Alterado", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Changed", StringComparison.OrdinalIgnoreCase))
            return "O que melhorou:";
        if (t.Equals("Novo", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Added", StringComparison.OrdinalIgnoreCase))
            return "O que mudou:";
        if (t.Equals("Summary", StringComparison.OrdinalIgnoreCase))
            return "Resumo:";
        if (t.Equals("Test plan", StringComparison.OrdinalIgnoreCase)
            || t.Equals("Pra testar", StringComparison.OrdinalIgnoreCase))
            return "Como testar:";
        if (!t.EndsWith(':'))
            t += ":";
        return t;
    }

    private static string SoftInline(string line)
    {
        line = Regex.Replace(line, "`([^`]+)`", "$1");
        line = Regex.Replace(line, @"\[([^\]]+)\]\([^)]+\)", "$1");
        line = Regex.Replace(line, @"\*\*(.+?)\*\*", "$1");
        line = Regex.Replace(line, @"__(.+?)__", "$1");
        return line.Trim();
    }

    private static string NormalizeVersionLabel(string? tag)
    {
        string version = string.IsNullOrWhiteSpace(tag) ? AppMeta.Version : tag.Trim();
        if (!version.StartsWith('v') && !version.StartsWith('V'))
            version = "v" + version;
        return version;
    }
}
