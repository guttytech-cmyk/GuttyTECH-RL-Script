using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>Diagnostico e reparacao (menu CORRIGIR ERROS) — perfil otimizado vs boot stock.</summary>
internal static class ErrorRepair
{
    private static readonly Regex BootKiller = new(
        @"^(OnlyStreamInTextures|WaitForGPU)=(True)$",
        RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

    public static int Diagnostico(string? cfg, Func<string?> detectMode, bool interactive)
    {
        var lines = new List<string>();
        bool issues = false;

        if (cfg is null || !File.Exists(cfg))
        {
            lines.Add("INI: AUSENTE — abra o RL 1x pela Epic/Steam.");
            issues = true;
        }
        else
        {
            lines.Add("INI: " + Fit(cfg, 52));
            bool writable = FolderAccess.CanWriteToDirectory(Path.GetDirectoryName(cfg)!);
            lines.Add(writable ? "Gravacao: OK" : "Gravacao: BLOQUEADA (use Permissoes)");
            if (!writable) issues = true;

            string text = "";
            try { text = File.ReadAllText(cfg); } catch { issues = true; }

            string? mode = detectMode();
            lines.Add(mode is null ? "Modo Gutty: NENHUM (stock / parcial)" : "Modo Gutty: " + mode);
            if (mode is null) issues = true;

            var map = ParseSystemSettings(text);
            void Check(string key, string want, bool critical = true)
            {
                map.TryGetValue(key, out string? got);
                if (string.Equals(got, want, StringComparison.OrdinalIgnoreCase))
                    lines.Add($"  OK {key}={got}");
                else
                {
                    lines.Add($"  !! {key}={got ?? "?"} (esperado {want})");
                    if (critical) issues = true;
                }
            }

            if (mode == "COMPLETO")
            {
                Check("UncappedFramerate", "True");
                Check("bAllowLightShafts", "False");
                Check("bUseTranslucentArenaShaders", "False");
                Check("ParticleLODBias", "100");
                Check("OnlyStreamInTextures", "False");
                Check("WaitForGPU", "False");
            }
            else if (mode == "CRIADOR")
            {
                Check("UncappedFramerate", "True");
                Check("bAllowLightShafts", "False");
                Check("OnlyStreamInTextures", "False");
                Check("WaitForGPU", "False");
            }
            else
            {
                if (HasBootKillers(text))
                {
                    lines.Add("  !! Boot killers ativos (OnlyStream/WaitForGPU=True)");
                    issues = true;
                }
            }
        }

        bool rlOpen = Process.GetProcessesByName("RocketLeague").Length > 0;
        lines.Add(rlOpen ? "Rocket League: ABERTO (feche antes de reparar)" : "Rocket League: fechado");
        if (rlOpen) issues = true;

        string lockPath = Path.Combine(AppMeta.GuttyDir, "watcher.lock");
        if (File.Exists(lockPath))
        {
            try
            {
                string pidLine = File.ReadAllLines(lockPath).FirstOrDefault() ?? "";
                lines.Add("Watcher: lock ativo (pid " + pidLine + ")");
            }
            catch { lines.Add("Watcher: lock presente"); }
        }
        else
            lines.Add("Watcher: inativo");

        string? epic = SaveRecovery.SaveDirFromIni(cfg ?? "", epic: true);
        string? steam = SaveRecovery.SaveDirFromIni(cfg ?? "", epic: false);
        int epicN = CountSaves(epic);
        int steamN = CountSaves(steam);
        lines.Add($"Saves Epic: {epicN} | Steam: {steamN}");

        AppMeta.Log("DIAG: " + string.Join(" | ", lines));

        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("DIAGNOSTICO", Ui.MAmber);
            Ui.Gap();
            Ui.PanelTop(issues ? "PROBLEMAS DETETADOS" : "ESTADO OK");
            foreach (string l in lines)
                Ui.PanelLine(Ui.C(l, l.Contains("!!") || l.Contains("BLOQUE") || l.Contains("AUSENTE") || l.Contains("ABERTO")
                    ? Ui.Amber : Ui.Gray));
            Ui.PanelBottom();
            Ui.Gap();
            if (issues)
            {
                Ui.StepsPanel("SUGESTAO", new[]
                {
                    "Menu quebrado / pos-boot / APLICAR → [2] REPARAR PERFIL",
                    "Jogo nao abre de todo → [3] RECUPERAR BOOT (stock)",
                    "Pasta bloqueada → [1] PERMISSOES",
                }, Ui.MAmber);
            }
            else
            {
                Ui.CompletionMessage(Ui.OkGreen, "NADA CRITICO", new[]
                {
                    "INI gravavel e perfil coerente.",
                    "Se o menu in-game ainda falhar: [2] REPARAR PERFIL.",
                });
            }
        }

        return issues ? 1 : 0;
    }

