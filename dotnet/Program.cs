using System.Diagnostics;
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
        "UncappedFramerate", "bSmoothFrameRate", "CustomFPS", "UseVsync",
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

    [STAThread]
    private static int Main(string[] args)
    {
        // Extracao estavel do single-file (evita TEMP limpo pelo AV / Edge cache).
        try
        {
            string extract = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "GuttyTECH", "RL-Optimizer-v22", "bundle-extract");
            Directory.CreateDirectory(extract);
            Environment.SetEnvironmentVariable("DOTNET_BUNDLE_EXTRACT_BASE_DIR", extract);
        }
        catch { }

        if (args.Length == 0)
            ConsoleWindowService.Hide();
        else
            ConsoleWindowService.PrepareForCli(
                args[0].Equals("CONSOLE", StringComparison.OrdinalIgnoreCase));
        StartupGuard.Install();
        return StartupGuard.Run(() => Run(args));
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0)
            return RunDesktop();

        if (args[0].Equals("CONSOLE", StringComparison.OrdinalIgnoreCase))
            args = Array.Empty<string>();

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
            Ui.PanelLine(Ui.Dot(Ui.Red, "Pasta do Rocket League não encontrada."));
            Ui.PanelLine(Ui.C("Abra o jogo uma vez pela Epic para criar as pastas.", Ui.Gray));
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

    private static int RunDesktop()
    {
        try
        {
            var application = new App();
            application.DispatcherUnhandledException += (_, e) =>
            {
                StartupGuard.ReportFatal("Erro na interface do GuttyRL.", e.Exception);
                e.Handled = true;
                try { application.Shutdown(99); } catch { }
            };
            var window = new MainWindow();
            return application.Run(window);
        }
        catch (Exception ex)
        {
            StartupGuard.ReportFatal("Falha ao abrir a janela do GuttyRL.", ex);
            return 99;
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
                "Jogo NAO abre → [3] RECUPERAR BOOT ou [5] CORRIGIR TUDO (nuclear)",
                "Jogo abre mas menu High Quality / 60 FPS → [2] REPARAR PERFIL",
                "Pasta bloqueada / Defender → [1] PERMISSOES",
                "Presets do carro: menu principal [6]",
            }, Ui.MAmber);
            Ui.Gap();
            Ui.PanelTop("OPCOES");
            Ui.PanelBlank();
            Ui.MenuCard("1", "PERMISSOES", "Destrava INI e libera gravacao na pasta", Ui.Amber);
            Ui.MenuCard("2", "REPARAR PERFIL", "Mantem COMPLETO/CRIADOR — reclampa INI+menu", Ui.Green);
            Ui.MenuCard("3", "RECUPERAR BOOT", "Nuclear: stock INI + quarentena + EAC 30005", Ui.Amber);
            Ui.MenuCard("4", "REPARAR EAC", "Erro 30005 CreateService 1072 (Easy Anti-Cheat)", Ui.Amber);
            Ui.MenuCard("5", "DIAGNOSTICO", "Mostra o que esta errado no INI/pasta/EAC", Ui.Cyan);
            Ui.MenuCard("6", "TUDO", "Desbloqueio nuclear — prioridade abrir o jogo", Ui.Amber);
            Ui.MenuCard("7", "VOLTAR", "Menu principal", Ui.DimC);
            Ui.PanelBlank();
            Ui.PanelBottom();
            Ui.Prompt("Selecione uma operação  [1–7]");
            switch (Console.ReadLine()?.Trim())
            {
                case "1": CorrigirPermissoes(true); Ui.EnterButton(); break;
                case "2": CorrigirPerfil(true); Ui.EnterButton(); break;
                case "3": CorrigirBoot(true); Ui.EnterButton(); break;
                case "4": ErrorRepair.RepararEac(true); Ui.EnterButton(); break;
                case "5": CorrigirDiagnostico(true); Ui.EnterButton(); break;
                case "6": CorrigirTudo(true); Ui.EnterButton(); break;
                case "7": return;
            }
        }
    }

    private static int CorrigirPerfil(bool interactive) =>
        ErrorRepair.RepararPerfil(_cfg, DetectAppliedMode, interactive);

    private static int CorrigirDiagnostico(bool interactive)
    {
        // CLI e GUI: sempre gera o ZIP completo (o botão GERAR PACOTE usa o mesmo motor).
        EnsureEngineInitializedForGui();
        OptimizerStatus status = GetStatusForGui();
        SupportLogService.PackResult pack = SupportLogService.CreateSupportPack(_cfg, DetectAppliedMode, status);
        AppMeta.Log("DIAG-PACK: " + pack.Summary);

        if (interactive)
        {
            ErrorRepair.Diagnostico(_cfg, DetectAppliedMode, interactive: true);
            Ui.Gap();
            if (pack.Success)
            {
                Ui.CompletionMessage(Ui.OkGreen, "PACOTE GERADO", new[]
                {
                    pack.Summary,
                    pack.ZipPath,
                });
            }
            else
            {
                Ui.CompletionMessage(Ui.MAmber, "DIAG OK — ZIP FALHOU", new[] { pack.Summary });
            }
        }

        return pack.Success ? 0 : 1;
    }

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
        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("RECUPERAR BOOT", Ui.MAmber);
            Ui.StepsPanel("ULTIMO RECURSO — JOGO NAO ABRE", new[]
            {
                "Fecha o RL e o watcher automatico",
                "REMOVE o otimizador (INI stock/original)",
                "Remove boot-killers (OnlyStream/WaitForGPU)",
                "Quarentena saves suspeitos (garagem fica no cofre Best)",
                "Purga RLSettingsData — NAO reaplica COMPLETO/CRIADOR",
            }, Ui.MAmber);
            Ui.Gap();
            Ui.Prompt("Confirma recuperar boot STOCK? (S/N)");
            if (!IsYes(Console.ReadLine()))
                return 1;
        }

        if (!EnsureConfigDir())
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta Config do RL nao existe.", "Abra o jogo 1x pela Epic/Steam." });
            return 1;
        }

        int code = ErrorRepair.UnbreakBoot(
            _cfg,
            TryRestoreIni,
            () => { if (_cfg is not null && File.Exists(_cfg)) Unlock(_cfg); },
            interactive);
        RefreshWritableCache();
        ModeDetect.Clear();
        Log("CORRIGIR-BOOT concluido code=" + code);
        return code;
    }

    private static int CorrigirTudo(bool interactive)
    {
        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("CORRIGIR TUDO", Ui.MAmber);
            Ui.StepsPanel("PRIORIDADE: FAZER O JOGO ABRIR", new[]
            {
                "1) Fecha RL + watcher",
                "2) Liberta pasta / permissoes",
                "3) INI stock + remocao de boot-killers",
                "4) Quarentena de saves + purge cache Epic + EAC",
                "5) Depois que abrir: RESTAURAR PRESETS (obrigatorio se quer a garagem)",
                "6) So entao reaplique COMPLETO/CRIADOR",
            }, Ui.MAmber);
            Ui.Gap();
            Ui.Prompt("Executar correcao nuclear agora? (S/N)");
            if (!IsYes(Console.ReadLine()))
                return 1;
        }

        if (!EnsureConfigDir())
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta Config do RL nao existe.", "Abra o jogo 1x pela Epic/Steam." });
            return 1;
        }

        // Passo extra vs RECUPERAR BOOT: forca heal de permissoes antes do nuclear.
        if (_cfg is not null)
        {
            try { Unlock(_cfg); } catch { }
            FolderAccess.EnsureWriteAccess(_cfg, interactive: false);
        }

        int code = ErrorRepair.UnbreakBoot(
            _cfg,
            TryRestoreIni,
            () => { if (_cfg is not null && File.Exists(_cfg)) Unlock(_cfg); },
            interactive);
        RefreshWritableCache();
        ModeDetect.Clear();
        Log("CORRIGIR-TUDO concluido code=" + code);
        return code;
    }

    /// <summary>Usado por ErrorRepair.UnbreakBoot sem aceder ao campo privado _cfg.</summary>
    internal static void BackupIniForRepair(string cfgPath)
    {
        try
        {
            if (!File.Exists(cfgPath)) return;
            string ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            File.Copy(cfgPath, Path.Combine(AppMeta.BackupDir, $"TASystemSettings.{ts}.bak"), true);
        }
        catch { }
    }

    /// <summary>Remove marcas Gutty apos boot nuclear (API para ErrorRepair).</summary>
    internal static void StripGuttyMarkersForRepair(string iniPath) => StripGuttyMarkers(iniPath);

    private static int Dispatch(string mode, bool interactive)
    {
        if (mode == "REMOVER") return Remover(interactive);
        if (mode is "CORRIGIR-BOOT" or "RECUPERAR") return CorrigirBoot(interactive);
        if (mode is "CORRIGIR-EAC" or "REPARAR-EAC" or "EAC") return ErrorRepair.RepararEac(interactive);
        if (mode == "CORRIGIR-TUDO") return CorrigirTudo(interactive);
        if (mode is "CORRIGIR-PERFIL" or "REPARAR" or "REPARAR-PERFIL") return CorrigirPerfil(interactive);
        if (mode is "DIAGNOSTICO" or "DIAG") return CorrigirDiagnostico(interactive);
        if (mode is "RESTAURAR-PRESETS" or "RESTAURAR-SAVES") return RestoreLatestSaves(interactive);
        if (mode is "CORRIGIR-SAVE" or "LOAD-FAILURE" or "HEAL-SAVE") return CorrigirSaveLoadFailure(interactive);
        if (mode is "COMPLETO" or "CRIADOR") return Apply(mode, interactive);
        if (mode is "CORRIGIR" or "HEAL") return CorrigirPermissoes(interactive);
        Ui.SectionTitle("ARGUMENTO INVALIDO", Ui.Amber);
        Ui.PanelTop("SINTAXE");
        Ui.PanelLine(Ui.C("GuttyTECH_RL.exe [COMPLETO | CRIADOR | REMOVER | CORRIGIR]", Ui.Gray));
        Ui.PanelLine(Ui.C("[CORRIGIR-PERFIL | CORRIGIR-BOOT | CORRIGIR-EAC | DIAG | RESTAURAR-PRESETS | CORRIGIR-SAVE]", Ui.DimC));
        Ui.PanelBottom();
        return 2;
    }

    internal static string LaunchCommandForGui => LaunchRecommended;

    internal static void EnsureEngineInitializedForGui()
    {
        try { Directory.CreateDirectory(AppMeta.BackupDir); } catch { }
        _cfg = ResolveConfigPath();
        if (_cfg is not null)
        {
            try { _cfg = RlPathResolver.RelocateOffOneDriveIfNeeded(_cfg); } catch { }
            AppMeta.Log($"GUI INI: {_cfg}");
            // Snapshot preventivo: se a garagem ainda esta grande, guarda no Best agora
            try { SaveRecovery.BackupGaragePresets(_cfg); } catch { }

            // A GUI usa Dispatch(interactive:false), portanto o watcher antigo nunca
            // arrancava. O RL regravava o SystemSettings principal (LOD 128, shaders
            // e decals ON) e o COMPLETO ficava extremo apenas nas secoes derivadas.
            string? activeMode = DetectAppliedMode();
            if (activeMode is "COMPLETO" or "CRIADOR")
            {
                if (GetRl().Length == 0)
                {
                    bool healed = VideoSettingsSync.HealIfNeeded(_cfg, activeMode);
                    AppMeta.Log(healed
                        ? $"GUI startup heal completo ({activeMode}) OK."
                        : $"GUI startup heal completo ({activeMode}) parcial.");
                }
                VideoSettingsSync.StartExitWatcher(activeMode);
            }
        }
        RefreshWritableCache();
    }

    internal static Action<int, string, string?>? GuiProgress;

    internal static void ReportGui(int percentage, string message, string? detail = null)
    {
        try
        {
            GuiProgress?.Invoke(Math.Clamp(percentage, 0, 100), message, detail);
        }
        catch { }
    }

    internal static int DispatchForGui(string mode)
    {
        if (_cfg is null)
            EnsureEngineInitializedForGui();
        return Dispatch(mode, interactive: false);
    }

    internal static SupportLogService.PackResult CreateSupportPackForGui()
    {
        if (_cfg is null)
            EnsureEngineInitializedForGui();
        OptimizerStatus status = GetStatusForGui();
        return SupportLogService.CreateSupportPack(_cfg, DetectAppliedMode, status);
    }

    internal static OptimizerStatus GetStatusForGui()
    {
        if (_cfg is null)
            EnsureEngineInitializedForGui();

        string path = _cfg ?? string.Empty;
        bool exists = !string.IsNullOrWhiteSpace(path) && SafeExists(path);
        string? detected = DetectAppliedMode();
        string mode = detected ?? "ORIGINAL";
        string label;

        if (exists)
        {
            (label, _, _) = ReadState();
        }
        else
        {
            label = string.IsNullOrWhiteSpace(path)
                ? "Perfil do Rocket League ainda não localizado"
                : "INI ausente — abra o Rocket League uma vez";
        }

        RefreshWritableCache();
        bool watcher = detected is "COMPLETO" or "CRIADOR"
                       && VideoSettingsSync.IsHealthyWatcherRunning(detected);
        // UI "proteção": modo Gutty ativo (watcher anti-rewrite). Read-only fica
        // off de proposito p/ o menu de video — nao e falha de deteccao.
        bool protectionOn = detected is "COMPLETO" or "CRIADOR";
        return new OptimizerStatus(
            mode,
            label,
            IsCfgWritable(),
            GetRl().Length > 0,
            path,
            protectionOn,
            ElevationService.IsAdministrator(),
            exists,
            watcher);
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
            + Ui.Chip(writable ? "PASTA OK" : "PASTA BLOQUEADA", writable ? Ui.GreenBg : Ui.Red)
            + "  "
            + Ui.Chip(rlOpen ? "JOGO ABERTO" : "JOGO FECHADO", rlOpen ? Ui.AmberBg : Ui.GreenBg);
        Ui.PanelLine("  " + chips);
        Ui.PanelBlank();
        Ui.PanelLine(Ui.Field("Arquivo", Ui.C(FitPath(_cfg!, 58), Ui.Gray)));
        Ui.PanelLine(Ui.Field("Estado", Ui.Dot(modeAccent, label)));
        string trava = locked
            ? Ui.Dot(Ui.Green, "LIGADO (só leitura)")
            : Ui.Dot(Ui.DimC, "DESLIGADO (vídeo livre no jogo)");
        string adm = IsAdmin() ? Ui.Dot(Ui.Green, "SIM") : Ui.Dot(Ui.Gray, "NÃO");
        Ui.PanelLine(Ui.Field("Travar INI", trava + "    " + Ui.C("Admin ", Ui.DimC) + adm));
        Ui.PanelBottom();
        Ui.Gap();

        Ui.PanelTop("MODOS");
        Ui.PanelBlank();
        Ui.MenuCard("1", "COMPLETO", "FPS máximo · visual competitivo · High Performance", Ui.Red, "FPS");
        Ui.MenuCard("2", "CRIADOR DE CONTEÚDO", "Performance forte · visual preservado", Ui.Cyan, "STREAM");
        Ui.MenuCard("3", "REMOVER", "Para watcher + INI stock + limpa cache (preserva presets)", Ui.Amber);
        Ui.MenuCard("4", "COMANDO DE INICIALIZAÇÃO", "Copia o comando compatível com Steam e Epic", Ui.Cyan);
        Ui.MenuCard("5", "CORRIGIR ERROS", "Perfil, boot, permissões e diagnóstico", Ui.Amber);
        Ui.MenuCard("6", "RESTAURAR PRESETS", "Recupera a garagem dos backups Epic/Steam", Ui.Amber);
        Ui.MenuCard("7", "SAIR", "Encerra o RL Optimizer", Ui.DimC);
        Ui.PanelBlank();
        Ui.PanelBottom();
        Ui.FooterHint("COMPLETO e CRIADOR executam limpeza, aplicação e sincronização");
        Ui.Prompt("Selecione um modo  [1–7]");
    }

    private static string FitPath(string p, int max)
        => p.Length <= max ? p : "..." + p[^(max - 3)..];

    // -------------------------------------------------------------- Launch Options
    // Pesquisado/validado no RL (UE3): boot + micro-FPS in-game. EAC-safe para online.
    // Steam e Epic usam o MESMO comando — tela unica, copia no open.
    private const string LaunchRecommended = "-nomovie -NOSPLASH -nomansky +mat_antialias 0 -high";
    private const string LaunchNoPriority = "-nomovie -NOSPLASH -nomansky +mat_antialias 0";

    private static void LaunchOptions()
    {
        Ui.HideCursor();
        Ui.Cls();
        Ui.MiniBannerIfTall(Ui.MCyan);
        Ui.TitleBar("COMANDO DE INICIALIZACAO", Ui.MCyan);

        Ui.Gap();
        Ui.LaunchHeading("COPIAR E COLAR (Steam = Epic)");
        Ui.CopyStatus(ClipboardUtil.TryCopy(LaunchRecommended));
        Ui.CodeBox(LaunchRecommended);

        Ui.StepsPanel("STEAM", new[]
        {
            "Steam > botao direito no Rocket League > Propriedades",
            "Geral > Opcoes de Inicializacao",
            "Cole (Ctrl+V) e feche — NAO use %command%"
        }, Ui.MCyan);

        Ui.StepsPanel("EPIC GAMES", new[]
        {
            "Epic > Biblioteca > ... no Rocket League > Gerenciar",
            "Marque 'Argumentos de linha de comando adicionais'",
            "Cole (Ctrl+V) o comando e salve"
        }, Ui.MCyan);

        Ui.LaunchHeading("O que cada flag faz");
        Ui.LaunchParam("+", Ui.OkGreen, "-nomovie", "pula intros / cutscenes");
        Ui.LaunchParam("+", Ui.OkGreen, "-NOSPLASH", "remove splash (logo)");
        Ui.LaunchParam("+", Ui.OkGreen, "-nomansky", "ceu mais leve (menos GPU)");
        Ui.LaunchParam("+", Ui.OkGreen, "+mat_antialias 0", "anti-aliasing off = mais FPS");
        Ui.LaunchParam("+", Ui.OkGreen, "-high", "prioridade alta (tire se engasgar)");
        Ui.LaunchNote("Sem -high: " + LaunchNoPriority);

        Ui.EnterButton();
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
        ReportGui(18, "Backup do perfil", "Preservando INI e presets da garagem");
        // Presets/garagem (saves grandes) — antes de qualquer patch de video.
        try { SaveRecovery.BackupGaragePresets(_cfg); } catch { }

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
            ReportGui(24, "Destravando arquivo", "Removendo somente-leitura se existir");
            Backup();
            Unlock(_cfg!);
            ReportGui(32, "Limpando modo anterior", "Reset limpo antes de gravar " + mode);
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
            Ui.StepAnimated("Validando boot-safe", () => ErrorRepair.ForceBootSafeIni(_cfg!));
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
            ErrorRepair.ForceBootSafeIni(_cfg!);
            if (mode is "CRIADOR" or "COMPLETO")
                Ui.StepAnimated("Mantendo video ajustavel no jogo", UnlockCfg);
            Ui.FlushInput();
        }
        else
        {
            ReportGui(42, "Gravando otimização", mode + " → TASystemSettings.ini");
            if (!DoWrite(content, mode)) return FailOrElevate(mode, interactive);
            ReportGui(48, "Validando boot-safe", "Checando chaves que travam o jogo");
            ErrorRepair.ForceBootSafeIni(_cfg!);
            ReportGui(52, "Sincronizando menu de vídeo", "Patch nas contas — esta etapa demora mais");
            int lastSyncPct = 52;
            Action<int, int, string> guiBar = (cur, tot, detail) =>
            {
                int safeTot = Math.Max(1, tot);
                int pct = 52 + (int)Math.Round(34.0 * Math.Clamp(cur, 0, safeTot) / safeTot);
                if (pct < lastSyncPct) pct = lastSyncPct;
                else lastSyncPct = pct;
                string label = string.IsNullOrWhiteSpace(detail)
                    ? $"Conta {cur}/{safeTot}"
                    : $"{detail} ({cur}/{safeTot})";
                ReportGui(Math.Min(pct, 86), "Sincronizando menu de vídeo", label);
            };
            if (!VideoSettingsSync.SyncVideoSave(_cfg!, mode, interactive, guiBar)) return 1;
            ReportGui(90, "Reforçando presets", "Garantindo que a garagem não sumiu");
            ErrorRepair.ForceBootSafeIni(_cfg!);
            if (mode is "CRIADOR" or "COMPLETO") UnlockCfg();
            ReportGui(96, "Finalizando", "INI liberado pra o menu do jogo");
        }

        string msg = previous is null ? $"Aplicado {mode} (limpo)."
            : previous.Equals(mode, StringComparison.OrdinalIgnoreCase)
                ? $"Reaplicado {mode} (limpo + sync contas)."
                : $"Trocou {previous} → {mode} (limpo + sync contas).";
        ModeDetect.Persist(mode);
        Log(msg);
        RefreshWritableCache();
        // Watcher sempre — GUI e CLI. Sem isto PROTEÇÃO fica OFF apos Apply nao-interativo.
        VideoSettingsSync.StartExitWatcher(mode);
        if (interactive)
        {
            Ui.CompletionSuccess(mode, acc, AppMeta.BackupDir);
            Ui.FooterHint("MONITOR ATIVO  ·  reparo automático ao fechar o Rocket League");
        }
        return 0;
    }

    private static string? DetectAppliedMode() => ModeDetect.Detect(_cfg);

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
        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("REMOVER OTIMIZACAO", Ui.MAmber);
            Ui.StepsPanel("O QUE VAI SER FEITO", new[]
            {
                "Para o watcher automatico (impede o modo de voltar sozinho)",
                "Fecha o Rocket League se estiver aberto",
                "Preserva presets/garagem no cofre Best",
                "Restaura INI stock/original + boot-safe",
                "Limpa cache Epic (RLSettingsData) e runtime do watcher",
                "Presets e backups Gutty ficam — so o otimizador ativo sai",
            }, Ui.MAmber);
        }

        if (!EnsureConfigDir())
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta Config do RL nao existe.", "Abra o jogo 1x pela Epic/Steam." });
            return 1;
        }

        var report = new List<string>();

        // 1) Watcher PRIMEIRO — senao HealIfNeeded regrava COMPLETO apos o restore.
        if (interactive)
            Ui.StepAnimated("Parando watcher automatico", () => { VideoSettingsSync.CleanWatcherRuntime(); return true; });
        else
            VideoSettingsSync.CleanWatcherRuntime();
        report.Add("watcher parado");

        // 2) Fechar RL
        bool killed;
        if (interactive)
        {
            if (GetRl().Length > 0)
            {
                Ui.Gap();
                Ui.PanelTop("ROCKET LEAGUE ABERTO");
                Ui.PanelLine(Ui.C("O jogo sobrescreve o INI ao fechar — precisa sair agora.", Ui.Amber));
                Ui.PanelBottom();
                Ui.Prompt("Fechar o jogo agora? (S/N)");
                if (!IsYes(Console.ReadLine()))
                    return 1;
            }
            killed = Ui.StepAnimated("Fechando Rocket League", () =>
            {
                ErrorRepair.ForceCloseRocketLeague();
                return GetRl().Length == 0;
            });
            if (!killed && GetRl().Length > 0)
            {
                Ui.CompletionMessage(Ui.MRed, "ACAO BLOQUEADA", new[] { "Nao consegui fechar o Rocket League.", "Feche-o manualmente e tente de novo." });
                return 1;
            }
        }
        else
        {
            ErrorRepair.ForceCloseRocketLeague();
            if (GetRl().Length > 0)
            {
                Log("REMOVER: RL ainda aberto apos kill.");
                return 1;
            }
            killed = true;
        }
        report.Add(killed ? "RL fechado" : "RL ja fechado");

        // 3) Permissoes — passar sempre o caminho do INI (mesmo se ainda nao existir).
        if (!FolderAccess.EnsureWriteAccess(_cfg!, interactive))
            return 1;

        // 4) Snapshot garagem
        int snapped = 0;
        if (interactive)
            Ui.StepAnimated("Preservando presets/garagem", () =>
            {
                snapped = SaveRecovery.BackupGaragePresets(_cfg);
                return true;
            });
        else
            snapped = SaveRecovery.BackupGaragePresets(_cfg);
        if (snapped > 0) report.Add($"presets={snapped}");

        // 5) Unlock + backup + stock INI
        bool iniOk;
        if (interactive)
        {
            Ui.StepAnimated("Destravando TASystemSettings.ini", () =>
            {
                if (File.Exists(_cfg!)) Unlock(_cfg!);
                return true;
            });
            Ui.StepAnimated("Backup de seguranca do INI", () =>
            {
                if (File.Exists(_cfg!)) Backup();
                return true;
            });
            iniOk = Ui.StepAnimated("Restaurando INI stock (sem otimizador)", TryRestoreIni);
            Ui.StepAnimated("Removendo marcas Gutty do INI", () => StripGuttyMarkers(_cfg!));
            Ui.StepAnimated("Boot-safe final", () => ErrorRepair.ForceBootSafeIni(_cfg!));
        }
        else
        {
            if (File.Exists(_cfg!)) { Unlock(_cfg!); Backup(); }
            iniOk = TryRestoreIni();
            StripGuttyMarkers(_cfg!);
            ErrorRepair.ForceBootSafeIni(_cfg!);
        }
        report.Add(iniOk ? "INI stock OK" : "INI FALHOU");

        // 6) Cache Epic — evita menu/FPS otimizado a lutar com INI stock
        bool purgeOk;
        if (interactive)
            purgeOk = Ui.StepAnimated("Limpando cache Epic (RLSettingsData)", SaveRecovery.PurgeRlSettingsData);
        else
            purgeOk = SaveRecovery.PurgeRlSettingsData();
        report.Add(purgeOk ? "cache limpo" : "cache parcial");

        // 7) Garantir que o watcher nao voltou e modo detetado sumiu
        VideoSettingsSync.CleanWatcherRuntime();
        ModeDetect.Clear();
        string? left = DetectAppliedMode();
        if (left is not null)
        {
            // OrigBackup poluido / fingerprint residual → forcar Templates.Stock limpo.
            try
            {
                Unlock(_cfg!);
                var disp = File.Exists(_cfg!) ? ReadDisplay(_cfg!) : DefaultDisplay();
                string stock = ApplyDisplay(Templates.Stock, disp);
                File.WriteAllText(_cfg!, stock, new UTF8Encoding(false));
                StripGuttyMarkers(_cfg!);
                ErrorRepair.ForceBootSafeIni(_cfg!);
            }
            catch (Exception ex)
            {
                Log("REMOVER force-stock: " + ex.Message);
            }
            ModeDetect.Clear();
            left = DetectAppliedMode();
        }
        bool clean = left is null && iniOk;
        report.Add(clean ? "modo Gutty ausente" : "modo residual=" + (left ?? "?"));

        RefreshWritableCache();
        Log("REMOVER: " + string.Join("; ", report));

        if (interactive)
        {
            Ui.CompletionMessage(clean ? Ui.OkGreen : Ui.MAmber, clean ? "OTIMIZACAO REMOVIDA" : "REMOCAO PARCIAL", new[]
            {
                string.Join(" · ", report),
                "Presets/garagem: intactos (cofre Best).",
                "Se colou flags em Steam/Epic, remova-as manualmente.",
                "Abra o RL 1x — o menu pode pedir APLICAR video (normal).",
                "Para reaplicar depois: COMPLETO ou CRIADOR.",
            });
        }

        return clean ? 0 : 1;
    }

    /// <summary>Remove GuttyTechMode / comentarios Gutty se sobrarem apos restore.</summary>
    private static bool StripGuttyMarkers(string iniPath)
    {
        try
        {
            if (!File.Exists(iniPath)) return true;
            string text = File.ReadAllText(iniPath);
            var sb = new StringBuilder();
            bool changed = false;
            foreach (string raw in text.Replace("\r\n", "\n").Split('\n'))
            {
                string t = raw.TrimStart();
                if (t.StartsWith("GuttyTechMode=", StringComparison.OrdinalIgnoreCase)
                    || t.StartsWith("GUTTYTECH-RL-OPTIMIZER=", StringComparison.OrdinalIgnoreCase)
                    || t.Contains("GUTTYTECH-RL-OPTIMIZER", StringComparison.OrdinalIgnoreCase)
                        && t.StartsWith(';'))
                {
                    changed = true;
                    continue;
                }
                sb.Append(raw).Append("\r\n");
            }
            if (changed)
            {
                try { File.SetAttributes(iniPath, FileAttributes.Normal); } catch { }
                File.WriteAllText(iniPath, sb.ToString(), new UTF8Encoding(false));
                ErrorRepair.ForceBootSafeIni(iniPath);
                Log("Marcas Gutty removidas do INI.");
            }
            return DetectAppliedMode() is null;
        }
        catch (Exception ex)
        {
            Log("StripGuttyMarkers: " + ex.Message);
            return false;
        }
    }

    private static bool TryRestoreIni()
    {
        try
        {
            bool fromOriginal = File.Exists(AppMeta.OrigBackup) && IsSafeOriginalBackup(AppMeta.OrigBackup);
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
            // Sempre neutralizar boot-killers mesmo se OrigBackup estiver limpo na maior parte.
            ErrorRepair.ForceBootSafeIni(_cfg!);
            return true;
        }
        catch { return false; }
    }

    /// <summary>OrigBackup so conta se nao tiver modo Gutty, boot-killers nem fingerprint Completo.</summary>
    private static bool IsSafeOriginalBackup(string path)
    {
        try
        {
            string text = File.ReadAllText(path);
            if (text.Contains("GuttyTechMode=", StringComparison.OrdinalIgnoreCase)) return false;
            if (text.Contains("GUTTYTECH-RL-OPTIMIZER=", StringComparison.OrdinalIgnoreCase)) return false;
            if (ErrorRepair.HasBootKillers(text)) return false;
            // Backup feito apos Completo sem marcador — nao reintroduzir potato.
            if (text.Contains("MaxShadowResolution=1", StringComparison.OrdinalIgnoreCase)
                && text.Contains("DynamicShadows=False", StringComparison.OrdinalIgnoreCase)
                && text.Contains("MaxLODSize=2", StringComparison.OrdinalIgnoreCase))
                return false;
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

        try { cfg = RlPathResolver.RelocateOffOneDriveIfNeeded(cfg); _cfg = cfg; } catch { }

        string? saveDir = SaveRecovery.SaveDirFromIni(cfg);
        if (saveDir is null)
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta SaveDataEpic nao encontrada." });
            return 1;
        }

        var (bakFiles, bakGarage, bakBytes) = SaveRecovery.CountBackups();

        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("RESTAURAR PRESETS", Ui.MAmber);
            Ui.StepsPanel("GARAGEM / PRESETS DO CARRO", new[]
            {
                "Prioriza o maior save de cada conta (cofre Best sticky)",
                "Procura em Best + Backups + Presets + Quarentena",
                "Reforca contas live pequeninas e faz 2o passe anti-cloud",
                "Abra o RL OFFLINE depois — cloud Epic pode regravar online",
            }, Ui.MAmber);
            Ui.Gap();
            Ui.PanelTop("BACKUPS DISPONIVEIS");
            Ui.PanelLine(Ui.C($"Ficheiros: {bakFiles}  |  Garagem(>=250KB): {bakGarage}  |  {bakBytes / 1024} KB", Ui.Gray));
            Ui.PanelBottom();
            if (bakFiles == 0)
            {
                Ui.CompletionMessage(Ui.MRed, "SEM BACKUP", new[]
                {
                    "Nao ha saves em GuttyTECH\\RL-Optimizer-v22\\Backups",
                    "Sem backup local nao da para recuperar presets.",
                    "Epic Launcher -> Biblioteca -> RL -> Verificar ficheiros.",
                });
                return 1;
            }
            Ui.Gap();
            Ui.Prompt("Restaurar agora? (S/N)");
            if (!IsYes(Console.ReadLine())) return 1;
        }
        else if (bakFiles == 0)
        {
            return 1;
        }

        // Fecha RL — senao o jogo/cloud regrava por cima
        foreach (var p in GetRl()) { try { p.Kill(); } catch { } }
        Thread.Sleep(1500);

        string summary = "";
        bool ok;
        if (interactive)
        {
            ok = Ui.StepAnimated("Snapshot + restaurar garagem (Epic/Steam)", () =>
                SaveRecovery.RestorePresets(cfg, out summary));
            Ui.StepAnimated("A confirmar pasta", () => Directory.Exists(saveDir));
        }
        else
        {
            ok = SaveRecovery.RestorePresets(cfg, out summary);
        }

        if (interactive)
        {
            Ui.CompletionMessage(ok ? Ui.OkGreen : Ui.MRed, ok ? "PRESETS RESTAURADOS" : "FALHOU", ok
                ? new[]
                {
                    summary,
                    "Destino: " + FitPath(saveDir, 48),
                    "1) Abre o RL OFFLINE e confirma a garagem",
                    "2) No Epic: pausa Cloud Saves do Rocket League",
                    "3) Se o Windows ainda apontava Documentos ao OneDrive, ja copiei para local",
                }
                : new[]
                {
                    "Nenhum save de garagem recuperavel nos backups.",
                    "Pasta: GuttyTECH\\RL-Optimizer-v22\\Backups\\Presets\\Best",
                    "Epic -> Verificar ficheiros do Rocket League.",
                });
        }
        Log(ok ? $"RESTAURAR-PRESETS OK: {summary}" : "RESTAURAR-PRESETS: falhou.");
        return ok ? 0 : 1;
    }

    private static int CorrigirSaveLoadFailure(bool interactive)
    {
        if (!CheckGame(interactive)) return 1;
        string? cfg = _cfg ?? ResolveConfigPath();
        if (cfg is null)
        {
            if (interactive)
                Ui.CompletionMessage(Ui.MRed, "ERRO", new[] { "Pasta Config do RL nao encontrada." });
            return 1;
        }

        if (interactive)
        {
            Ui.Cls();
            Ui.MiniBannerIfTall(Ui.MAmber);
            Ui.TitleBar("CORRIGIR SAVE — LOAD FAILURE", Ui.MAmber);
            Ui.StepsPanel("STEAM / SAVE LOCAL", new[]
            {
                "Fecha Rocket League + Steam (precisa editar Cloud)",
                "Limpa SaveData\\DBE_Production (NAO reinsere Best — era o bug)",
                "Quarentena Steam Cloud remote + CloudEnabled=0 no localconfig",
                "Depois: NEW SAVE no aviso (recomendado) — tutorial as vezes e normal",
                "So depois: RESTAURAR PRESETS no Gutty",
            }, Ui.MAmber);
            Ui.Gap();
            foreach (string line in SaveRecovery.AssessSaveHealth(cfg))
                Ui.PanelLine(Ui.C(line, line.Contains("!!") ? Ui.Amber : Ui.Gray));
            Ui.Gap();
            Ui.Prompt("Corrigir agora? (S/N)");
            if (!IsYes(Console.ReadLine())) return 1;
        }

        foreach (var p in GetRl()) { try { p.Kill(); } catch { } }
        Thread.Sleep(1200);
        VideoSettingsSync.StopExistingWatchers();

        string summary = "";
        bool ok;
        if (interactive)
            ok = Ui.StepAnimated("Heal LOAD FAILURE (Steam+local)", () => SaveRecovery.HealLoadFailure(cfg, out summary));
        else
            ok = SaveRecovery.HealLoadFailure(cfg, out summary);

        if (interactive)
        {
            Ui.CompletionMessage(ok ? Ui.OkGreen : Ui.MAmber, ok ? "SAVE LIMPO — PROXIMO PASSO" : "POUCO A FAZER", new[]
            {
                summary,
                "1) Abre a Steam (Cloud do RL ja deve estar OFF)",
                "2) Abre o RL — se LOAD FAILURE: NEW SAVE (nao fiques no RETRY)",
                "3) Tutorial as vezes aparece — normal; rank/itens sao online",
                "4) Fecha > RESTAURAR PRESETS > abre OFFLINE > so depois Cloud ON",
                "5) Guia: Desktop\\GuttyTECH-RL-LOAD-FAILURE.txt",
            });
        }

        Log(ok ? $"CORRIGIR-SAVE OK: {summary}" : "CORRIGIR-SAVE: sem mudancas relevantes");
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
        string? found = RlPathResolver.ResolveIni();
        if (!string.IsNullOrWhiteSpace(found))
            return found;
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
        string label = "Original (não otimizado)";
        int cat = 0;
        string? mode = DetectAppliedMode();
        if (mode == "COMPLETO")
        { label = "FPS máximo ativo"; cat = 2; }
        else if (mode == "CRIADOR")
        { label = "Visual + perf ativo"; cat = 2; }
        else
        {
            try
            {
                string text = File.ReadAllText(_cfg!);
                if (text.Contains("MaxLODSize=16")) { label = "Otimizado (versão antiga)"; cat = 1; }
            }
            catch { }
        }
        bool locked = false;
        try { locked = (File.GetAttributes(_cfg!) & FileAttributes.ReadOnly) != 0; } catch { }
        return (label, locked, cat);
    }

    private static bool IsAdmin() => ElevationService.IsAdministrator();

    // -------------------------------------------------------------- Helpers
    private static bool CheckGame(bool interactive)
    {
        if (Environment.GetEnvironmentVariable("GUTTYRL_SKIP_GAMECHECK") == "1") return true;
        if (GetRl().Length == 0) return true;
        Ui.SectionTitle("ROCKET LEAGUE ABERTO", Ui.Amber);
        Ui.PanelTop("AÇÃO NECESSÁRIA");
        Ui.PanelLine(Ui.C("O jogo sobrescreve o arquivo ao fechar.", Ui.Gray));
        Ui.PanelLine(Ui.C("Feche-o antes de aplicar qualquer modo.", Ui.DimC));
        Ui.PanelBottom();
        if (!interactive) { Console.WriteLine(Ui.C("  Feche o jogo e rode de novo.", Ui.Red)); return false; }
        Ui.Prompt("Fechar o jogo agora? (S/N)");
        if (!IsYes(Console.ReadLine())) return false;
        foreach (var p in GetRl()) { try { p.Kill(); } catch { } }
        for (int i = 0; i < 4; i++)
        {
            Thread.Sleep(500);
            if (GetRl().Length == 0) return true;
        }
        Ui.CompletionMessage(Ui.Red, "AÇÃO BLOQUEADA", new[] { "Não consegui fechar o jogo.", "Feche-o manualmente e tente novamente." });
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
            if (text.Contains("GUTTYTECH-RL-OPTIMIZER=", StringComparison.OrdinalIgnoreCase)
                || text.Contains("GuttyTechMode=", StringComparison.OrdinalIgnoreCase)
                || text.Contains("MaxLODSize=16")
                || ErrorRepair.HasBootKillers(text))
            {
                Log("Arquivo atual ja otimizado/poluido; original nao capturado - usar stock no REMOVER.");
                return;
            }
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
        Console.WriteLine(new string(' ', Ui.Margin) + Ui.B("GUTTYTECH  /  TESSERACT", Ui.Red)
            + Ui.C("    SESSÃO ENCERRADA", Ui.DimC));
        Ui.Gap();
    }

    private static void Log(string msg) => AppMeta.Log(msg);

    private static bool IsYes(string? s) => string.Equals(s?.Trim(), "S", StringComparison.OrdinalIgnoreCase);
}
