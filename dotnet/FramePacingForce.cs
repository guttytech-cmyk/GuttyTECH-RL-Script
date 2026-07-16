using System.Text;

namespace GuttyRL;

/// <summary>Trava frame pacing em todas as secoes SystemSettings*.
/// Combo WaitForGPU/OneFrameThreadLag/AllowPerFrame* = False causa tela preta no RL.</summary>
internal static class FramePacingForce
{
    private static readonly Dictionary<string, string> Keys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["WaitForGPU"] = "True",
        ["OneFrameThreadLag"] = "True",
        ["AllowPerFrameSleep"] = "True",
        ["AllowPerFrameYield"] = "True",
        ["UncappedFramerate"] = "True",
        ["bSmoothFrameRate"] = "False",
    };

    public static string Apply(string iniText)
    {
        var sb = new StringBuilder();
        bool inSection = false;

        foreach (var raw in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw;
            if (line.StartsWith('['))
            {
                inSection = line.StartsWith("[SystemSettings", StringComparison.OrdinalIgnoreCase);
                sb.Append(line).Append("\r\n");
                continue;
            }

            if (!inSection || string.IsNullOrWhiteSpace(line) || line.StartsWith(';'))
            {
                sb.Append(line).Append("\r\n");
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq <= 0)
            {
                sb.Append(line).Append("\r\n");
                continue;
            }

            string key = line[..eq];
            if (Keys.TryGetValue(key, out string? val))
            {
                sb.Append(key).Append('=').Append(val).Append("\r\n");
                continue;
            }

            sb.Append(line).Append("\r\n");
        }

        return sb.ToString();
    }
}
