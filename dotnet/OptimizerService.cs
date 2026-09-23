namespace GuttyRL;

/// <summary>
/// Ponte thread-safe entre a experiência WPF e o motor legado.
/// O motor continua sendo a única fonte de verdade para aplicação e reparo.
/// </summary>
internal sealed class OptimizerService
{
    private readonly SemaphoreSlim _operationGate = new(1, 1);

    private OptimizerService()
    {
    }

    public static OptimizerService Instance { get; } = new();

    public string LaunchCommand => Program.LaunchCommandForGui;

    public Task<OptimizerStatus> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.Run(Program.GetStatusForGui, cancellationToken);

    public async Task<OperationResult> ExecuteAsync(
        OptimizerAction action,
        IProgress<OperationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            progress?.Report(new OperationProgress(8, "prog.safe"));
            return await Task.Run(
                () => ExecuteCore(action, progress, cancellationToken),
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return new OperationResult(
                false,
                true,
                FeedbackTone.Warning,
                "res.cancelledTitle",
                "res.cancelledBody");
        }
        catch (Exception ex)
        {
            AppMeta.Log($"GUI {action}: {ex.GetType().Name}: {ex.Message}");
            return new OperationResult(
                false,
                false,
                FeedbackTone.Error,
                "res.crashTitle",
                "res.crashBody",
                AppMeta.LogFile);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private OperationResult ExecuteCore(
        OptimizerAction action,
        IProgress<OperationProgress>? progress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (action == OptimizerAction.CopiarComando)
        {
            progress?.Report(new OperationProgress(40, "prog.cmdPrep"));
            string cmd = LaunchCommand.Trim();
            progress?.Report(new OperationProgress(65, "prog.cmdWrite"));
            string? desktopFile = TryWriteLaunchCommandFile(cmd);
            bool copied = ClipboardUtil.TryCopy(cmd);
            // App elevado → clipboard do admin != clipboard do user (Steam). De-eleva via explorer.
            if (!copied || ElevationService.IsAdministrator())
                copied = ClipboardUtil.TryCopyUnelevated(cmd) || copied;

            progress?.Report(new OperationProgress(100, copied ? "prog.cmdReady" : "prog.cmdDesktop"));
            if (copied)
            {
                return desktopFile is not null
                    ? Success("res.copyTitle", "res.copyBodyFile", cmd, desktopFile)
                    : Success("res.copyTitle", "res.copyBody", cmd);
            }

            return desktopFile is not null
                ? Success("res.copyDesktopTitle", "res.copyDesktopBody", desktopFile, cmd)
                : Failure("res.copyFailTitle", "res.copyFailBody", cmd);
        }

        if (action == OptimizerAction.Diagnostico)
        {
            progress?.Report(new OperationProgress(20, "prog.diagRead"));
            Program.EnsureEngineInitializedForGui();
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new OperationProgress(55, "prog.diagPack"));
            SupportLogService.PackResult pack = Program.CreateSupportPackForGui();
            progress?.Report(new OperationProgress(100, pack.Success ? "prog.diagReady" : "prog.diagFail"));
            return BuildSupportPackResult(pack);
        }

        Program.EnsureEngineInitializedForGui();
        OptimizerStatus before = Program.GetStatusForGui();

        OperationResult? blocked = ValidatePreconditions(action, before);
        if (blocked is not null)
            return blocked;

        if (before.IsRocketLeagueOpen)
        {
            progress?.Report(new OperationProgress(8, "prog.closing", "prog.closingDetail"));
            ErrorRepair.ForceCloseRocketLeague();
            if (System.Diagnostics.Process.GetProcessesByName("RocketLeague").Length > 0)
            {
                return NoOp("res.closeFailTitle", "res.closeFailBody");
            }
        }

        progress?.Report(new OperationProgress(12, GetPreparationMessage(action), "prog.prepModeDetail"));
        cancellationToken.ThrowIfCancellationRequested();

        void Sink(int pct, string message, string? detail) =>
            progress?.Report(new OperationProgress(pct, message, detail));

        Program.GuiProgress = Sink;
        int exitCode;
        try
        {
            exitCode = Program.DispatchForGui(ToEngineMode(action));
        }
        finally
        {
            Program.GuiProgress = null;
        }

        progress?.Report(new OperationProgress(98, "prog.validate", "prog.validateDetail"));

        if (exitCode == 0)
        {
            string? watcherMode = action switch
            {
                OptimizerAction.Completo => "COMPLETO",
                OptimizerAction.Criador => "CRIADOR",
                OptimizerAction.RepararPerfil => null, // resolve apos status
                _ => null,
            };
            // Arranca watcher ANTES do status final — senao o card PROTEÇÃO fica OFF.
            if (watcherMode is not null)
                VideoSettingsSync.StartExitWatcher(watcherMode);
        }

        OptimizerStatus after = Program.GetStatusForGui();

        if (exitCode == 0
            && action == OptimizerAction.RepararPerfil
            && after.AppliedMode is "COMPLETO" or "CRIADOR")
        {
            VideoSettingsSync.StartExitWatcher(after.AppliedMode);
            after = Program.GetStatusForGui();
        }

        // Re-lê apos watcher COMPLETO/CRIADOR para o card refletir ATIVA.
        if (exitCode == 0
            && action is OptimizerAction.Completo or OptimizerAction.Criador)
        {
            after = Program.GetStatusForGui();
        }

        progress?.Report(new OperationProgress(100, exitCode == 0 ? "prog.done" : "prog.attention", null));

        // CORRIGIR-EAC: exit 2 = precisa reiniciar PC (serviço 1072) — nao e falha do otimizador.
        if (action == OptimizerAction.RepararEac && exitCode == 2)
        {
            return new OperationResult(
                true,
                false,
                FeedbackTone.Warning,
                "res.rebootTitle",
                "res.rebootBody");
        }

        if (exitCode != 0)
            return BuildFailure(action, after);

        return BuildSuccess(action, before, after);
    }

    private static string? TryWriteLaunchCommandFile(string cmd)
    {
        try
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
                desktop = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string path = Path.Combine(desktop, "GuttyTECH-RL-LaunchCommand.txt");
            File.WriteAllText(
                path,
                cmd + Environment.NewLine + Environment.NewLine
                + UiCopy.Get("file.steam") + Environment.NewLine
                + UiCopy.Get("file.epic") + Environment.NewLine,
                new System.Text.UTF8Encoding(false));
            return path;
        }
        catch (Exception ex)
        {
            AppMeta.Log("LaunchCommand file: " + ex.Message);
            return null;
        }
    }

