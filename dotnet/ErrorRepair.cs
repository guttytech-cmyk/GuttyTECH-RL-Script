using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>
/// Diagnostico e reparacao (menu CORRIGIR ERROS).
/// UnbreakBoot = caminho nuclear quando o Rocket League nao abre.
/// </summary>
internal static class ErrorRepair
{
    private static readonly (string Key, string Value)[] BootSafeKeys =
    {
        ("OnlyStreamInTextures", "False"),
        ("WaitForGPU", "False"),
        ("OneFrameThreadLag", "True"),
        ("AllowPerFrameSleep", "True"),
        ("AllowPerFrameYield", "True"),
    };

    public static (IReadOnlyList<string> Lines, bool Issues) CollectDiagnosticReport(
        string? cfg,
        Func<string?> detectMode)
    {
        var lines = new List<string>();
        bool issues = false;
        bool bootRisk = false;

        if (cfg is null || !File.Exists(cfg))
        {
            lines.Add("INI: AUSENTE — abra o RL 1x pela Epic/Steam.");
            issues = true;
            bootRisk = true;
        }
        else
        {
            lines.Add("INI: " + Fit(cfg, 52));
            bool writable = FolderAccess.CanWriteToDirectory(Path.GetDirectoryName(cfg)!);
            lines.Add(writable ? "Gravacao: OK" : "Gravacao: BLOQUEADA (use Permissoes)");
            if (!writable) issues = true;

            string text = "";
            try { text = File.ReadAllText(cfg); } catch { issues = true; bootRisk = true; }

            string? mode = detectMode();
            lines.Add(mode is null ? "Modo Gutty: NENHUM (stock / parcial)" : "Modo Gutty: " + mode);
            lines.Add("App: " + AppMeta.Version);

            try
            {
                var fi = new FileInfo(cfg);
                int dups = string.IsNullOrEmpty(text) ? 0 : IniHygiene.CountDuplicateKeyLines(text);
                if (fi.Length >= IniHygiene.SoftBloatBytes || dups > 0)
                {
                    lines.Add($"  !! INI inchado: {fi.Length / 1024}KB, chaves duplicadas na secao={dups} (reaplique o modo / REPARAR PERFIL)");
                    issues = true;
                }
                else
                    lines.Add($"  OK INI tamanho {fi.Length / 1024}KB (sem dups na secao)");
            }
            catch { }

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

            if (HasBootKillers(text))
            {
                lines.Add("  !! BOOT RISK: OnlyStreamInTextures/WaitForGPU=True");
                issues = true;
                bootRisk = true;
            }
            else
            {
                lines.Add("  OK boot-safe (OnlyStream/WaitForGPU)");
            }

            if (mode == "COMPLETO")
            {
                Check("UncappedFramerate", "True");
                Check("bAllowLightShafts", "False");
                Check("bUseTranslucentArenaShaders", "False");
                Check("ParticleLODBias", "100");
                Check("DynamicLights", "False");
                Check("DynamicShadows", "False");
                Check("MaxShadowResolution", "1");
                Check("MaxFilterBlurSampleCount", "1");
                Check("ApexLODResourceBudget", "0.000000");
                Check("TessellationAdaptivePixelsPerTriangle", "4096.000000");
                Check("MobileNormalMapping", "False");
                Check("OnlyStreamInTextures", "False");
                Check("WaitForGPU", "False");
            }
            else if (mode == "CRIADOR")
            {
                Check("UncappedFramerate", "True");
                Check("UseVsync", "False");
                Check("bSmoothFrameRate", "False");
                Check("CustomFPS", "0", critical: false);
                Check("MaxFilterBlurSampleCount", "1");
                Check("OnlyStreamInTextures", "False");
                Check("WaitForGPU", "False");
                Check("DynamicShadows", "True"); // visual keep
                Check("AllowApexCloth", "False", critical: false);
            }
        }

        bool rlOpen = Process.GetProcessesByName("RocketLeague").Length > 0;
        lines.Add(rlOpen ? "Rocket League: ABERTO (feche antes de reparar)" : "Rocket League: fechado");
        if (rlOpen) issues = true;

        string lockPath = Path.Combine(AppMeta.GuttyDir, "watcher.lock");
        string? modeNow = detectMode();
        if (File.Exists(lockPath))
        {
            try
            {
                string[] lockLines = File.ReadAllLines(lockPath);
                string pidLine = lockLines.FirstOrDefault() ?? "";
                string watchMode = lockLines.Length > 1 ? lockLines[1].Trim() : "";
                bool healthy = modeNow is "COMPLETO" or "CRIADOR"
                               && (string.IsNullOrEmpty(watchMode)
                                   || watchMode.Equals(modeNow, StringComparison.OrdinalIgnoreCase))
                               && VideoSettingsSync.IsHealthyWatcherRunning(modeNow);
                if (healthy)
                    lines.Add($"Watcher: ATIVO OK (pid {pidLine}, {modeNow})");
                else
                {
                    lines.Add($"Watcher: lock residual (pid {pidLine}) — use REMOVER ou reaplique o modo");
                    issues = true;
                }
            }
            catch
            {
                lines.Add("Watcher: lock presente (ilegivel)");
                issues = true;
            }
        }
        else if (modeNow is "COMPLETO" or "CRIADOR")
            lines.Add("Watcher: inativo (modo ativo sem protecao — reaplique o modo)");
        else
            lines.Add("Watcher: inativo");

        var (eacOk, eacDetail) = EacRepairService.Assess();
        lines.Add(eacOk ? "EAC: OK — " + eacDetail : "EAC: PROBLEMA — " + eacDetail);
        if (!eacOk) issues = true;

        string? epic = SaveRecovery.SaveDirFromIni(cfg ?? "", epic: true);
        string? steam = SaveRecovery.SaveDirFromIni(cfg ?? "", epic: false);
        int epicN = CountSaves(epic);
        int steamN = CountSaves(steam);
        lines.Add($"Saves Epic: {epicN} | Steam: {steamN}");

        if (IsOneDrivePath(cfg) || IsOneDrivePath(epic) || IsOneDrivePath(steam))
        {
            lines.Add("  !! OneDrive: saves/INI em pasta sincronizada — risco alto de preset com nome e Octane padrão");
            lines.Add("  ACAO OneDrive: pause o sync ou tire My Games\\Rocket League da sincronizacao; depois RESTAURAR PRESETS");
            issues = true;
        }

        foreach (string health in SaveRecovery.AssessSaveHealth(cfg))
        {
            lines.Add(health);
            if (health.Contains("!!", StringComparison.Ordinal))
                issues = true;
        }

        if (bootRisk)
            lines.Add("ACAO: use RECUPERAR BOOT / CORRIGIR TUDO (caminho nuclear).");
        else if (issues && lines.Any(l => l.Contains("LOAD FAILURE", StringComparison.OrdinalIgnoreCase)
                                          && l.Contains("provavel", StringComparison.OrdinalIgnoreCase)))
            lines.Add("ACAO: use CORRIGIR SAVE (LOAD FAILURE) — Steam Cloud + saves locais.");

        return (lines, issues);
    }

