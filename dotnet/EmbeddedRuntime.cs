using System.Reflection;

namespace GuttyRL;

/// <summary>Extrai Python embed + patch .save embutidos no exe (1a execucao por versao).</summary>
internal static class EmbeddedRuntime
{
    private const string BundleLogicalName = "GuttyRL.embed-bundle.zip";

    private static readonly string RuntimeRoot = Path.Combine(AppMeta.GuttyDir, "runtime", AppMeta.Version.TrimStart('v'));
    private static readonly string StampPath = Path.Combine(RuntimeRoot, ".ready");
    private static string? _pythonExe;
    private static string? _toolsDir;

    public static bool EnsureReady(out string pythonExe, out string toolsDir)
    {
        pythonExe = _pythonExe ?? "";
        toolsDir = _toolsDir ?? "";

        try
        {
            if (!Directory.Exists(RuntimeRoot) || !File.Exists(StampPath))
                ExtractBundle();

            pythonExe = Path.Combine(RuntimeRoot, "py311", "python.exe");
            toolsDir = Path.Combine(RuntimeRoot, "tools");

            if (!File.Exists(pythonExe) || !File.Exists(Path.Combine(toolsDir, "patch_save_video.py")))
            {
                AppMeta.Log("Runtime embutido incompleto apos extracao.");
                return false;
            }

            _pythonExe = pythonExe;
            _toolsDir = toolsDir;
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("Falha ao preparar runtime embutido: " + ex.Message);
            return false;
        }
    }

    private static void ExtractBundle()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(BundleLogicalName)
            ?? throw new InvalidOperationException("embed-bundle.zip ausente no exe. Recompile com build_exe.bat.");

        string tempZip = Path.Combine(Path.GetTempPath(), $"gutty-embed-{Guid.NewGuid():N}.zip");
        try
        {
            using (var fs = File.Create(tempZip))
                stream.CopyTo(fs);

            if (Directory.Exists(RuntimeRoot))
                Directory.Delete(RuntimeRoot, true);
            Directory.CreateDirectory(RuntimeRoot);

            System.IO.Compression.ZipFile.ExtractToDirectory(tempZip, RuntimeRoot);
            File.WriteAllText(StampPath, DateTime.UtcNow.ToString("O"));
            AppMeta.Log($"Runtime embutido extraido em {RuntimeRoot}");
        }
        finally
        {
            try { File.Delete(tempZip); } catch { }
        }
    }
}
