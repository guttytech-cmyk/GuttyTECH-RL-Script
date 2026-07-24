using System.Diagnostics;
using System.Text;

namespace GuttyRL;

/// <summary>Patch .save Epic (VideoSettingsSavePC + EffectIntensity) via runtime Python embutido.</summary>
internal static class SaveVideoPatcher
{
    public static bool PatchSaveDirectory(string saveDir, string mode, Action<string>? progress = null)
    {
        if (!Directory.Exists(saveDir))
        {
            AppMeta.Log("Save Epic nao encontrado; pulando patch de video.");
            return true;
        }

        if (!EmbeddedRuntime.EnsureReady(out string pythonExe, out string toolsDir))
        {
            AppMeta.Log("Falha ao preparar patch de save embutido no exe.");
            return false;
        }

        string scriptPath = Path.Combine(toolsDir, "patch_save_video.py");
        string modeArg = mode.Equals("CRIADOR", StringComparison.OrdinalIgnoreCase) ? "criador" : "completo";

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" --mode {modeArg} \"{saveDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            psi.Environment["PYTHONPATH"] = toolsDir;
            psi.Environment["PYTHONUNBUFFERED"] = "1";

            using var p = Process.Start(psi);
            if (p is null) return false;

            var errTask = p.StandardError.ReadToEndAsync();
            string? line;
            while ((line = p.StandardOutput.ReadLine()) is not null)
            {
                string t = line.Trim();
                if (t.Length == 0) continue;
                AppMeta.Log("Save patch: " + t);
                if (t.StartsWith("PROGRESS ", StringComparison.Ordinal))
                    progress?.Invoke(t["PROGRESS ".Length..]);
            }

            if (!p.WaitForExit(180_000))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
                AppMeta.Log("Save patch timeout.");
                return false;
            }

            string stderr = errTask.GetAwaiter().GetResult();
            if (!string.IsNullOrWhiteSpace(stderr))
                AppMeta.Log("Save patch stderr: " + stderr.Trim());

            return p.ExitCode == 0;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao executar patch_save_video: " + ex.Message);
            return false;
        }
    }
}