    private static OperationResult? ValidatePreconditions(OptimizerAction action, OptimizerStatus status)
    {
        if (string.IsNullOrWhiteSpace(status.ConfigPath)
            && action != OptimizerAction.Diagnostico)
        {
            return NoOp("res.noProfileTitle", "res.noProfileBody");
        }

        // Recuperar Boot / Corrigir Tudo fecham o RL automaticamente no ExecuteCore.
        // Reparar perfil exige modo ativo — sem modo, o caminho certo e o boot nuclear.
        if (action == OptimizerAction.RepararPerfil
            && status.AppliedMode is not ("COMPLETO" or "CRIADOR"))
        {
            return NoOp("res.noModeTitle", "res.noModeBody");
        }

        return null;
    }

    private static OperationResult BuildSuccess(
        OptimizerAction action,
        OptimizerStatus before,
        OptimizerStatus after) =>
        action switch
        {
            OptimizerAction.Completo => Success(
                "res.okCompletoTitle",
                before.AppliedMode == "COMPLETO" ? "res.okCompletoAgain" : "res.okCompletoFresh"),
            OptimizerAction.Criador => Success(
                "res.okCriadorTitle",
                before.AppliedMode == "CRIADOR" ? "res.okCriadorAgain" : "res.okCriadorFresh"),
            OptimizerAction.Remover => Success("res.okRemoveTitle", "res.okRemoveBody"),
            OptimizerAction.CorrigirPermissoes => Success(
                "res.okPermTitle",
                after.IsWritable ? "res.okPermYes" : "res.okPermMaybe"),
            OptimizerAction.RepararPerfil => Success("res.okProfileTitle", "res.okProfileBody"),
            OptimizerAction.RecuperarBoot => Success("res.okBootTitle", "res.okBootBody"),
            OptimizerAction.RepararEac => Success("res.okEacTitle", "res.okEacBody"),
            OptimizerAction.CorrigirTudo => Success("res.okAllTitle", "res.okAllBody"),
            OptimizerAction.RestaurarPresets => Success("res.okPresetsTitle", "res.okPresetsBody"),
            OptimizerAction.CorrigirSave => Success("res.okSaveTitle", "res.okSaveBody"),
            _ => Success("res.okGenericTitle", "res.okGenericBody"),
        };