    /// <summary>Mantém COMPLETO/CRIADOR: unlock + reclamp INI + sync menu + purge cache.</summary>
    public static int RepararPerfil(string? cfg, Func<string?> detectMode, bool interactive)
    {
        if (cfg is null)
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Caminho do INI desconhecido." });
            return 1;
        }

        if (Process.GetProcessesByName("RocketLeague").Length > 0)
        {
            if (interactive)
            {
                Ui.Gap();
                Ui.PanelTop("ROCKET LEAGUE ABERTO");
                Ui.PanelLine(Ui.C("Feche o jogo para reparar INI+save com seguranca.", Ui.Amber));
                Ui.PanelBottom();
                Ui.Prompt("Fechar o jogo agora? (S/N)");
                if (!IsYes(Console.ReadLine()))
                    return 1;
                foreach (var p in Process.GetProcessesByName("RocketLeague"))
                {
                    try { p.Kill(); } catch { }
                }
                Thread.Sleep(2000);
            }
            else
                return 1;
        }

        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("REPARAR PERFIL", Ui.MAmber);
            Ui.StepsPanel("O QUE ISTO FAZ", new[]
            {
                "NAO remove o otimizador (mantem COMPLETO/CRIADOR)",
                "Reclampa INI (Uncapped, shaders, particle, boot-safe)",
                "Regrava menu VideoOptions nas contas Epic/Steam",
                "Limpa cache RLSettingsData (Epic)",
            }, Ui.MAmber);
        }

        if (!FolderAccess.EnsureWriteAccess(cfg, interactive))
            return 1;

        string? mode = detectMode();
        if (mode is not ("COMPLETO" or "CRIADOR"))
        {
            if (interactive)
            {
                Ui.Gap();
                Ui.PanelTop("SEM MODO GUTTY");
                Ui.PanelLine(Ui.C("Nao ha GuttyTechMode no INI.", Ui.Amber));
                Ui.PanelLine(Ui.C("Sem isso so posso aplicar um perfil agora.", Ui.Gray));
                Ui.PanelBottom();
                Ui.Prompt("Aplicar COMPLETO agora? (S=COMPLETO / N=cancelar)");
                if (!IsYes(Console.ReadLine()))
                    return 1;
                mode = "COMPLETO";
            }
            else
            {
                AppMeta.Log("REPARAR: sem modo — abort.");
                return 1;
            }
        }

        bool unlockOk = true;
        bool reclampOk;
        bool syncOk;
        bool purgeOk;

        if (interactive)
        {
            unlockOk = Ui.StepAnimated("Destravando INI", () =>
            {
                try { File.SetAttributes(cfg, FileAttributes.Normal); return true; }
                catch { return false; }
            });
            reclampOk = Ui.StepAnimated("Reclampando INI (" + mode + ")", () =>
                VideoSettingsSync.ReclampIni(cfg, mode!) && StripBootKillers(cfg));
            syncOk = Ui.StepAnimated("Sincronizando menu (saves)", () =>
                VideoSettingsSync.SyncVideoSave(cfg, mode!, interactive: false));
            purgeOk = Ui.StepAnimated("Limpando cache RLSettingsData", SaveRecovery.PurgeRlSettingsData);
        }
        else
        {
            try { File.SetAttributes(cfg, FileAttributes.Normal); } catch { unlockOk = false; }
            reclampOk = VideoSettingsSync.ReclampIni(cfg, mode!) && StripBootKillers(cfg);
            syncOk = VideoSettingsSync.SyncVideoSave(cfg, mode!, interactive: false);
            purgeOk = SaveRecovery.PurgeRlSettingsData();
        }

        if (interactive)
            VideoSettingsSync.StartExitWatcher(mode!);
        bool ok = unlockOk && reclampOk && syncOk && purgeOk;
        AppMeta.Log($"REPARAR PERFIL {mode}: ok={ok} reclamp={reclampOk} sync={syncOk} purge={purgeOk}");

        if (interactive)
        {
            if (ok)
            {
                Ui.CompletionMessage(Ui.OkGreen, "PERFIL REPARADO", new[]
                {
                    "Modo " + mode + " reaplicado no INI + menu.",
                    "Watcher ativo: ao fechar o RL, repara sozinho.",
                    "Nao clique APLICAR em resolucao/sem bordas no jogo.",
                });
            }
            else
            {
                Ui.CompletionMessage(Ui.MAmber, "REPARO PARCIAL", new[]
                {
                    reclampOk ? "INI: OK" : "INI: falhou",
                    syncOk ? "Save/menu: OK" : "Save/menu: falhou (Python/runtime?)",
                    purgeOk ? "Cache: OK" : "Cache: falhou",
                    "Se o jogo nao abrir: use [3] RECUPERAR BOOT.",
                });
            }
        }

        return ok ? 0 : 1;
    }

    /// <summary>Remove OnlyStreamInTextures/WaitForGPU=True sem restaurar stock.</summary>
    public static bool StripBootKillers(string iniPath)
    {
        try
        {
            if (!File.Exists(iniPath)) return false;
            string text = File.ReadAllText(iniPath);
            if (!HasBootKillers(text)) return true;
            string fixedText = BootKiller.Replace(text, "$1=False");
            File.WriteAllText(iniPath, fixedText);
            AppMeta.Log("Boot killers removidos (OnlyStream/WaitForGPU -> False).");
            return true;
        }
        catch (Exception ex)
        {
            AppMeta.Log("StripBootKillers: " + ex.Message);
            return false;
        }
    }

    public static bool HasBootKillers(string iniText) =>
        Regex.IsMatch(iniText, @"(?im)^(OnlyStreamInTextures|WaitForGPU)=True\s*$");

    private static Dictionary<string, string> ParseSystemSettings(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string section = "";
        foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (raw.StartsWith('[') && raw.EndsWith(']'))
            {
                section = raw[1..^1];
                continue;
            }
            if (!section.Equals("SystemSettings", StringComparison.OrdinalIgnoreCase)) continue;
            int eq = raw.IndexOf('=');
            if (eq <= 0) continue;
            map[raw[..eq].Trim()] = raw[(eq + 1)..].Trim();
        }
        return map;
    }

    private static int CountSaves(string? dir)
    {
        try
        {
            if (dir is null || !Directory.Exists(dir)) return 0;
            return Directory.EnumerateFiles(dir, "*.save").Count();
        }
        catch { return 0; }
    }

    private static string Fit(string path, int max)
    {
        if (path.Length <= max) return path;
        return "..." + path[^(max - 3)..];
    }

    private static bool IsYes(string? s) =>
        !string.IsNullOrWhiteSpace(s) && (s.Trim().StartsWith("S", StringComparison.OrdinalIgnoreCase)
            || s.Trim().StartsWith("Y", StringComparison.OrdinalIgnoreCase));
}