    /// <summary>Documentos/Desktop sob OneDrive — sync costuma corromper preset (nome OK, Octane padrao).</summary>
    internal static bool IsOneDrivePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return path.IndexOf("OneDrive", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public static int Diagnostico(string? cfg, Func<string?> detectMode, bool interactive)
    {
        (IReadOnlyList<string> lines, bool issues) = CollectDiagnosticReport(cfg, detectMode);
        AppMeta.Log("DIAG: " + string.Join(" | ", lines));

        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("DIAGNOSTICO", Ui.MAmber);
            Ui.Gap();
            Ui.PanelTop(issues ? "PROBLEMAS DETETADOS" : "ESTADO OK");
            foreach (string l in lines)
                Ui.PanelLine(Ui.C(l, l.Contains("!!") || l.Contains("BLOQUE") || l.Contains("AUSENTE") || l.Contains("ABERTO") || l.Contains("ACAO")
                    ? Ui.Amber : Ui.Gray));
            Ui.PanelBottom();
            Ui.Gap();
            if (issues)
            {
                Ui.StepsPanel("SUGESTAO", new[]
                {
                    "Jogo NAO abre → [3] RECUPERAR BOOT ou [5] CORRIGIR TUDO",
                    "Jogo abre mas menu errado → [2] REPARAR PERFIL",
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

    /// <summary>
    /// Caminho nuclear: faz o Rocket League voltar a abrir.
    /// Nao reaplica COMPLETO/CRIADOR. Nao reinsere saves Best (podem estar corrompidos).
    /// </summary>
    public static int UnbreakBoot(
        string? cfg,
        Func<bool> restoreStockIni,
        Action unlockIni,
        bool interactive)
    {
        var report = new List<string>();
        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("RECUPERAR BOOT — JOGO NAO ABRE", Ui.MAmber);
            Ui.StepsPanel("CAMINHO NUCLEAR", new[]
            {
                "Fecha o Rocket League e o watcher automatico",
                "Preserva garagem no cofre Best (sem reaplicar agora)",
                "Volta o INI para stock/original sem otimizador",
                "Remove chaves que travam o boot",
                "Quarentena saves suspeitos + limpa cache Epic",
                "Depois: abra o RL 1x → RESTAURAR PRESETS → so entao reaplique o modo",
            }, Ui.MAmber);
        }

        if (cfg is null)
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Caminho do INI desconhecido.", "Abra o Rocket League 1x pela Epic/Steam." });
            return 1;
        }

        // 1) Matar jogo + watcher (sem perguntar no modo GUI)
        bool killed = ForceCloseRocketLeague();
        report.Add(killed ? "RL fechado" : "RL ja estava fechado");
        VideoSettingsSync.StopExistingWatchers();
        report.Add("watcher parado");

        if (interactive)
        {
            Ui.StepAnimated("Encerrando Rocket League / watcher", () =>
            {
                ForceCloseRocketLeague();
                VideoSettingsSync.StopExistingWatchers();
                return true;
            });
        }

        // 2) Permissões mínimas
        bool accessOk;
        if (interactive)
            accessOk = Ui.StepAnimated("Liberando escrita na pasta", () => FolderAccess.EnsureWriteAccess(cfg, interactive: false));
        else
            accessOk = FolderAccess.EnsureWriteAccess(cfg, interactive: false);
        report.Add(accessOk ? "pasta gravavel" : "pasta ainda bloqueada");

        // 3) Snapshot da garagem ANTES de quarentenar
        int snapped = 0;
        if (interactive)
            Ui.StepAnimated("Preservando presets no cofre Best", () =>
            {
                snapped = SaveRecovery.BackupGaragePresets(cfg);
                return true;
            });
        else
            snapped = SaveRecovery.BackupGaragePresets(cfg);
        if (snapped > 0) report.Add($"best snapshot={snapped}");

        // 4) Unlock + stock INI
        bool unlockOk = true;
        bool iniOk;
        if (interactive)
        {
            unlockOk = Ui.StepAnimated("Destravando TASystemSettings.ini", () =>
            {
                try { unlockIni(); File.SetAttributes(cfg, FileAttributes.Normal); return true; }
                catch { return false; }
            });
            Ui.StepAnimated("Backup de seguranca do INI", () => { try { Program.BackupIniForRepair(cfg); return true; } catch { return true; } });
            iniOk = Ui.StepAnimated("Restaurando INI stock (sem otimizador)", restoreStockIni);
            Ui.StepAnimated("Removendo boot-killers", () => ForceBootSafeIni(cfg));
        }
        else
        {
            try { unlockIni(); File.SetAttributes(cfg, FileAttributes.Normal); } catch { unlockOk = false; }
            try { Program.BackupIniForRepair(cfg); } catch { }
            iniOk = restoreStockIni();
            ForceBootSafeIni(cfg);
        }

        report.Add(iniOk ? "INI stock OK" : "INI stock FALHOU");
        report.Add(unlockOk ? "INI destravado" : "INI lock residual");

        // 5) Quarentena dos saves live (nao reinsere Best — pode ser o save que trava)
        bool saveOk;
        if (interactive)
            saveOk = Ui.StepAnimated("Quarentena de saves + purge cache", () => SaveRecovery.UnbreakSaves(cfg));
        else
            saveOk = SaveRecovery.UnbreakSaves(cfg);
        report.Add(saveOk ? "saves/cache limpos" : "saves/cache parcial");

        // 5b) Easy Anti-Cheat 30005 / CreateService 1072
        var eac = EacRepairService.Repair();
        if (interactive)
            Ui.StepAnimated("Reparando Easy Anti-Cheat (30005/1072)", () => eac.Ok || !eac.NeedsReboot);
        report.Add(eac.Ok ? "EAC OK" : (eac.NeedsReboot ? "EAC precisa REBOOT" : "EAC parcial"));
        if (!string.IsNullOrWhiteSpace(eac.Detail))
            AppMeta.Log("UNBREAK-BOOT EAC: " + eac.Detail);

        // 5c) Limpar marcas / tag de modo — senao a UI ainda diz COMPLETO com INI stock.
        try
        {
            if (File.Exists(cfg))
                Program.StripGuttyMarkersForRepair(cfg);
        }
        catch { }
        ModeDetect.Clear();
        report.Add("modo Gutty limpo");

        // 6) Verificacao final
        bool bootSafe = true;
        try
        {
            if (File.Exists(cfg))
            {
                string text = File.ReadAllText(cfg);
                bootSafe = !HasBootKillers(text) && ForceBootSafeIni(cfg);
                try { File.SetAttributes(cfg, FileAttributes.Normal); } catch { }
            }
            else bootSafe = false;
        }
        catch { bootSafe = false; }
        report.Add(bootSafe ? "boot-safe verificado" : "boot-safe FALHOU");

        AppMeta.Log("UNBREAK-BOOT: " + string.Join("; ", report));

        bool ok = iniOk && bootSafe;
        if (interactive)
        {
            Ui.CompletionMessage(ok ? Ui.OkGreen : Ui.MAmber, ok ? "JOGO DESBLOQUEADO" : "RECUPERACAO PARCIAL", new[]
            {
                string.Join(" · ", report),
                "1) Epic/Steam → Verificar arquivos do Rocket League",
                "2) Se erro EAC 30005 / CreateService 1072: reinicie o PC e abra de novo",
                "3) Abra o jogo 1x e confirme que entra no menu",
                "4) AGORA use RESTAURAR PRESETS (este caminho tirou os saves live de proposito)",
                "5) So depois aplique COMPLETO ou CRIADOR de novo",
            });
        }

        return ok ? 0 : 1;
    }

    /// <summary>Repara so o Easy Anti-Cheat (erro 30005 CreateService 1072).</summary>
    public static int RepararEac(bool interactive)
    {
        ForceCloseRocketLeague();
        if (interactive)
            Ui.StepAnimated("A preparar reparo EAC", () => true);

        var result = EacRepairService.Repair();
        AppMeta.Log($"CORRIGIR-EAC ok={result.Ok} reboot={result.NeedsReboot} {result.Detail}");

        string detail = string.IsNullOrWhiteSpace(result.Detail)
            ? "Sem detalhe extra."
            : (result.Detail.Length > 180 ? result.Detail[..180] + "…" : result.Detail);

        if (interactive)
        {
            if (result.Ok)
            {
                Ui.CompletionMessage(Ui.OkGreen, "EAC REPARADO", new[]
                {
                    detail,
                    "Abra o Rocket League pela Epic/Steam.",
                });
            }
            else if (result.NeedsReboot)
            {
                Ui.CompletionMessage(Ui.MAmber, "REINICIE O PC", new[]
                {
                    "O Windows marcou o servico EAC para apagar (erro 1072).",
                    "Reinicie o PC e abra o jogo — na maioria dos casos resolve.",
                    "Se continuar: Epic → Verificar ficheiros → abra de novo.",
                    detail,
                });
            }
            else
            {
                Ui.CompletionMessage(Ui.MAmber, "EAC PARCIAL", new[]
                {
                    detail,
                    "Reinicie o PC. Se falhar: Verificar ficheiros na Epic/Steam.",
                });
            }
        }

        return result.Ok ? 0 : (result.NeedsReboot ? 2 : 1);
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

        ForceCloseRocketLeague();

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
                ForceCloseRocketLeague();
            }
            else if (Process.GetProcessesByName("RocketLeague").Length > 0)
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
                "Se o jogo NAO abrir depois: use RECUPERAR BOOT",
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
                Ui.PanelLine(Ui.C("Sem modo ativo, use RECUPERAR BOOT se o jogo nao abre.", Ui.Gray));
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
        bool bootSafe;

        if (interactive)
        {
            unlockOk = Ui.StepAnimated("Destravando INI", () =>
            {
                try { File.SetAttributes(cfg, FileAttributes.Normal); return true; }
                catch { return false; }
            });
            reclampOk = Ui.StepAnimated("Reclampando INI (" + mode + ")", () =>
                VideoSettingsSync.ReclampIni(cfg, mode!) && ForceBootSafeIni(cfg));
            syncOk = Ui.StepAnimated("Sincronizando menu (saves)", () =>
                VideoSettingsSync.SyncVideoSave(cfg, mode!, interactive: false));
            purgeOk = Ui.StepAnimated("Limpando cache RLSettingsData", SaveRecovery.PurgeRlSettingsData);
            bootSafe = Ui.StepAnimated("Validando boot-safe", () => ForceBootSafeIni(cfg));
        }
        else
        {
            try { File.SetAttributes(cfg, FileAttributes.Normal); } catch { unlockOk = false; }
            reclampOk = VideoSettingsSync.ReclampIni(cfg, mode!) && ForceBootSafeIni(cfg);
            syncOk = VideoSettingsSync.SyncVideoSave(cfg, mode!, interactive: false);
            purgeOk = SaveRecovery.PurgeRlSettingsData();
            bootSafe = ForceBootSafeIni(cfg);
        }

        // Watcher em GUI e CLI — senao PROTEÇÃO fica OFF apos reparar pela UI.
        if (bootSafe)
            VideoSettingsSync.StartExitWatcher(mode!);

        bool ok = unlockOk && reclampOk && syncOk && purgeOk && bootSafe;
        AppMeta.Log($"REPARAR PERFIL {mode}: ok={ok} reclamp={reclampOk} sync={syncOk} purge={purgeOk} bootSafe={bootSafe}");

        if (interactive)
        {
            if (ok)
            {
                Ui.CompletionMessage(Ui.OkGreen, "PERFIL REPARADO", new[]
                {
                    "Modo " + mode + " reaplicado no INI + menu.",
                    "Boot-safe confirmado (OnlyStream/WaitForGPU=False).",
                    "Nao clique APLICAR em resolucao/sem bordas no jogo.",
                });
            }
            else
            {
                Ui.CompletionMessage(Ui.MAmber, "REPARO PARCIAL", new[]
                {
                    reclampOk ? "INI: OK" : "INI: falhou",
                    syncOk ? "Save/menu: OK" : "Save/menu: falhou",
                    bootSafe ? "Boot-safe: OK" : "Boot-safe: FALHOU",
                    "Se o jogo nao abrir: use RECUPERAR BOOT agora.",
                });
            }
        }

        return ok ? 0 : 1;
    }

    public static bool ForceCloseRocketLeague()
    {
        bool any = false;
        try
        {
            foreach (var p in Process.GetProcessesByName("RocketLeague"))
            {
                any = true;
                try { p.Kill(entireProcessTree: true); } catch { try { p.Kill(); } catch { } }
            }
        }
        catch { }

        for (int i = 0; i < 8; i++)
        {
            Thread.Sleep(400);
            if (Process.GetProcessesByName("RocketLeague").Length == 0)
                break;
            foreach (var p in Process.GetProcessesByName("RocketLeague"))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
            }
        }

        return any;
    }

    /// <summary>Garante chaves que nao podem travar o boot.</summary>
    public static bool ForceBootSafeIni(string iniPath)
    {
        try
        {
            if (!File.Exists(iniPath)) return false;
            string text = File.ReadAllText(iniPath);
            string fixedText = text;
            foreach (var (key, value) in BootSafeKeys)
                fixedText = UpsertSystemSettingsKey(fixedText, key, value);

            if (!string.Equals(text, fixedText, StringComparison.Ordinal))
            {
                try { File.SetAttributes(iniPath, FileAttributes.Normal); } catch { }
                File.WriteAllText(iniPath, fixedText, new UTF8Encoding(false));
                AppMeta.Log("Boot-safe INI aplicado.");
            }

            return !HasBootKillers(File.ReadAllText(iniPath));
        }
        catch (Exception ex)
        {
            AppMeta.Log("ForceBootSafeIni: " + ex.Message);
            return false;
        }
    }

    /// <summary>Remove OnlyStreamInTextures/WaitForGPU=True sem restaurar stock.</summary>
    public static bool StripBootKillers(string iniPath) => ForceBootSafeIni(iniPath);

    public static bool HasBootKillers(string iniText) =>
        Regex.IsMatch(iniText, @"(?im)^(OnlyStreamInTextures|WaitForGPU)=True\s*$");

    private static string UpsertSystemSettingsKey(string content, string key, string value)
    {
        var sb = new StringBuilder();
        bool inSs = false;
        bool replaced = false;
        string want = key + "=" + value;
        foreach (string raw in content.Replace("\r\n", "\n").Split('\n'))
        {
            string line = raw;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                if (inSs && !replaced)
                {
                    sb.Append(want).Append("\r\n");
                    replaced = true;
                }
                inSs = line.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
                sb.Append(line).Append("\r\n");
                continue;
            }

            if (inSs && line.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append(want).Append("\r\n");
                replaced = true;
                continue;
            }

            sb.Append(line).Append("\r\n");
        }

        if (!replaced)
        {
            // Append key right after [SystemSettings] header (no recursion).
            int idx = content.IndexOf("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return "[SystemSettings]\r\n" + want + "\r\n" + content;

            int lineEnd = content.IndexOf('\n', idx);
            if (lineEnd < 0)
                return content + "\r\n" + want + "\r\n";

            return content.Insert(lineEnd + 1, want + "\r\n");
        }

        return sb.ToString();
    }

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
