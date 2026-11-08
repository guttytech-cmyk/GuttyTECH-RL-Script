using System.Diagnostics;

namespace GuttyRL;

/// <summary>
/// Uma instância só. Watchers velhos e o python embutido preso no arquivo
/// eram o que recolocava o save bugado e derrubava o runtime.
/// </summary>
internal static class ProcessGuard
{
    public static void KillOtherInstances()
    {
        int self = Environment.ProcessId;
        foreach (Process p in Process.GetProcesses())
        {
            try
            {
                string? exe = null;
                if (p.ProcessName.StartsWith("python", StringComparison.OrdinalIgnoreCase))
                {
                    try { exe = p.MainModule?.FileName; } catch { }
                }
                if (!ShouldStop(p.Id, self, p.ProcessName, exe))
                    continue;
                try { p.Kill(entireProcessTree: true); }
                catch { try { p.Kill(); } catch { } }
            }
            catch { }
            finally
            {
                try { p.Dispose(); } catch { }
            }
        }
    }

    /// <summary>
    /// GuttyTECH_RL de outro pid sai. python só se o exe estiver na pasta do otimizador.
    /// </summary>
    internal static bool ShouldStop(int pid, int selfPid, string processName, string? exePath)
    {
        if (pid == selfPid || pid <= 0)
            return false;
        if (processName.StartsWith("GuttyTECH_RL", StringComparison.OrdinalIgnoreCase)
            || processName.StartsWith("GuttyRL", StringComparison.OrdinalIgnoreCase))
            return true;
        if (!processName.StartsWith("python", StringComparison.OrdinalIgnoreCase))
            return false;
        if (string.IsNullOrEmpty(exePath))
            return false;
        return exePath.Contains(@"\GuttyTECH\RL-Optimizer-v22\", StringComparison.OrdinalIgnoreCase);
    }
}
