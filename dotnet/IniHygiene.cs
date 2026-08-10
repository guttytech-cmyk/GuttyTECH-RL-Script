using System.Text;

namespace GuttyRL;

/// <summary>Limpeza de INI UE3: dedupe por secao e detecao de inchaco.</summary>
internal static class IniHygiene
{
    /// <summary>Acima disto o reclamp prefere reescrever do template (stock RL ~100KB+).</summary>
    public const int SoftBloatBytes = 64_000;

    private static readonly string[] DisplayKeys =
    {
        "ResX", "ResY", "Fullscreen", "Borderless", "AutoDetectDesktopResolution",
    };

    /// <summary>
    /// Dentro de cada secao, remove chaves repetidas (mantem a ultima — UE3 usa a ultima).
    /// </summary>
    public static string DeduplicateSectionKeys(string iniText)
    {
        var sb = new StringBuilder(iniText.Length);
        var sectionLines = new List<string>();

        void FlushSection()
        {
            if (sectionLines.Count == 0) return;
            var drop = new HashSet<int>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // Reverso: mantem a ultima ocorrencia (UE3).
            for (int i = sectionLines.Count - 1; i >= 0; i--)
            {
                string line = sectionLines[i];
                if (line.Length == 0 || line[0] == ';' || line[0] == '[')
                    continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line[..eq];
                if (!seen.Add(key))
                    drop.Add(i);
            }

            for (int i = 0; i < sectionLines.Count; i++)
            {
                if (drop.Contains(i)) continue;
                sb.Append(sectionLines[i]).Append("\r\n");
            }

            sectionLines.Clear();
        }

        foreach (string raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('['))
            {
                FlushSection();
                sectionLines.Add(raw);
                continue;
            }

            sectionLines.Add(raw);
        }

        FlushSection();
        return sb.ToString().TrimEnd('\r', '\n') + "\r\n";
    }

    public static int CountDuplicateKeyLines(string iniText)
    {
        int dups = 0;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('['))
            {
                seen.Clear();
                continue;
            }

            if (raw.Length == 0 || raw[0] == ';') continue;
            int eq = raw.IndexOf('=');
            if (eq <= 0) continue;
            string key = raw[..eq];
            if (!seen.Add(key))
                dups++;
        }

        return dups;
    }

    public static Dictionary<string, string> ReadDisplay(string iniPath)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ResX"] = "1920",
            ["ResY"] = "1080",
            ["Fullscreen"] = "True",
            ["Borderless"] = "False",
            ["AutoDetectDesktopResolution"] = "False",
        };
        try
        {
            if (!File.Exists(iniPath)) return map;
            bool inMain = false;
            foreach (string raw in File.ReadLines(iniPath))
            {
                if (raw.StartsWith('['))
                {
                    inMain = raw.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inMain) continue;
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw[..eq];
                if (map.ContainsKey(key))
                    map[key] = raw[(eq + 1)..].Trim();
            }
        }
        catch { }

        return map;
    }

    public static string ApplyDisplay(string templateText, Dictionary<string, string> disp)
    {
        var sb = new StringBuilder(templateText.Length + 64);
        bool inMain = false;
        var pending = new HashSet<string>(DisplayKeys, StringComparer.OrdinalIgnoreCase);

        foreach (string raw in templateText.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('['))
            {
                if (inMain)
                {
                    foreach (string key in pending)
                    {
                        if (disp.TryGetValue(key, out string? val))
                            sb.Append(key).Append('=').Append(val).Append("\r\n");
                    }

                    pending.Clear();
                }

                inMain = raw.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
                sb.Append(raw).Append("\r\n");
                continue;
            }

            if (inMain)
            {
                int eq = raw.IndexOf('=');
                if (eq > 0)
                {
                    string key = raw[..eq];
                    if (disp.TryGetValue(key, out string? val))
                    {
                        sb.Append(key).Append('=').Append(val).Append("\r\n");
                        pending.Remove(key);
                        continue;
                    }
                }
            }

            sb.Append(raw).Append("\r\n");
        }

        return sb.ToString().TrimEnd('\r', '\n') + "\r\n";
    }

    /// <summary>Template limpo + display do INI live + Force do modo.</summary>
    public static string RebuildModeFromTemplate(string iniPath, string mode)
    {
        var disp = ReadDisplay(iniPath);
        string template = mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
            ? Templates.Completo
            : Templates.Criador;
        string content = ApplyDisplay(template, disp);
        content = mode.Equals("COMPLETO", StringComparison.OrdinalIgnoreCase)
            ? CompletoForce.Apply(content)
            : CriadorForce.Apply(content);
        return DeduplicateSectionKeys(content);
    }
}
