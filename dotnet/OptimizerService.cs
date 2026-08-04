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
            progress?.Report(new OperationProgress(8, "Preparando ambiente seguro"));
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
                "OPERAÇÃO CANCELADA",
                "Nenhuma nova operação foi iniciada.");
        }
        catch (Exception ex)
        {
            AppMeta.Log($"GUI {action}: {ex.GetType().Name}: {ex.Message}");
            return new OperationResult(
                false,
                false,
                FeedbackTone.Error,
                "FALHA INESPERADA",
                "O motor interrompeu a operação com segurança. Consulte o log em " + AppMeta.LogFile);
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
            progress?.Report(new OperationProgress(55, "Copiando comando unificado"));
            bool copied = ClipboardUtil.TryCopy(LaunchCommand);
            progress?.Report(new OperationProgress(100, copied ? "Comando pronto" : "Clipboard indisponível"));
            return copied
                ? Success("COMANDO COPIADO", "Compatível com Steam e Epic Games. Basta colar nas opções de inicialização.")
                : Failure("NÃO FOI POSSÍVEL COPIAR", "Selecione o comando exibido e copie manualmente com Ctrl+C.");
        }

        if (action == OptimizerAction.Diagnostico)
        {
            progress?.Report(new OperationProgress(20, "Lendo perfil e logs do otimizador"));
            Program.EnsureEngineInitializedForGui();
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new OperationProgress(55, "Empacotando diagnóstico e INI"));
            SupportLogService.PackResult pack = Program.CreateSupportPackForGui();
            progress?.Report(new OperationProgress(100, pack.Success ? "Pacote pronto no Desktop" : "Falha ao gerar pacote"));
            return BuildSupportPackResult(pack);
        }

        Program.EnsureEngineInitializedForGui();
        OptimizerStatus before = Program.GetStatusForGui();

        OperationResult? blocked = ValidatePreconditions(action, before);
        if (blocked is not null)
            return blocked;

        if (before.IsRocketLeagueOpen)
        {
            progress?.Report(new OperationProgress(8, "Fechando Rocket League", "O jogo sobrescreve o perfil se ficar aberto"));
            ErrorRepair.ForceCloseRocketLeague();
            if (System.Diagnostics.Process.GetProcessesByName("RocketLeague").Length > 0)
            {
                return NoOp(
                    "NÃO CONSEGUI FECHAR O JOGO",
                    "Feche o Rocket League manualmente e tente de novo — o jogo sobrescreve o INI ao sair.");
            }
        }

        progress?.Report(new OperationProgress(12, GetPreparationMessage(action), "Preparando motor Gutty"));
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

        progress?.Report(new OperationProgress(98, "Validando resultado", "Lendo perfil aplicado"));
        OptimizerStatus after = Program.GetStatusForGui();

        if (exitCode == 0)
        {
            string? watcherMode = action switch
            {
                OptimizerAction.Completo => "COMPLETO",
                OptimizerAction.Criador => "CRIADOR",
                OptimizerAction.RepararPerfil when after.AppliedMode is "COMPLETO" or "CRIADOR"
                    => after.AppliedMode,
                _ => null,
            };
            if (watcherMode is not null)
                VideoSettingsSync.StartExitWatcher(watcherMode);
        }

        progress?.Report(new OperationProgress(100, exitCode == 0 ? "Operação concluída" : "Ação requer atenção", null));

        // CORRIGIR-EAC: exit 2 = precisa reiniciar PC (serviço 1072) — nao e falha do otimizador.
        if (action == OptimizerAction.RepararEac && exitCode == 2)
        {
            return new OperationResult(
                true,
                false,
                FeedbackTone.Warning,
                "REINICIE O PC",
                "O Windows marcou o Easy Anti-Cheat para apagar (1072). Reinicie e abra o RL. Se continuar: Verificar ficheiros na Epic.");
        }

        if (exitCode != 0)
            return BuildFailure(action, after);

        return BuildSuccess(action, before, after);
    }

    private static OperationResult? ValidatePreconditions(OptimizerAction action, OptimizerStatus status)
    {
        if (string.IsNullOrWhiteSpace(status.ConfigPath)
            && action != OptimizerAction.Diagnostico)
        {
            return NoOp(
                "PERFIL DO JOGO NÃO ENCONTRADO",
                "Abra o Rocket League uma vez para criar a pasta TAGame\\Config e tente novamente.");
        }

        // Recuperar Boot / Corrigir Tudo fecham o RL automaticamente no ExecuteCore.
        // Reparar perfil exige modo ativo — sem modo, o caminho certo e o boot nuclear.
        if (action == OptimizerAction.RepararPerfil
            && status.AppliedMode is not ("COMPLETO" or "CRIADOR"))
        {
            return NoOp(
                "NENHUM MODO ATIVO",
                "Se o jogo não abre, use RECUPERAR BOOT ou CORRIGIR TUDO. Se abre, aplique COMPLETO/CRIADOR e depois repare.");
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
                "MODO COMPLETO APLICADO",
                before.AppliedMode == "COMPLETO"
                    ? "Perfil reaplicado e sincronizado com as contas encontradas."
                    : "FPS máximo ativado, perfil de vídeo sincronizado e backup preservado."),
            OptimizerAction.Criador => Success(
                "MODO CRIADOR APLICADO",
                before.AppliedMode == "CRIADOR"
                    ? "Perfil visual reaplicado e sincronizado com as contas encontradas."
                    : "Performance forte ativada com o visual competitivo preservado."),
            OptimizerAction.Remover => Success(
                "OTIMIZAÇÃO REMOVIDA DO SISTEMA",
                "Watcher parado, INI stock, cache Epic limpo. Presets/garagem intactos. Se colou flags em Steam/Epic, remova-as manualmente."),
            OptimizerAction.CorrigirPermissoes => Success(
                "ACESSO RESTABELECIDO",
                after.IsWritable
                    ? "A pasta do jogo está gravável novamente."
                    : "O reparo terminou, mas o Windows ainda pode exigir uma exceção no Defender."),
            OptimizerAction.RepararPerfil => Success(
                "PERFIL REPARADO",
                "INI, menu de vídeo e cache foram sincronizados sem remover o modo aplicado."),
            OptimizerAction.RecuperarBoot => Success(
                "JOGO DESBLOQUEADO",
                "INI stock + boot-safe + saves em quarentena + EAC verificado. Abra o RL 1×; se erro 30005 continuar, reinicie o PC e use REPARAR EAC."),
            OptimizerAction.RepararEac => Success(
                "EAC REPARADO",
                "Serviço EasyAntiCheat_EOS reinstalado. Abra o Rocket League pela Epic/Steam."),
            OptimizerAction.CorrigirTudo => Success(
                "CORREÇÃO NUCLEAR CONCLUÍDA",
                "Caminho de boot aplicado (não reaplica o otimizador). Confirme que o jogo abre e só então volte a aplicar o modo."),
            OptimizerAction.RestaurarPresets => Success(
                "PRESETS RESTAURADOS",
                "Garagem reposta + Steam Cloud remote em quarentena. Abra OFFLINE na 1ª sessão; Steam: cloud OFF temporário."),
            OptimizerAction.CorrigirSave => Success(
                "SAVE STEAM LIMPO",
                "Steam fechada, Cloud OFF, pasta SaveData limpa (sem repor Best). Abre o RL → se LOAD FAILURE usa NEW SAVE (tutorial pode aparecer; itens online ficam). Depois RESTAURAR PRESETS. Guia no Desktop."),
            _ => Success("OPERAÇÃO CONCLUÍDA", "O estado do otimizador foi atualizado."),
        };

    private static OperationResult BuildFailure(OptimizerAction action, OptimizerStatus status)
    {
        if (status.IsRocketLeagueOpen)
        {
            return Failure(
                "AÇÃO BLOQUEADA PELO JOGO",
                "Feche o Rocket League e execute a operação novamente.");
        }

        if (!status.IsWritable)
        {
            return Failure(
                "PASTA AINDA BLOQUEADA",
                "Execute CORRIGIR PERMISSÕES como administrador ou permita o aplicativo no Acesso Controlado a Pastas.");
        }

        return action switch
        {
            OptimizerAction.RepararPerfil => Failure(
                "REPARO PARCIAL",
                "O modo foi detectado, mas INI, save ou cache não sincronizou por completo."),
            OptimizerAction.RecuperarBoot => Failure(
                "RECUPERAÇÃO INCOMPLETA",
                "Não foi possível restaurar todos os arquivos stock/saves/EAC. Se viu erro 30005, reinicie o PC e use REPARAR EAC."),
            OptimizerAction.RepararEac => Failure(
                "EAC AINDA FALHOU",
                "Reinicie o PC (serviço marcado para apagar — 1072). Depois Epic → Verificar ficheiros e abra o jogo."),
            OptimizerAction.RestaurarPresets => Failure(
                "SEM BACKUP RECUPERÁVEL",
                "Não há save grande em Backups/Presets/Best. Sem esse arquivo local não dá para recuperar a garagem."),
            _ => Failure(
                "OPERAÇÃO NÃO CONCLUÍDA",
                "O motor preservou o backup, mas uma etapa falhou. Gere o pacote de logs e manda pro Gutty."),
        };
    }

    private static OperationResult BuildSupportPackResult(SupportLogService.PackResult pack)
    {
        if (!pack.Success)
            return Failure("PACOTE NÃO GERADO", pack.Summary);

        if (pack.HasIssues)
        {
            return new OperationResult(
                true,
                false,
                FeedbackTone.Warning,
                "PACOTE PRONTO — TEM ALERTAS",
                pack.Summary);
        }

        return Success("PACOTE PRONTO NO DESKTOP", pack.Summary);
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
            OptimizerAction.Completo or OptimizerAction.Criador => "Criando backup e limpando perfil anterior",
            OptimizerAction.Remover => "Parando watcher e preservando presets",
            OptimizerAction.RestaurarPresets => "Localizando o melhor backup de garagem",
            OptimizerAction.CorrigirSave => "Analisando saves Steam e remote Cloud",
            _ => "Analisando INI, saves e permissões",
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

    private static OperationResult Success(string title, string message) =>
        new(true, false, FeedbackTone.Success, title, message);

    private static OperationResult Failure(string title, string message) =>
        new(false, false, FeedbackTone.Error, title, message);

    private static OperationResult NoOp(string title, string message) =>
        new(false, true, FeedbackTone.Warning, title, message);
}
