using System.Diagnostics;
using System.Security.Principal;
using System.Text;

namespace GuttyRL;

internal static class Program
{
    private static readonly string[] DisplayKeys =
        { "ResX", "ResY", "Fullscreen", "Borderless", "AutoDetectDesktopResolution" };

    // Frame pacing + efeitos que o jogo reescreve — sempre do template (nunca preservar).
    private static readonly string[] FramePacingKeys =
    {
        "WaitForGPU", "OneFrameThreadLag", "AllowPerFrameSleep", "AllowPerFrameYield",
        "UncappedFramerate", "bSmoothFrameRate", "CustomFPS",
    };

    private static readonly string[] VideoLockedKeys =
    {
        "bAllowLightShafts", "MobileFog", "MobileHeightFog",
        "MobileLightShaftScale", "MobileLightShaftFirstPass", "MobileLightShaftSecondPass",
        "MobileModShadows", "MobileMinimizeFogShaders",
    };

    // Chaves de video do CRIADOR vêm sempre do template apos limpeza (REMOVER).
    // Resolucao/borda: ReadDisplay.
    private static string? _cfg;
    private static bool? _cfgWritable;

    private static int Main(string[] args)
    {
        StartupGuard.Install();
        return StartupGuard.Run(() => Run(args));
    }

    private static int Run(string[] args)
    {
        try
        {
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; Environment.Exit(0); };
            Console.Title = "GUTTYTECH - RL INI OPTIMIZER " + AppMeta.Version;
        }
        catch { }

        bool ansi = Vt.Enable();
        Ui.Init(ansi);
        try { Directory.CreateDirectory(AppMeta.BackupDir); } catch { }

        if (args.Length > 0 && args[0].Equals("AUDIT", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[*] Auditando templates embutidos...");
            int rc = IniAudit.Run();
            Console.WriteLine(rc == 0 ? "[+] Audit OK." : $"[X] Audit FALHOU ({rc} problema(s) reportados acima).");
            return rc;
        }

        _cfg = ResolveConfigPath();
        if (_cfg is null)
        {
            Ui.Cls();
            Ui.Banner(false);
            Ui.PanelTop("ERRO");
            Ui.PanelLine(Ui.C("X  Pasta do Rocket League nao encontrada.", Ui.Red));
            Ui.PanelLine(Ui.C("Abra o jogo 1x pela Epic para criar as pastas.", Ui.Gray));
            Ui.PanelLine(Ui.C("Caminho esperado: Documents\\My Games\\Rocket League\\...", Ui.DimC));
            Ui.PanelBottom();
            AppMeta.Log("Pasta Config do RL nao encontrada.");
            Ui.EnterButton();
            return 1;
        }

        if (!File.Exists(_cfg))
            AppMeta.Log($"INI ausente; usando caminho alvo: {_cfg}");

        AppMeta.Log($"INI: {_cfg}");
        RefreshWritableCache();

        // Modo nao-interativo (e relancamento elevado): GuttyRL.exe COMPLETO [/keepopen]
        if (args.Length > 0)
        {
            string mode = args[0].Trim('/', '-').ToUpperInvariant();
            mode = mode switch
            {
                "1" => "COMPLETO",
                "2" => "CRIADOR",
                "3" => "REMOVER",
                "RECUPERAR" => "CORRIGIR-BOOT",
                "RECUPERAR-SAVE" => "CORRIGIR-BOOT",
                "CORRIGIR-BOOT" => "CORRIGIR-BOOT",
                "CORRIGIR-TUDO" => "CORRIGIR-TUDO",
                "CORRIGIR-PERFIL" => "CORRIGIR-PERFIL",
                "REPARAR" => "CORRIGIR-PERFIL",
                "REPARAR-PERFIL" => "CORRIGIR-PERFIL",
                "DIAG" => "DIAGNOSTICO",
                "DIAGNOSTICO" => "DIAGNOSTICO",
                "RESTAURAR-SAVES" => "RESTAURAR-PRESETS",
                "RESTAURAR-PRESETS" => "RESTAURAR-PRESETS",
                "5" => "CORRIGIR",
                "6" => "RESTAURAR-PRESETS",
                "HEAL" => "CORRIGIR",
                "WATCH" => "WATCH",
                _ => mode
            };
            if (mode == "WATCH")
            {
                string watchMode = args.Length > 1 ? args[1].Trim().ToUpperInvariant() : (DetectAppliedMode() ?? "COMPLETO");
                if (watchMode is not ("COMPLETO" or "CRIADOR")) watchMode = "COMPLETO";
                return VideoSettingsSync.RunWatch(_cfg!, watchMode);
            }
            bool keepOpen = args.Length > 1 && args[1].Equals("/keepopen", StringComparison.OrdinalIgnoreCase);
            int rc = Dispatch(mode, keepOpen);
            if (keepOpen || rc != 0) Ui.EnterButton();
            return rc;
        }

        Ui.Intro();

        // O RL no boot reescreve INI+save — heal completo (INI force + video).
        string? activeMode = DetectAppliedMode();
        if (_cfg is not null && activeMode is "COMPLETO" or "CRIADOR")
        {
            if (VideoSettingsSync.HealIfNeeded(_cfg, activeMode))
                AppMeta.Log($"Auto-heal INI+video ({activeMode}) OK.");
        }

        while (true)
        {
            ShowMenu();
            switch (Console.ReadLine()?.Trim())
            {
                case "1": Dispatch("COMPLETO", true); Ui.EnterButton(); break;
                case "2": Dispatch("CRIADOR", true); Ui.EnterButton(); break;
                case "3": Dispatch("REMOVER", true); Ui.EnterButton(); break;
                case "4": LaunchOptions(); break;
                case "5": CorrigirErrosMenu(); Ui.EnterButton(); break;
                case "6": Dispatch("RESTAURAR-PRESETS", true); Ui.EnterButton(); break;
                case "7": Ui.ShowCursor(); Goodbye(); return 0;
            }
        }
    }

    private static int RunHeal(bool interactive) => CorrigirPermissoes(interactive);

