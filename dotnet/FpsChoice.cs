using System.Globalization;

namespace GuttyRL;

/// <summary>
/// Limite de FPS que o jogador escolhe no menu do Rocket League (ex.: 360).
/// Abaixo de 120 é o padrão do jogo (30/60) que o modo levanta para ilimitado.
/// 10000 é o nosso ilimitado, não um limite.
/// </summary>
internal static class FpsChoice
{
    public const int MinCap = 120;
    public const int MaxCap = 960;

    public static bool IsUserCap(string? uncapped, string? customFps, out int cap)
    {
        cap = 0;
        if (string.IsNullOrWhiteSpace(customFps))
            return false;
        if (!double.TryParse(customFps.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
            return false;
        int n = (int)Math.Round(v);
        // O template injeta UncappedFramerate=True com CustomFPS=0.
        // 144/240/360 escrito no INI e limite do jogador, mesmo se Uncapped ainda estiver True.
        if (n == 0 && uncapped is not null && uncapped.Equals("True", StringComparison.OrdinalIgnoreCase))
            return false;
        if (n < MinCap || n > MaxCap)
            return false;
        cap = n;
        return true;
    }

    public static bool TryRead(string? iniText, out int cap)
    {
        cap = 0;
        if (string.IsNullOrEmpty(iniText))
            return false;
        string? uncapped = null;
        string? custom = null;
        bool inMain = false;
        foreach (string raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('['))
            {
                if (inMain)
                    break;
                inMain = raw.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
                continue;
            }
            if (!inMain)
                continue;
            int eq = raw.IndexOf('=');
            if (eq <= 0)
                continue;
            string key = raw[..eq].Trim();
            string val = raw[(eq + 1)..].Trim();
            if (key.Equals("UncappedFramerate", StringComparison.OrdinalIgnoreCase))
                uncapped = val;
            else if (key.Equals("CustomFPS", StringComparison.OrdinalIgnoreCase))
                custom = val;
        }
        return IsUserCap(uncapped, custom, out cap);
    }

    /// <summary>Grava o limite em todas as seções SystemSettings. O resto do texto fica.</summary>
    public static string Stamp(string ini, int cap)
    {
        var sb = new System.Text.StringBuilder(ini.Length + 32);
        bool inSs = false;
        string fps = cap.ToString(CultureInfo.InvariantCulture);
        string[] lines = ini.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            if (i == lines.Length - 1 && raw.Length == 0)
                break;
            if (raw.StartsWith('['))
            {
                inSs = raw.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase);
                sb.Append(raw).Append("\r\n");
                continue;
            }
            if (inSs)
            {
                int eq = raw.IndexOf('=');
                if (eq > 0)
                {
                    string key = raw[..eq];
                    if (key.Equals("UncappedFramerate", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append("UncappedFramerate=False\r\n");
                        continue;
                    }
                    if (key.Equals("CustomFPS", StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append("CustomFPS=").Append(fps).Append("\r\n");
                        continue;
                    }
                }
            }
            sb.Append(raw).Append("\r\n");
        }
        return sb.ToString();
    }
}
