using System.Diagnostics;

namespace GuttyRL;

/// <summary>Patch .save Epic (VideoSettingsSavePC + EffectIntensity) via runtime Python embutido.</summary>
internal static class SaveVideoPatcher
{
    public static bool PatchSaveDirectory(string saveDir)
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

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                Arguments = $"\"{scriptPath}\" \"{saveDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.Environment["PYTHONPATH"] = toolsDir;

            using var p = Process.Start(psi);
            if (p is null) return false;
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit(120_000);

            foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                AppMeta.Log("Save patch: " + line.Trim());
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
