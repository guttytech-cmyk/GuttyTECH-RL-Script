using System.Diagnostics;

namespace GuttyRL;

/// <summary>Patch .save Epic (VideoSettingsSavePC + EffectIntensity) via nixwrap + purge RLSettingsData.</summary>
internal static class SaveVideoPatcher
{
    private static readonly string ToolsDir = Path.Combine(AppMeta.GuttyDir, "tools");
    private static readonly string ScriptPath = Path.Combine(ToolsDir, "patch_save_video.py");
    private static readonly string WheelPath = Path.Combine(ToolsDir, "nixwrap_rl-0.1.3-py3-none-any.whl");

    public static bool PatchSaveDirectory(string saveDir)
    {
        if (!Directory.Exists(saveDir))
        {
            AppMeta.Log("Save Epic nao encontrado; pulando patch de video.");
            return true;
        }

        if (!EnsureTooling())
        {
            AppMeta.Log("Falha ao preparar patch_save_video (Python 3.11 + nixwrap).");
            return false;
        }

        string py = FindPython311();
        if (py is null)
        {
            AppMeta.Log("Python 3.11 nao encontrado. Instale de python.org para sincronizar o menu.");
            return false;
        }

        try
        {
            string pyExe = py;
            string pyPrefix = "";
            if (py.Equals("py", StringComparison.OrdinalIgnoreCase))
            {
                pyExe = "py";
                pyPrefix = "-3.11 ";
            }

            var psi = new ProcessStartInfo
            {
                FileName = pyExe,
                Arguments = $"{pyPrefix}\"{ScriptPath}\" \"{saveDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.Environment["PYTHONPATH"] = $"{Path.Combine(ToolsDir, "pydeps")}{Path.PathSeparator}{ToolsDir}";

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

    private static bool EnsureTooling()
    {
        try
        {
            Directory.CreateDirectory(ToolsDir);
            Directory.CreateDirectory(Path.Combine(ToolsDir, "pydeps"));

            string bundledDir = Path.Combine(AppContext.BaseDirectory, "tools");
            if (Directory.Exists(bundledDir))
            {
                foreach (var f in Directory.EnumerateFiles(bundledDir))
                {
                    string dest = Path.Combine(ToolsDir, Path.GetFileName(f));
                    File.Copy(f, dest, true);
                }
            }

            if (!File.Exists(ScriptPath))
            {
                AppMeta.Log("patch_save_video.py ausente. Reinstale o RL Optimizer.");
                return false;
            }

            if (!File.Exists(WheelPath))
            {
                AppMeta.Log("Wheel nixwrap ausente em tools/. Copie nixwrap_rl-0.1.3-py3-none-any.whl.");
                return false;
            }

            string? py = FindPython311();
            if (py is null) return false;

            string deps = Path.Combine(ToolsDir, "pydeps");
            string stamp = Path.Combine(deps, ".nixwrap_installed");
            if (!File.Exists(stamp) && !NixwrapReady(py, deps))
            {
                int code = RunPy(py, $"{PyPrefix(py)}-m pip install --target \"{deps}\" --no-deps --ignore-requires-python \"{WheelPath}\" pycryptodome psutil -q");
                if (code != 0 && !NixwrapReady(py, deps)) return false;
                File.WriteAllText(stamp, DateTime.UtcNow.ToString("O"));
            }

            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("EnsureTooling: " + ex.Message);
            return false;
        }
    }

    private static string PyPrefix(string py) =>
        py.Equals("py", StringComparison.OrdinalIgnoreCase) ? "-3.11 " : "";

    private static int RunPy(string py, string args)
    {
        string file = py.Equals("py", StringComparison.OrdinalIgnoreCase) ? "py" : py;
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        using var p = Process.Start(psi);
        if (p is null) return -1;
        p.WaitForExit(180_000);
        return p.ExitCode;
    }

    private static bool NixwrapReady(string py, string deps)
    {
        string file = py.Equals("py", StringComparison.OrdinalIgnoreCase) ? "py" : py;
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = $"{PyPrefix(py)}-c \"import nixwrap.save_file\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        psi.Environment["PYTHONPATH"] = deps;
        using var p = Process.Start(psi);
        if (p is null) return false;
        p.WaitForExit(30_000);
        return p.ExitCode == 0;
    }

    private static string? FindPython311()
    {
        string[] candidates =
        [
            "py -3.11",
            @"C:\Users\" + Environment.UserName + @"\AppData\Local\Programs\Python\Python311\python.exe",
            "python3.11",
            "python",
        ];

        foreach (var c in candidates)
        {
            try
            {
                string file, args;
                if (c.StartsWith("py ", StringComparison.Ordinal))
                {
                    file = "py";
                    args = c[3..] + " -c \"import sys; print(sys.version_info[:2])\"";
                }
                else
                {
                    file = c;
                    args = "-c \"import sys; print(sys.version_info[:2])\"";
                }

                var psi = new ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p is null) continue;
                string ver = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(5000);
                if (ver.StartsWith("(3, 11)", StringComparison.Ordinal))
                    return c.StartsWith("py ", StringComparison.Ordinal) ? "py" : file;
            }
            catch { }
        }

        return null;
    }
}