    private static OperationResult BuildFailure(OptimizerAction action, OptimizerStatus status)
    {
        if (status.IsRocketLeagueOpen)
            return Failure("res.blockedTitle", "res.blockedBody");

        if (!status.IsWritable)
            return Failure("res.lockedTitle", "res.lockedBody");

        if (action is OptimizerAction.RecuperarBoot
            or OptimizerAction.RepararEac
            or OptimizerAction.CorrigirTudo)
        {
            GameInstallProbe.Report install = GameInstallProbe.ScanLive();
            if (install.Verdict != GameInstallProbe.InstallVerdict.Ok)
            {
                return Failure(
                    install.Verdict == GameInstallProbe.InstallVerdict.Missing
                        ? "res.gameMissingTitle"
                        : "res.gameBadTitle",
                    install.Verdict == GameInstallProbe.InstallVerdict.Missing
                        ? "res.installMissing"
                        : "res.installIncomplete");
            }
        }

        return action switch
        {
            OptimizerAction.RepararPerfil => Failure("res.profilePartialTitle", "res.profilePartialBody"),
            OptimizerAction.RecuperarBoot => Failure("res.bootPartialTitle", "res.bootPartialBody"),
            OptimizerAction.RepararEac => Failure("res.eacFailTitle", "res.eacFailBody"),
            OptimizerAction.RestaurarPresets => Failure("res.noBackupTitle", "res.noBackupBody"),
            OptimizerAction.CorrigirSave => Failure("res.saveRiskTitle", "res.saveRiskBody"),
            _ => Failure("res.failTitle", "res.failBody"),
        };
    }

    private static OperationResult BuildSupportPackResult(SupportLogService.PackResult pack)
    {
        if (!pack.Success)
            return Failure("res.packFailTitle", "raw.pass", pack.Summary);

        if (pack.HasIssues)
        {
            return new OperationResult(
                true,
                false,
                FeedbackTone.Warning,
                "res.packWarnTitle",
                "raw.pass",
                pack.Summary);
        }

        return Success("res.packOkTitle", "raw.pass", pack.Summary);
    }

    private static string ToEngineMode(OptimizerAction action) =>
        action switch
        {
            OptimizerAction.Completo => "COMPLETO",
            OptimizerAction.Criador => "CRIADOR",
            OptimizerAction.Remover => "REMOVER",
            OptimizerAction.CorrigirPermissoes => "CORRIGIR",
            OptimizerAction.RepararPerfil => "CORRIGIR-PERFIL",
            OptimizerAction.RecuperarBoot => "CORRIGIR-BOOT",
            OptimizerAction.RepararEac => "CORRIGIR-EAC",
            OptimizerAction.CorrigirTudo => "CORRIGIR-TUDO",
            OptimizerAction.RestaurarPresets => "RESTAURAR-PRESETS",
            OptimizerAction.CorrigirSave => "CORRIGIR-SAVE",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    private static string GetPreparationMessage(OptimizerAction action) =>
        action switch
        {
            OptimizerAction.Completo or OptimizerAction.Criador => "prog.prepMode",
            OptimizerAction.Remover => "prog.prepRemove",
            OptimizerAction.RestaurarPresets => "prog.prepPresets",
            OptimizerAction.CorrigirSave => "prog.prepSave",
            _ => "prog.prepGeneric",
        };

    private static string GetExecutionMessage(OptimizerAction action) =>
        action switch
        {
            OptimizerAction.Completo => "Aplicando perfil de FPS máximo",
            OptimizerAction.Criador => "Aplicando perfil visual de criação",
            OptimizerAction.Remover => "Parando watcher e restaurando INI stock",
            OptimizerAction.CorrigirPermissoes => "Reparando acesso à pasta do jogo",
            OptimizerAction.RepararPerfil => "Sincronizando INI, menu e cache",
            OptimizerAction.RecuperarBoot => "Desbloqueando boot (stock + EAC)",
            OptimizerAction.RepararEac => "Reinstalando serviço EasyAntiCheat_EOS",
            OptimizerAction.CorrigirTudo => "Executando desbloqueio nuclear do boot",
            OptimizerAction.RestaurarPresets => "Restaurando garagem Epic e Steam",
            OptimizerAction.CorrigirSave => "Quarentena Steam Cloud + saves partidos",
            _ => "Executando operação",
        };

    private static OperationResult Success(string title, string message, string arg0 = "", string arg1 = "") =>
        new(true, false, FeedbackTone.Success, title, message, arg0, arg1);

    private static OperationResult Failure(string title, string message, string arg0 = "") =>
        new(false, false, FeedbackTone.Error, title, message, arg0);

    private static OperationResult NoOp(string title, string message) =>
        new(false, true, FeedbackTone.Warning, title, message);
}