    private static void CorrigirErrosMenu()
    {
        while (true)
        {
            Ui.HideCursor();
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("CORRIGIR ERROS", Ui.MAmber);
            Ui.StepsPanel("RESTAURACAO E CORRECAO", new[]
            {
                "Menu High Quality / 60 FPS / pos-boot → REPARAR PERFIL",
                "Jogo NAO abre de todo → RECUPERAR BOOT (stock)",
                "Pasta bloqueada / Defender → PERMISSOES",
                "Presets do carro: menu principal [6]",
            }, Ui.MAmber);
            Ui.Gap();
            Ui.PanelTop("OPCOES");
            Ui.PanelBlank();
            Ui.MenuCard("1", "PERMISSOES", "Destrava INI e libera gravacao na pasta", Ui.Amber);
            Ui.MenuCard("2", "REPARAR PERFIL", "Mantem COMPLETO/CRIADOR — reclampa INI+menu", Ui.Green);
            Ui.MenuCard("3", "RECUPERAR BOOT", "Stock INI+save (so se o jogo nao abre)", Ui.Amber);
            Ui.MenuCard("4", "DIAGNOSTICO", "Mostra o que esta errado no INI/pasta", Ui.Cyan);
            Ui.MenuCard("5", "TUDO", "Permissoes + reparar (ou boot stock se preciso)", Ui.Amber);
            Ui.MenuCard("6", "VOLTAR", "Menu principal", Ui.DimC);
            Ui.PanelBlank();
            Ui.PanelBottom();
            Ui.Prompt("Escolha (1-6)");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": CorrigirPermissoes(true); Ui.EnterButton(); break;
                case "2": CorrigirPerfil(true); Ui.EnterButton(); break;
                case "3": CorrigirBoot(true); Ui.EnterButton(); break;
                case "4": CorrigirDiagnostico(true); Ui.EnterButton(); break;
                case "5": CorrigirTudo(true); Ui.EnterButton(); break;
                case "6": return;
            }
        }
    }

    private static int CorrigirPerfil(bool interactive) =>
        ErrorRepair.RepararPerfil(_cfg, DetectAppliedMode, interactive);

    private static int CorrigirDiagnostico(bool interactive) =>
        ErrorRepair.Diagnostico(_cfg, DetectAppliedMode, interactive);

    private static int CorrigirPermissoes(bool interactive)
    {
        if (_cfg is null) return 1;

        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("CORRIGIR ERROS", Ui.MAmber);
            Ui.StepsPanel("ESTA OPCAO TENTA CORRIGIR", new[]
            {
                "Rocket League aberto (impede gravar ao fechar)",
                "Arquivo INI travado (read-only / ACL de script antigo)",
                "Pasta bloqueada (Defender / Acesso Controlado)",
                "Caminho do .ini errado (OneDrive / outro perfil)",
                "Falta de permissao (eleva admin via UAC se precisar)"
            }, Ui.MAmber);
        }

        if (GetRl().Length > 0)
        {
            if (interactive)
            {
                Ui.Gap();
                Ui.PanelTop("ROCKET LEAGUE ABERTO");
                Ui.PanelLine(Ui.C("O jogo sobrescreve o .ini ao fechar.", Ui.Gray));
                Ui.PanelBottom();
                Ui.Prompt("Fechar o jogo agora? (S/N)");
                if (IsYes(Console.ReadLine()))
                {
                    foreach (var p in GetRl()) { try { p.Kill(); } catch { } }
                    for (int i = 0; i < 4; i++)
                    {
                        Thread.Sleep(500);
                        if (GetRl().Length == 0) break;
                    }
                }
            }
            else if (GetRl().Length > 0)
                return 1;
        }

        bool UnlockIni()
        {
            try { Unlock(_cfg!); return true; } catch { return false; }
        }

        bool RefreshIniPath()
        {
            var found = ResolveConfigPath();
            if (found is null) return false;
            _cfg = found;
            return true;
        }

        if (interactive)
        {
            Ui.StepAnimated("Destravando TASystemSettings.ini", UnlockIni);
            Ui.StepAnimated("Re-localizando arquivo do jogo", RefreshIniPath);
        }
        else
        {
            UnlockIni();
            RefreshIniPath();
        }

        bool ok = FolderAccess.RunHealMode(_cfg!, interactive, showBanner: false);
        RefreshWritableCache();

        if (interactive)
        {
            if (ok && IsCfgWritable())
            {
                Ui.CompletionMessage(Ui.OkGreen, "CORRIGIDO", new[]
                {
                    "Consegui gravar na pasta do jogo.",
                    "Arquivo: " + FitPath(_cfg!, 48),
                    "Aplique COMPLETO ou CRIADOR de novo."
                });
            }
            else if (!ok)
            {
                Ui.CompletionMessage(Ui.MRed, "AINDA COM ERRO", new[]
                {
                    "Nao consegui liberar tudo automaticamente.",
                    "Siga o guia do Defender/antivirus acima.",
                    "Ou rode o GuttyTECH_RL.exe como administrador."
                });
            }
        }

        return ok && IsCfgWritable() ? 0 : 1;
    }

    private static int CorrigirBoot(bool interactive)
    {
        if (!CheckGame(interactive)) return 1;
        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("RECUPERAR BOOT", Ui.MAmber);
            Ui.StepsPanel("ULTIMO RECURSO — JOGO NAO ABRE", new[]
            {
                "REMOVE o otimizador (volta INI stock/original)",
                "Restaura save Epic+Steam do backup mais antigo",
                "Purga RLSettingsData (cache Epic)",
                "Se o jogo ABRE mas o menu esta errado: use REPARAR PERFIL",
            }, Ui.MAmber);
            Ui.Gap();
            Ui.Prompt("Confirma recuperar boot STOCK? (S/N)");
            if (!IsYes(Console.ReadLine()))
                return 1;

            string? existing = DetectAppliedMode();
            if (existing is "COMPLETO" or "CRIADOR")
            {
                Ui.Gap();
                Ui.PanelTop("MODO " + existing + " DETETADO");
                Ui.PanelLine(Ui.C("Em vez de stock, posso REPARAR o perfil (mantem FPS).", Ui.Green));
                Ui.PanelBottom();
                Ui.Prompt("Reparar perfil em vez de stock? (S/N)");
                if (IsYes(Console.ReadLine()))
                    return CorrigirPerfil(true);
            }
        }

        if (!EnsureConfigDir())
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta Config do RL nao existe.", "Abra o jogo 1x pela Epic." });
            return 1;
        }

        if (File.Exists(_cfg!))
        {
            if (!FolderAccess.EnsureWriteAccess(_cfg!, interactive)) return 1;
        }
        else if (!FolderAccess.EnsureWriteAccess(Path.GetDirectoryName(_cfg!)!, interactive))
        {
            return 1;
        }

        bool iniOk;
        bool saveOk;
        if (interactive)
        {
            Ui.StepAnimated("Destravando o arquivo", () => { if (File.Exists(_cfg!)) Unlock(_cfg!); return true; });
            Ui.StepAnimated("Backup de seguranca", () => { if (File.Exists(_cfg!)) Backup(); return true; });
            iniOk = Ui.StepAnimated("Restaurando INI stock (boot)", TryRestoreIni);
            saveOk = Ui.StepAnimated("Restaurando save Epic/Steam + cache", () => SaveRecovery.FullRecovery(_cfg!));
        }
        else
        {
            if (File.Exists(_cfg!)) { Unlock(_cfg!); Backup(); }
            iniOk = TryRestoreIni();
            saveOk = SaveRecovery.FullRecovery(_cfg!);
        }

        VideoSettingsSync.StopExistingWatchers();
        Log("CORRIGIR-BOOT concluido.");
        RefreshWritableCache();
        if (interactive)
        {
            Ui.CompletionMessage(iniOk && saveOk ? Ui.OkGreen : Ui.MAmber, "BOOT RECUPERADO (STOCK)", new[]
            {
                "INI e saves restaurados — otimizador removido.",
                "Abra o Rocket League e confirme que entra no menu.",
                "Depois aplique COMPLETO ou CRIADOR de novo.",
                "Presets sumiram? Menu [6] RESTAURAR PRESETS.",
            });
        }
        return iniOk && saveOk ? 0 : 1;
    }

    private static int CorrigirTudo(bool interactive)
    {
        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("CORRIGIR TUDO", Ui.MAmber);
            Ui.StepsPanel("ORDEM INTELIGENTE", new[]
            {
                "1) Permissoes / Defender / ACL",
                "2) Se ha COMPLETO/CRIADOR → REPARAR PERFIL",
                "3) Se nao ha modo → RECUPERAR BOOT stock",
            }, Ui.MAmber);
        }

        int a = CorrigirPermissoes(interactive);
        string? mode = DetectAppliedMode();
        int b;
        if (mode is "COMPLETO" or "CRIADOR")
            b = CorrigirPerfil(interactive);
        else
            b = CorrigirBoot(interactive);
        return a == 0 && b == 0 ? 0 : 1;
    }

    private static int Dispatch(string mode, bool interactive)
    {
        if (mode == "REMOVER") return Remover(interactive);
        if (mode is "CORRIGIR-BOOT" or "RECUPERAR") return CorrigirBoot(interactive);
        if (mode == "CORRIGIR-TUDO") return CorrigirTudo(interactive);
        if (mode is "CORRIGIR-PERFIL" or "REPARAR" or "REPARAR-PERFIL") return CorrigirPerfil(interactive);
        if (mode is "DIAGNOSTICO" or "DIAG") return CorrigirDiagnostico(interactive);
        if (mode is "RESTAURAR-PRESETS" or "RESTAURAR-SAVES") return RestoreLatestSaves(interactive);
        if (mode is "COMPLETO" or "CRIADOR") return Apply(mode, interactive);
        if (mode is "CORRIGIR" or "HEAL") return CorrigirPermissoes(interactive);
        Ui.SectionTitle("ARGUMENTO INVALIDO", Ui.Amber);
        Console.WriteLine(Ui.C("  Use: GuttyTECH_RL.exe [COMPLETO | CRIADOR | REMOVER | CORRIGIR | CORRIGIR-PERFIL | CORRIGIR-BOOT | DIAG | RESTAURAR-PRESETS]", Ui.Gray));
        return 2;
    }

    // -------------------------------------------------------------- Menu
    private static void ShowMenu()
    {
        Ui.HideCursor();
        Ui.Cls();
        Ui.Banner(false);
        RefreshWritableCache();

        var (label, locked, cat) = ReadState();
        var modeAccent = cat == 2 ? Ui.Green : cat == 1 ? Ui.Cyan : Ui.Gray;
        bool writable = IsCfgWritable();
        bool rlOpen = GetRl().Length > 0;
        string applied = DetectAppliedMode() ?? "";

        Ui.PanelTop("STATUS");
        Ui.PanelBlank();
        string chips =
            Ui.Chip(string.IsNullOrEmpty(applied) ? "ORIGINAL" : applied,
                string.IsNullOrEmpty(applied) ? Ui.BorderHi : (applied == "COMPLETO" ? Ui.Red : Ui.Cyan))
            + "  "
            + Ui.Chip(writable ? "GRAVAVEL" : "BLOQUEADO", writable ? (20, 80, 40) : Ui.Red)
            + "  "
            + Ui.Chip(rlOpen ? "RL ABERTO" : "RL FECHADO", rlOpen ? (90, 60, 10) : (24, 48, 36));
        Ui.PanelLine("  " + chips);
        Ui.PanelBlank();
        Ui.PanelLine(Ui.Field("Arquivo", Ui.C(FitPath(_cfg!, 58), Ui.Gray)));
        Ui.PanelLine(Ui.Field("Estado", Ui.Dot(modeAccent, label)));
        string trava = locked ? Ui.Dot(Ui.Green, "SIM") : Ui.Dot(Ui.Amber, "NAO");
        string adm = IsAdmin() ? Ui.Dot(Ui.Green, "SIM") : Ui.Dot(Ui.Gray, "nao necessario");
        Ui.PanelLine(Ui.Field("Protegido", trava + "    " + Ui.C("Admin ", Ui.DimC) + adm));
        Ui.PanelBottom();
        Ui.Gap();

        Ui.PanelTop("MODOS");
        Ui.PanelBlank();
        Ui.MenuCard("1", "COMPLETO", "FPS maximo - grafico de batata - menu High Performance", Ui.Red, "FPS");
        Ui.MenuCard("2", "CRIADOR DE CONTEUDO", "Otimizacoes fortes mantendo o visual bonito", Ui.Cyan, "STREAM");
        Ui.MenuCard("3", "REMOVER", "Restaura so o INI (preserva presets do carro)", Ui.Amber);
        Ui.MenuCard("4", "COMANDO DE INICIALIZACAO", "Copia o comando mais foda p/ Steam ou Epic", Ui.Cyan);
        Ui.MenuCard("5", "CORRIGIR ERROS", "Reparar perfil, boot, permissoes, diagnostico", Ui.Amber);
        Ui.MenuCard("6", "RESTAURAR PRESETS", "Garagem: backup mais recente -> Epic automatico", Ui.Amber);
        Ui.MenuCard("7", "SAIR", "Fechar o GuttyRL", Ui.DimC);
        Ui.PanelBlank();
        Ui.PanelBottom();
        Ui.FooterHint("COMPLETO/CRIADOR: limpa + aplica + sync rapido (perfis recentes)");
        Ui.Prompt("Escolha (1-7)");
    }

    private static string FitPath(string p, int max)
        => p.Length <= max ? p : "..." + p[^(max - 3)..];

    // -------------------------------------------------------------- Launch Options
    // Pesquisado/validado no RL (UE3): boot + micro-FPS in-game. EAC-safe para online.
    private const string LaunchRecommended = "-nomovie -NOSPLASH -nomansky +mat_antialias 0 -high";
    private const string LaunchNoPriority = "-nomovie -NOSPLASH -nomansky +mat_antialias 0";

    private static void LaunchOptions()
    {
        while (true)
        {
            Ui.HideCursor();
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MCyan);
            Ui.TitleBar("COMANDO DE INICIALIZACAO", Ui.MCyan);
            Console.WriteLine();
            Ui.LaunchParam("[1]", Ui.MCyan, "STEAM", "como colar na Steam (passo a passo)");
            Ui.LaunchParam("[2]", Ui.MCyan, "EPIC GAMES", "como colar na Epic (passo a passo)");
            Ui.LaunchParam("[3]", Ui.DarkGray, "VOLTAR", "menu principal");
            Ui.Prompt("Escolha (1-3)");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": ShowPlatform("STEAM", LaunchRecommended, true); break;
                case "2": ShowPlatform("EPIC GAMES", LaunchRecommended, false); break;
                case "3": return;
            }
        }
    }

    private static void ShowPlatform(string platform, string cmd, bool isSteam)
    {
        Ui.HideCursor();
        Ui.Cls();
        Ui.MiniBannerIfTall(Ui.MCyan);
        Ui.TitleBar(platform + " - COMANDO DE INICIALIZACAO", Ui.MCyan);

        string[] steps = isSteam
            ? new[]
            {
                "1. Steam > botao direito no Rocket League > Propriedades",
                "2. Geral > Opcoes de Inicializacao",
                "3. Cole (Ctrl+V) e feche — NAO use %command%"
            }
            : new[]
            {
                "1. Epic > Biblioteca > ... no Rocket League > Gerenciar",
                "2. Marque 'Argumentos de linha de comando adicionais'",
                "3. Cole (Ctrl+V) o comando e salve"
            };
        Ui.StepsPanel("PASSO A PASSO", steps, Ui.MCyan);

        Ui.Gap();
        Ui.LaunchHeading("COPIAR E COLAR");
        Ui.CopyStatus(CopyToClipboard(cmd));
        Ui.CodeBox(cmd);

        Ui.LaunchHeading("O que cada flag faz");
        Ui.LaunchParam("+", Ui.OkGreen, "-nomovie", "pula intro");
        Ui.LaunchParam("+", Ui.OkGreen, "-NOSPLASH", "pula splash");
        Ui.LaunchParam("+", Ui.OkGreen, "-nomansky", "ceu leve");
        Ui.LaunchParam("+", Ui.OkGreen, "+mat_antialias 0", "AA zero");
        Ui.LaunchParam("+", Ui.OkGreen, "-high", "prioridade alta (tire se estalar)");
        Ui.LaunchNote("Sem -high: " + LaunchNoPriority);

        Ui.EnterButton();
    }

    private static bool CopyToClipboard(string text)
    {
        try
        {
            var psi = new ProcessStartInfo("clip.exe")
            { RedirectStandardInput = true, UseShellExecute = false, CreateNoWindow = true };
            using var p = Process.Start(psi);
            if (p is null) return false;
            p.StandardInput.Write(text);
            p.StandardInput.Close();
            p.WaitForExit(3000);
            return p.ExitCode == 0;
        }
        catch { return false; }
    }

    // -------------------------------------------------------------- Apply
    private static int Apply(string mode, bool interactive)
    {
        if (!CheckGame(interactive)) return 1;
        if (!File.Exists(_cfg!) && !EnsureConfigDir())
            return FailOrElevate(mode, interactive);
        var acc = Ui.ModeColor(mode);
        if (interactive) { Ui.Cls(); Ui.MiniBannerIfTall(acc); Ui.TitleBar("APLICANDO MODO " + mode, acc); }
        if (File.Exists(_cfg!))
        {
            if (!FolderAccess.EnsureWriteAccess(_cfg!, interactive)) return 1;
        }
        else if (!FolderAccess.EnsureWriteAccess(Path.GetDirectoryName(_cfg!)!, interactive))
        {
            return FailOrElevate(mode, interactive);
        }

        EnsureOriginalBackup();

        // Sempre limpa como REMOVER antes de aplicar.
        // Troca de conta cria .save novo com VideoOptions vazio → menu Alta qualidade /
        // 60 FPS / tela preta longa; limpar + regravar INI+save em todos os perfis.
        string? previous = DetectAppliedMode();

        // IMPORTANTE: ler resolucao/borda ANTES do restore.
        // Se ler depois, volta ao Fullscreen do backup original e perde Sem bordas.
        var disp = File.Exists(_cfg!) ? ReadDisplay(_cfg!) : DefaultDisplay();

        bool UnlockCfg() { try { File.SetAttributes(_cfg!, FileAttributes.Normal); } catch { } return true; }

        if (interactive)
        {
            Ui.FlushInput();
            Ui.StepAnimated("Backup de seguranca", () => { Backup(); return true; });
            Ui.StepAnimated("Destravando o arquivo", () => { Unlock(_cfg!); return true; });
            if (!Ui.StepAnimated("Limpando (como REMOVER) antes de aplicar", TryRestoreIni))
                return FailOrElevate(mode, interactive);
        }
        else
        {
            Backup();
            Unlock(_cfg!);
            if (!TryRestoreIni()) return FailOrElevate(mode, interactive);
        }

        string template = mode == "COMPLETO" ? Templates.Completo : Templates.Criador;
        string content = ApplyDisplay(template, disp);
        if (mode == "COMPLETO")
            content = CompletoForce.Apply(content);
        else
            content = CriadorForce.Apply(content);
        content = EnsureModeMarker(content, mode);
        // Nao herdar CriadorUserKeys do INI stock pos-REMOVER — o template manda.

        var pacing = ReadSectionOverridesFromText(template, FramePacingKeys, textureGroups: false);
        content = ApplySectionOverrides(content, pacing);
        var locked = ReadSectionOverridesFromText(template, VideoLockedKeys, textureGroups: false);
        content = ApplySectionOverrides(content, locked);

        if (interactive)
        {
            if (!Ui.StepAnimated("Gravando otimizacao", () => DoWrite(content, mode))) return FailOrElevate(mode, interactive);
            if (!Ui.StepWithBar("Sincronizando menu de video", bar =>
                    VideoSettingsSync.SyncVideoSave(_cfg!, mode, interactive, bar)))
            {
                Ui.CompletionMessage(acc, "AVISO", new[]
                {
                    "INI gravado, mas o menu de video nao sincronizou.",
                    "Feche o RL e rode o modo de novo.",
                });
                return 1;
            }
            if (mode is "CRIADOR" or "COMPLETO")
                Ui.StepAnimated("Mantendo video ajustavel no jogo", UnlockCfg);
            Ui.FlushInput();
        }
        else
        {
            if (!DoWrite(content, mode)) return FailOrElevate(mode, interactive);
            if (!VideoSettingsSync.SyncVideoSave(_cfg!, mode, interactive)) return 1;
            if (mode is "CRIADOR" or "COMPLETO") UnlockCfg();
        }

        string msg = previous is null ? $"Aplicado {mode} (limpo)."
            : previous.Equals(mode, StringComparison.OrdinalIgnoreCase)
                ? $"Reaplicado {mode} (limpo + sync contas)."
                : $"Trocou {previous} → {mode} (limpo + sync contas).";
        Log(msg);
        RefreshWritableCache();
        // Watcher: quando o utilizador abrir/fechar o RL, repara o que o boot apagar.
        VideoSettingsSync.StartExitWatcher(mode);
        if (interactive)
        {
            Ui.CompletionSuccess(mode, acc, AppMeta.BackupDir);
            Console.WriteLine();
            string m = new string(' ', Ui.Margin);
            Console.WriteLine(m + Ui.C("  Watcher ativo: ao fechar o RL, o otimizador repara INI+menu sozinho.", Ui.DimC));
        }
        return 0;
    }

    private static string? DetectAppliedMode()
    {
        if (_cfg is null || !File.Exists(_cfg)) return null;
        try
        {
            string text = File.ReadAllText(_cfg);
            // Chave real (sobrevive ao APLICAR do jogo) + comentario antigo.
            if (text.Contains("GuttyTechMode=COMPLETO", StringComparison.OrdinalIgnoreCase)
                || text.Contains("GUTTYTECH-RL-OPTIMIZER=COMPLETO", StringComparison.Ordinal))
                return "COMPLETO";
            if (text.Contains("GuttyTechMode=CRIADOR", StringComparison.OrdinalIgnoreCase)
                || text.Contains("GUTTYTECH-RL-OPTIMIZER=CRIADOR", StringComparison.Ordinal))
                return "CRIADOR";
        }
        catch { }
        return null;
    }

    private static void RefreshWritableCache()
    {
        if (_cfg is null) { _cfgWritable = null; return; }
        try { _cfgWritable = FolderAccess.CanWriteToDirectory(Path.GetDirectoryName(_cfg!)!); }
        catch { _cfgWritable = false; }
    }

    private static bool IsCfgWritable()
    {
        if (_cfgWritable is bool b) return b;
        RefreshWritableCache();
        return _cfgWritable == true;
    }

    private static string MissingIniHint()
    {
        foreach (var root in DocumentRoots())
        {
            string cfgDir = Path.Combine(root, @"My Games\Rocket League\TAGame\Config");
            if (Directory.Exists(cfgDir))
                return "A pasta do jogo existe, mas o .ini sumiu. Abra o Rocket League 1x.";
        }
        return "Abra o Rocket League uma vez para ele criar o arquivo e rode de novo.";
    }

    /// <summary>Marcador que o RL nao apaga no APLICAR (comentario ; some).</summary>
    private static string EnsureModeMarker(string content, string mode)
    {
        string keyLine = "GuttyTechMode=" + mode;
        if (content.Contains("GuttyTechMode=", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new StringBuilder();
            foreach (var raw in content.Replace("\r\n", "\n").Split('\n'))
            {
                if (raw.StartsWith("GuttyTechMode=", StringComparison.OrdinalIgnoreCase))
                    sb.Append(keyLine).Append("\r\n");
                else
                    sb.Append(raw).Append("\r\n");
            }
            return sb.ToString();
        }

        const string hdr = "[SystemSettings]";
        int idx = content.IndexOf(hdr, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return keyLine + "\r\n" + content;
        int insert = idx + hdr.Length;
        if (insert < content.Length && content[insert] == '\r') insert++;
        if (insert < content.Length && content[insert] == '\n') insert++;
        return content[..insert] + keyLine + "\r\n" + content[insert..];
    }

    private static bool DoWrite(string content, string mode)
    {
        try
        {
            if (File.Exists(_cfg!)) File.Delete(_cfg!);
            File.WriteAllText(_cfg!, content, new UTF8Encoding(false));
            string written = File.ReadAllText(_cfg!);
            return written.Contains("GuttyTechMode=" + mode, StringComparison.OrdinalIgnoreCase)
                || written.Contains("GUTTYTECH-RL-OPTIMIZER=" + mode, StringComparison.Ordinal);
        }
        catch { return false; }
    }

    // -------------------------------------------------------------- Remover
    private static int Remover(bool interactive)
    {
        if (!CheckGame(interactive)) return 1;
        if (interactive) { Ui.Cls(); Ui.MiniBannerIfTall(Ui.MAmber); Ui.TitleBar("REMOVENDO / RESTAURANDO", Ui.MAmber); }

        if (!EnsureConfigDir())
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta Config do RL nao existe.", "Abra o jogo 1x pela Epic." });
            return 1;
        }

        if (File.Exists(_cfg!))
        {
            if (!FolderAccess.EnsureWriteAccess(_cfg!, interactive)) return 1;
        }
        else if (!FolderAccess.EnsureWriteAccess(Path.GetDirectoryName(_cfg!)!, interactive))
        {
            return 1;
        }

        if (interactive)
        {
            Ui.StepAnimated("Destravando o arquivo", () => { if (File.Exists(_cfg!)) Unlock(_cfg!); return true; });
            Ui.StepAnimated("Backup de seguranca", () => { if (File.Exists(_cfg!)) Backup(); return true; });
            if (!Ui.StepAnimated("Restaurando INI (sem tocar no save)", TryRestoreIni))
                return FailOrElevate("REMOVER", interactive);
            Log("REMOVER concluido (so INI).");
            RefreshWritableCache();
            Ui.CompletionMessage(Ui.MAmber, "RESTAURADO", new[]
            {
                "INI restaurado. Save Epic e presets do carro intactos.",
                "Menu errado? [5] CORRIGIR ERROS -> REPARAR PERFIL.",
                "Jogo nao abre? [5] -> RECUPERAR BOOT (stock).",
            });
        }
        else
        {
            if (File.Exists(_cfg!)) { Unlock(_cfg!); Backup(); }
            if (!TryRestoreIni()) return FailOrElevate("REMOVER", interactive);
            Log("REMOVER concluido (so INI).");
            RefreshWritableCache();
        }
        return 0;
    }

    private static bool TryRestoreIni()
    {
        try
        {
            bool fromOriginal = File.Exists(AppMeta.OrigBackup);
            if (fromOriginal)
            {
                if (File.Exists(_cfg!)) File.Delete(_cfg!);
                File.Copy(AppMeta.OrigBackup, _cfg!, true);
            }
            else if (File.Exists(_cfg!))
            {
                var disp = ReadDisplay(_cfg!);
                string content = ApplyDisplay(Templates.Stock, disp);
                File.Delete(_cfg!);
                File.WriteAllText(_cfg!, content, new UTF8Encoding(false));
            }
            else
            {
                string content = ApplyDisplay(Templates.Stock, DefaultDisplay());
                File.WriteAllText(_cfg!, content, new UTF8Encoding(false));
            }
            try { File.SetAttributes(_cfg!, FileAttributes.Normal); } catch { }
            return true;
        }
        catch { return false; }
    }

    private static int RestoreLatestSaves(bool interactive)
    {
        if (!CheckGame(interactive)) return 1;
        string? cfg = _cfg ?? ResolveConfigPath();
        if (cfg is null)
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta Config do RL nao encontrada." });
            return 1;
        }

        string? saveDir = SaveRecovery.SaveDirFromIni(cfg);
        if (saveDir is null)
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta SaveDataEpic nao encontrada." });
            return 1;
        }

        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("RESTAURAR PRESETS", Ui.MAmber);
            Ui.StepsPanel("GARAGEM / PRESETS DO CARRO", new[]
            {
                "Pega o backup mais recente em GuttyTECH\\Backups",
                "Copia automatico para SaveDataEpic da Epic",
                "Nao precisa abrir pasta manualmente",
            }, Ui.MAmber);
        }

        bool ok;
        if (interactive)
        {
            ok = Ui.StepAnimated("Copiando backup -> Epic SaveDataEpic", () => SaveRecovery.RestoreLatestBackup(cfg));
        }
        else
        {
            ok = SaveRecovery.RestoreLatestBackup(cfg);
        }

        if (interactive)
        {
            Ui.CompletionMessage(ok ? Ui.OkGreen : Ui.MRed, ok ? "PRESETS RESTAURADOS" : "FALHOU", ok
                ? new[]
                {
                    "Save copiado para:",
                    FitPath(saveDir, 52),
                    "Abra o Rocket League e confira a garagem.",
                }
                : new[]
                {
                    "Nenhum backup em GuttyTECH\\Backups\\SaveDataEpic",
                    "Epic Launcher -> Verificar arquivos do RL",
                });
        }
        Log(ok ? $"RESTAURAR-PRESETS: save copiado para {saveDir}" : "RESTAURAR-PRESETS: sem backup.");
        return ok ? 0 : 1;
    }

    private static Dictionary<string, string> DefaultDisplay() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["ResX"] = "1920",
        ["ResY"] = "1080",
        ["Fullscreen"] = "True",
        ["Borderless"] = "False",
        ["AutoDetectDesktopResolution"] = "False",
    };

    private static bool EnsureConfigDir()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_cfg!);
            if (string.IsNullOrEmpty(dir)) return false;
            Directory.CreateDirectory(dir);
            return true;
        }
        catch { return false; }
    }

    private static string? ResolveConfigPath()
    {
        string? ov = Environment.GetEnvironmentVariable("GUTTYRL_INI");
        if (!string.IsNullOrEmpty(ov)) return ov;

        foreach (var root in DocumentRoots())
        {
            string configDir = Path.Combine(root, @"My Games\Rocket League\TAGame\Config");
            if (Directory.Exists(configDir))
                return Path.Combine(configDir, "TASystemSettings.ini");
        }

        string up = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        try
        {
            string usersRoot = Path.GetDirectoryName(up)!;
            foreach (var u in Directory.GetDirectories(usersRoot))
            {
                foreach (var root in DocumentRootsForUser(u))
                {
                    string configDir = Path.Combine(root, @"My Games\Rocket League\TAGame\Config");
                    if (Directory.Exists(configDir))
                        return Path.Combine(configDir, "TASystemSettings.ini");
                }
            }
        }
        catch { }

        return FindIni();
    }

    // -------------------------------------------------------------- Find INI
    private static string? FindIni()
    {
        string? ov = Environment.GetEnvironmentVariable("GUTTYRL_INI");
        if (!string.IsNullOrEmpty(ov) && SafeExists(ov)) return ov;

        var tried = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in DocumentRoots())
        {
            string p = Path.Combine(root, AppMeta.IniRelative);
            if (tried.Add(p) && SafeExists(p)) return p;
        }

        string up = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        try
        {
            string usersRoot = Path.GetDirectoryName(up)!;
            foreach (var u in Directory.GetDirectories(usersRoot))
            {
                foreach (var root in DocumentRootsForUser(u))
                {
                    string p = Path.Combine(root, AppMeta.IniRelative);
                    if (tried.Add(p) && SafeExists(p)) return p;
                }
            }
        }
        catch { }

        return FindIniDeepSearch(up);
    }

    private static IEnumerable<string> DocumentRoots()
        => DocumentRootsForUser(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    private static IEnumerable<string> DocumentRootsForUser(string userProfile)
    {
        var roots = new List<string>();
        void Add(string? p)
        {
            if (string.IsNullOrWhiteSpace(p)) return;
            try
            {
                if (Directory.Exists(p))
                    roots.Add(Path.GetFullPath(p));
            }
            catch { }
        }

        string current = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (userProfile.Equals(current, StringComparison.OrdinalIgnoreCase))
            Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        Add(Path.Combine(userProfile, "Documents"));

        if (userProfile.Equals(current, StringComparison.OrdinalIgnoreCase))
        {
            foreach (string env in new[] { "OneDrive", "OneDriveCommercial", "OneDriveConsumer" })
            {
                string? od = Environment.GetEnvironmentVariable(env);
                if (string.IsNullOrWhiteSpace(od)) continue;
                Add(Path.Combine(od, "Documents"));
                Add(od);
            }
        }

        Add(Path.Combine(userProfile, "OneDrive", "Documents"));
        Add(Path.Combine(userProfile, "OneDrive - Personal", "Documents"));
        Add(Path.Combine(userProfile, "OneDrive - Pessoal", "Documents"));

        try
        {
            foreach (var d in Directory.GetDirectories(userProfile, "OneDrive*"))
                Add(Path.Combine(d, "Documents"));
        }
        catch { }

        return roots.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? FindIniDeepSearch(string userProfile)
    {
        try
        {
            var opts = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                MaxRecursionDepth = 10,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint
            };
            foreach (var p in Directory.EnumerateFiles(userProfile, "TASystemSettings.ini", opts))
            {
                if (p.Contains(@"Rocket League\", StringComparison.OrdinalIgnoreCase)
                    || p.Contains(@"TAGame\Config\", StringComparison.OrdinalIgnoreCase))
                    return p;
            }
        }
        catch { }
        return null;
    }

    private static bool SafeExists(string p) { try { return File.Exists(p); } catch { return false; } }

    // -------------------------------------------------------------- State
    private static (string label, bool locked, int cat) ReadState()
    {
        string label = "Original / padrao (nao otimizado)";
        int cat = 0;
        try
        {
            string text = File.ReadAllText(_cfg!);
            if (text.Contains("GuttyTechMode=COMPLETO", StringComparison.OrdinalIgnoreCase)
                || text.Contains("GUTTYTECH-RL-OPTIMIZER=COMPLETO", StringComparison.Ordinal))
            { label = "COMPLETO aplicado (FPS maximo)"; cat = 2; }
            else if (text.Contains("GuttyTechMode=CRIADOR", StringComparison.OrdinalIgnoreCase)
                || text.Contains("GUTTYTECH-RL-OPTIMIZER=CRIADOR", StringComparison.Ordinal))
            { label = "CRIADOR aplicado (visual + perf)"; cat = 2; }
            else if (text.Contains("MaxLODSize=16")) { label = "Otimizado por versao antiga v21"; cat = 1; }
        }
        catch { }
        bool locked = false;
        try { locked = (File.GetAttributes(_cfg!) & FileAttributes.ReadOnly) != 0; } catch { }
        return (label, locked, cat);
    }

    private static bool IsAdmin()
    {
        try
        {
            using var id = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    // -------------------------------------------------------------- Helpers
    private static bool CheckGame(bool interactive)
    {
        if (Environment.GetEnvironmentVariable("GUTTYRL_SKIP_GAMECHECK") == "1") return true;
        if (GetRl().Length == 0) return true;
        Ui.SectionTitle("ROCKET LEAGUE ABERTO", Ui.Amber);
        Console.WriteLine(Ui.C("  O jogo sobrescreve o arquivo ao fechar. Feche-o antes de aplicar.", Ui.Gray));
        if (!interactive) { Console.WriteLine(Ui.C("  Feche o jogo e rode de novo.", Ui.Red)); return false; }
        Ui.Prompt("Fechar o jogo agora? (S/N)");
        if (!IsYes(Console.ReadLine())) return false;
        foreach (var p in GetRl()) { try { p.Kill(); } catch { } }
        for (int i = 0; i < 4; i++)
        {
            Thread.Sleep(500);
            if (GetRl().Length == 0) return true;
        }
        Console.WriteLine(Ui.C("  Nao consegui fechar. Feche manualmente.", Ui.Red));
        return false;
    }

    private static Process[] GetRl()
    { try { return Process.GetProcessesByName("RocketLeague"); } catch { return Array.Empty<Process>(); } }

    private static void Unlock(string path)
    {
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
        Run("takeown.exe", $"/f \"{path}\"");
        Run("icacls.exe", $"\"{path}\" /reset");
        Run("icacls.exe", $"\"{path}\" /grant \"{Environment.UserName}:(F)\" /c /q");
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
    }

    private static void LockReadOnly(string path)
    {
        try { File.SetAttributes(path, FileAttributes.Normal); } catch { }
        try { File.SetAttributes(path, FileAttributes.ReadOnly); } catch { }
    }

    private static void Run(string exe, string args)
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true };
            using var p = Process.Start(psi);
            p?.WaitForExit(8000);
        }
        catch { }
    }

    private static void EnsureOriginalBackup()
    {
        if (File.Exists(AppMeta.OrigBackup) || !File.Exists(_cfg!)) return;
        try
        {
            string text = File.ReadAllText(_cfg!);
            if (text.Contains("GUTTYTECH-RL-OPTIMIZER=") || text.Contains("MaxLODSize=16"))
            { Log("Arquivo atual ja otimizado; original nao capturado - usar stock no REMOVER."); return; }
            File.Copy(_cfg!, AppMeta.OrigBackup, true);
            try { File.SetAttributes(AppMeta.OrigBackup, FileAttributes.Normal); } catch { }
            Log("Backup original pristino criado.");
        }
        catch { }
    }

    private static void Backup()
    {
        try
        {
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(_cfg!, Path.Combine(AppMeta.BackupDir, $"TASystemSettings.{ts}.bak"), true);
        }
        catch { }
    }

    private static Dictionary<string, string> ReadDisplay(string file)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            bool inSs = false;
            foreach (var line in File.ReadAllLines(file))
            {
                if (line.StartsWith('[')) { inSs = line.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase); continue; }
                if (!inSs) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line[..eq];
                foreach (var dk in DisplayKeys)
                    if (key.Equals(dk, StringComparison.OrdinalIgnoreCase) && !d.ContainsKey(dk))
                        d[dk] = line[(eq + 1)..];
            }
        }
        catch { }
        return d;
    }

    private static string ApplyDisplay(string templateText, Dictionary<string, string> disp) =>
        ApplySectionOverrides(templateText, disp);

    private static Dictionary<string, string> ReadSectionOverrides(string file, string[] keys, bool textureGroups)
    {
        try { return ReadSectionOverridesFromText(File.ReadAllText(file), keys, textureGroups); }
        catch { return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); }
    }

    private static Dictionary<string, string> ReadSectionOverridesFromText(string iniText, string[] keys, bool textureGroups)
    {
        var keySet = new HashSet<string>(keys, StringComparer.OrdinalIgnoreCase);
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        bool inSs = false;
        foreach (var line in iniText.Replace("\r\n", "\n").Split('\n'))
        {
            if (line.StartsWith('[')) { inSs = line.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase); continue; }
            if (!inSs) continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string key = line[..eq];
            if (!d.ContainsKey(key) && (keySet.Contains(key) || (textureGroups && key.StartsWith("TEXTUREGROUP_", StringComparison.OrdinalIgnoreCase))))
                d[key] = line[(eq + 1)..];
        }
        return d;
    }

    private static string ApplySectionOverrides(string templateText, Dictionary<string, string> overrides)
    {
        if (overrides.Count == 0) return templateText;
        var sb = new StringBuilder();
        bool inSs = false;
        var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in templateText.Replace("\r\n", "\n").Split('\n'))
        {
            string outLine = raw;
            if (raw.StartsWith('[')) inSs = raw.Equals("[SystemSettings]", StringComparison.OrdinalIgnoreCase);
            else if (inSs)
            {
                int eq = raw.IndexOf('=');
                if (eq > 0)
                {
                    string key = raw[..eq];
                    if (overrides.TryGetValue(key, out var v) && done.Add(key))
                        outLine = key + "=" + v;
                }
            }
            sb.Append(outLine).Append("\r\n");
        }
        return sb.ToString();
    }

    private static int FailOrElevate(string mode, bool interactive)
    {
        if (IsAdmin()) { FailPanel(); return 1; }
        Ui.Gap();
        Ui.PanelTop("PRECISA DE ADMINISTRADOR");
        Ui.PanelLine(Ui.C("O arquivo pode estar travado a nivel de SISTEMA", Ui.Amber));
        Ui.PanelLine(Ui.C("por um script antigo que rodou como admin.", Ui.Gray));
        Ui.PanelBottom();
        if (interactive)
        {
            Ui.Prompt("Elevar e tentar como administrador? (S/N)");
            if (!IsYes(Console.ReadLine())) { FailPanel(); return 1; }
        }
        try
        {
            var psi = new ProcessStartInfo(Environment.ProcessPath ?? "")
            { UseShellExecute = true, Verb = "runas", Arguments = mode + " /keepopen" };
            Process.Start(psi);
            Console.WriteLine(Ui.C("  » Abri uma janela de administrador. Pode fechar esta.", Ui.Green));
        }
        catch
        {
            Console.WriteLine(Ui.C("  X Nao consegui elevar. Rode o GuttyTECH_RL.exe como administrador.", Ui.Red));
        }
        return 1;
    }

    // -------------------------------------------------------------- Painel de falha
    private static void FailPanel()
    {
        Ui.CompletionMessage(Ui.MRed, "FALHA", new[]
        {
            "Nao consegui aplicar. Seu arquivo NAO foi corrompido.",
            "Backup em: " + FitPath(AppMeta.BackupDir, 45),
            "Cheque antivirus / Acesso Controlado a Pastas."
        });
    }

    private static void Goodbye()
    {
        Ui.Gap();
        Console.WriteLine(Ui.C(new string(' ', Ui.Margin) + "GUTTYTECH - TESSERACT  ", Ui.Red) + Ui.C("// ate a proxima.", Ui.DimC));
        Ui.Gap();
    }

    private static void Log(string msg) => AppMeta.Log(msg);

    private static bool IsYes(string? s) => string.Equals(s?.Trim(), "S", StringComparison.OrdinalIgnoreCase);
}
