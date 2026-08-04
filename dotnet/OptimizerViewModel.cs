using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;

namespace GuttyRL;

internal sealed class OptimizerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly OptimizerService _service;
    private readonly DispatcherTimer _refreshTimer;
    private readonly List<AsyncRelayCommand> _operationCommands = new();
    private OptimizerSection _selectedSection = OptimizerSection.Dashboard;
    private bool _isBusy;
    private bool _isStarted;
    private bool _refreshInFlight;
    private int _progressValue;
    private string _operationTitle = "PREPARANDO OPERAÇÃO";
    private string _progressMessage = "Aguarde um instante";
    private string _progressDetail = string.Empty;
    private string _appliedMode = "CARREGANDO";
    private string _stateLabel = "Lendo perfil do Rocket League";
    private string _writableLabel = "…";
    private string _writableHint = "A verificar permissão da pasta";
    private string _rocketLeagueLabel = "…";
    private string _rocketLeagueHint = "A verificar se o jogo está aberto";
    private string _protectedLabel = "…";
    private string _protectedHint = "A verificar bloqueio do perfil";
    private string _administratorLabel = "…";
    private string _administratorHint = "A verificar direitos de administrador";
    private string _configPath = "Localizando TASystemSettings.ini...";
    private string _lastUpdated = "AGORA";
    private bool _isWritable;
    private bool _isRocketLeagueOpen;
    private bool _isProtected;
    private bool _isAdministrator;
    private bool _configExists;
    private bool _isFeedbackVisible;
    private string _feedbackTitle = string.Empty;
    private string _feedbackMessage = string.Empty;
    private FeedbackTone _feedbackTone = FeedbackTone.Success;
    private bool _isConfirmationVisible;
    private string _confirmationTitle = string.Empty;
    private string _confirmationMessage = string.Empty;
    private string _confirmationActionLabel = "CONTINUAR";
    private OptimizerAction? _pendingAction;

    public OptimizerViewModel(OptimizerService service)
    {
        _service = service;
        LaunchCommandText = service.LaunchCommand;

        NavigateDashboardCommand = new RelayCommand(() => Navigate(OptimizerSection.Dashboard));
        NavigateOptimizationCommand = new RelayCommand(() => Navigate(OptimizerSection.Otimizacao));
        NavigateRecoveryCommand = new RelayCommand(() => Navigate(OptimizerSection.Recuperacao));
        NavigateSystemCommand = new RelayCommand(() => Navigate(OptimizerSection.Sistema));
        DismissFeedbackCommand = new RelayCommand(DismissFeedback);
        CancelConfirmationCommand = new RelayCommand(
            CancelConfirmation,
            () => IsConfirmationVisible && !IsBusy);

        CompletoCommand = CreateOperationCommand(OptimizerAction.Completo);
        CriadorCommand = CreateOperationCommand(OptimizerAction.Criador);
        RemoverCommand = CreateOperationCommand(OptimizerAction.Remover);
        CopiarComandoCommand = CreateOperationCommand(OptimizerAction.CopiarComando);
        CorrigirPermissoesCommand = CreateOperationCommand(OptimizerAction.CorrigirPermissoes);
        RepararPerfilCommand = CreateOperationCommand(OptimizerAction.RepararPerfil);
        RecuperarBootCommand = CreateOperationCommand(OptimizerAction.RecuperarBoot);
        RepararEacCommand = CreateOperationCommand(OptimizerAction.RepararEac);
        DiagnosticoCommand = CreateOperationCommand(OptimizerAction.Diagnostico);
        CorrigirTudoCommand = CreateOperationCommand(OptimizerAction.CorrigirTudo);
        RestaurarPresetsCommand = CreateOperationCommand(OptimizerAction.RestaurarPresets);
        CorrigirSaveCommand = CreateOperationCommand(OptimizerAction.CorrigirSave);
        ConfirmOperationCommand = new AsyncRelayCommand(
            ConfirmOperationAsync,
            ShowUnexpectedError,
            () => IsConfirmationVisible && !IsBusy);
        _operationCommands.Add((AsyncRelayCommand)ConfirmOperationCommand);

        RefreshStatusCommand = new AsyncRelayCommand(
            () => RefreshStatusAsync(showFeedback: true, checkUpdates: true),
            ShowUnexpectedError,
            () => !IsBusy && !IsConfirmationVisible);
        _operationCommands.Add((AsyncRelayCommand)RefreshStatusCommand);

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(5),
        };
        _refreshTimer.Tick += OnRefreshTimerTick;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand NavigateDashboardCommand { get; }
    public ICommand NavigateOptimizationCommand { get; }
    public ICommand NavigateRecoveryCommand { get; }
    public ICommand NavigateSystemCommand { get; }
    public ICommand DismissFeedbackCommand { get; }
    public ICommand CancelConfirmationCommand { get; }
    public ICommand ConfirmOperationCommand { get; }
    public ICommand CompletoCommand { get; }
    public ICommand CriadorCommand { get; }
    public ICommand RemoverCommand { get; }
    public ICommand CopiarComandoCommand { get; }
    public ICommand CorrigirPermissoesCommand { get; }
    public ICommand RepararPerfilCommand { get; }
    public ICommand RecuperarBootCommand { get; }
    public ICommand RepararEacCommand { get; }
    public ICommand DiagnosticoCommand { get; }
    public ICommand CorrigirTudoCommand { get; }
    public ICommand RestaurarPresetsCommand { get; }
    public ICommand CorrigirSaveCommand { get; }
    public ICommand RefreshStatusCommand { get; }

    public string LaunchCommandText { get; }
    public string VersionLabel => AppMeta.Version;

    public bool IsDashboardVisible => SelectedSection == OptimizerSection.Dashboard;
    public bool IsOptimizationVisible => SelectedSection == OptimizerSection.Otimizacao;
    public bool IsRecoveryVisible => SelectedSection == OptimizerSection.Recuperacao;
    public bool IsSystemVisible => SelectedSection == OptimizerSection.Sistema;

    public OptimizerSection SelectedSection
    {
        get => _selectedSection;
        private set
        {
            if (!SetProperty(ref _selectedSection, value))
                return;

            OnPropertyChanged(nameof(IsDashboardVisible));
            OnPropertyChanged(nameof(IsOptimizationVisible));
            OnPropertyChanged(nameof(IsRecoveryVisible));
            OnPropertyChanged(nameof(IsSystemVisible));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (!SetProperty(ref _isBusy, value))
                return;

            RaiseCommandStates();
        }
    }

    public int ProgressValue
    {
        get => _progressValue;
        set
        {
            if (!SetProperty(ref _progressValue, Math.Clamp(value, 0, 100)))
                return;
            OnPropertyChanged(nameof(ProgressBarPixelWidth));
            NotifyProgressSteps();
        }
    }

    /// <summary>Largura visual da barra (sem ProgressBar WPF — evita crash TwoWay).</summary>
    public double ProgressBarPixelWidth => ProgressValue / 100.0 * 404.0;

    public string OperationTitle
    {
        get => _operationTitle;
        private set => SetProperty(ref _operationTitle, value);
    }

    public string ProgressMessage
    {
        get => _progressMessage;
        private set => SetProperty(ref _progressMessage, value);
    }

    public string ProgressDetail
    {
        get => _progressDetail;
        private set => SetProperty(ref _progressDetail, value);
    }

    public bool IsProgressDetailVisible => !string.IsNullOrWhiteSpace(ProgressDetail);

    public bool IsStepBackupDone => ProgressValue >= 24;
    public bool IsStepCleanDone => ProgressValue >= 35;
    public bool IsStepWriteDone => ProgressValue >= 50;
    public bool IsStepSyncDone => ProgressValue >= 88;
    public bool IsStepSyncActive => ProgressValue >= 50 && ProgressValue < 88;
    public bool IsStepFinishDone => ProgressValue >= 100;

    public string AppliedMode
    {
        get => _appliedMode;
        private set => SetProperty(ref _appliedMode, value);
    }

    public string StateLabel
    {
        get => _stateLabel;
        private set => SetProperty(ref _stateLabel, value);
    }

    public string WritableLabel
    {
        get => _writableLabel;
        private set => SetProperty(ref _writableLabel, value);
    }

    public string WritableHint
    {
        get => _writableHint;
        private set => SetProperty(ref _writableHint, value);
    }

    public string RocketLeagueLabel
    {
        get => _rocketLeagueLabel;
        private set => SetProperty(ref _rocketLeagueLabel, value);
    }

    public string RocketLeagueHint
    {
        get => _rocketLeagueHint;
        private set => SetProperty(ref _rocketLeagueHint, value);
    }

    public string ProtectedLabel
    {
        get => _protectedLabel;
        private set => SetProperty(ref _protectedLabel, value);
    }

    public string ProtectedHint
    {
        get => _protectedHint;
        private set => SetProperty(ref _protectedHint, value);
    }

    public string AdministratorLabel
    {
        get => _administratorLabel;
        private set => SetProperty(ref _administratorLabel, value);
    }

    public string AdministratorHint
    {
        get => _administratorHint;
        private set => SetProperty(ref _administratorHint, value);
    }

    public string ConfigPath
    {
        get => _configPath;
        private set => SetProperty(ref _configPath, value);
    }

    public string LastUpdated
    {
        get => _lastUpdated;
        private set => SetProperty(ref _lastUpdated, value);
    }

    public bool IsWritable
    {
        get => _isWritable;
        private set => SetProperty(ref _isWritable, value);
    }

    public bool IsRocketLeagueOpen
    {
        get => _isRocketLeagueOpen;
        private set => SetProperty(ref _isRocketLeagueOpen, value);
    }

    public bool IsProtected
    {
        get => _isProtected;
        private set => SetProperty(ref _isProtected, value);
    }

    public bool IsAdministrator
    {
        get => _isAdministrator;
        private set => SetProperty(ref _isAdministrator, value);
    }

    public bool ConfigExists
    {
        get => _configExists;
        private set => SetProperty(ref _configExists, value);
    }

    public bool IsFeedbackVisible
    {
        get => _isFeedbackVisible;
        private set => SetProperty(ref _isFeedbackVisible, value);
    }

    public string FeedbackTitle
    {
        get => _feedbackTitle;
        private set => SetProperty(ref _feedbackTitle, value);
    }

    public string FeedbackMessage
    {
        get => _feedbackMessage;
        private set => SetProperty(ref _feedbackMessage, value);
    }

    public bool IsFeedbackSuccess => _feedbackTone == FeedbackTone.Success;
    public bool IsFeedbackWarning => _feedbackTone == FeedbackTone.Warning;
    public bool IsFeedbackError => _feedbackTone == FeedbackTone.Error;

    public bool IsConfirmationVisible
    {
        get => _isConfirmationVisible;
        private set
        {
            if (SetProperty(ref _isConfirmationVisible, value))
                RaiseCommandStates();
        }
    }

    public string ConfirmationTitle
    {
        get => _confirmationTitle;
        private set => SetProperty(ref _confirmationTitle, value);
    }

    public string ConfirmationMessage
    {
        get => _confirmationMessage;
        private set => SetProperty(ref _confirmationMessage, value);
    }

    public string ConfirmationActionLabel
    {
        get => _confirmationActionLabel;
        private set => SetProperty(ref _confirmationActionLabel, value);
    }

    public void Start()
    {
        if (_isStarted)
            return;

        _isStarted = true;
        _refreshTimer.Start();
        _ = InitializeAsync();
    }

    public void Dispose()
    {
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
    }

    private AsyncRelayCommand CreateOperationCommand(OptimizerAction action)
    {
        var command = new AsyncRelayCommand(
            () => RequestOperationAsync(action),
            ShowUnexpectedError,
            () => !IsBusy && !IsConfirmationVisible);
        _operationCommands.Add(command);
        return command;
    }

    private Task RequestOperationAsync(OptimizerAction action)
    {
        if (!TryGetConfirmation(action, out string title, out string message, out string actionLabel))
            return RunOperationAsync(action);

        _pendingAction = action;
        ConfirmationTitle = title;
        ConfirmationMessage = message;
        ConfirmationActionLabel = actionLabel;
        IsConfirmationVisible = true;
        return Task.CompletedTask;
    }

    private async Task ConfirmOperationAsync()
    {
        if (_pendingAction is not OptimizerAction action)
            return;

        _pendingAction = null;
        IsConfirmationVisible = false;
        await RunOperationAsync(action);
    }

    private async Task InitializeAsync()
    {
        try
        {
            await RefreshStatusAsync(showFeedback: false, checkUpdates: false);
            await CheckForUpdatesAsync(forceToast: false, showFeedback: false);
        }
        catch (Exception ex)
        {
            ShowUnexpectedError(ex);
        }
    }

    private async Task RunOperationAsync(OptimizerAction action)
    {
        if (IsBusy)
            return;

        IsBusy = true;
        DismissFeedback();
        ProgressValue = 0;
        OperationTitle = GetOperationTitle(action);
        ProgressMessage = "Preparando ambiente";
        ProgressDetail = string.Empty;
        NotifyProgressSteps();

        var progress = new Progress<OperationProgress>(value =>
        {
            ProgressValue = Math.Clamp(value.Percentage, 0, 100);
            ProgressMessage = value.Message;
            ProgressDetail = value.Detail ?? string.Empty;
        });

        try
        {
            OperationResult result = await _service.ExecuteAsync(action, progress);
            ShowFeedback(result);
            await RefreshStatusCoreAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NotifyProgressSteps()
    {
        OnPropertyChanged(nameof(IsProgressDetailVisible));
        OnPropertyChanged(nameof(IsStepBackupDone));
        OnPropertyChanged(nameof(IsStepCleanDone));
        OnPropertyChanged(nameof(IsStepWriteDone));
        OnPropertyChanged(nameof(IsStepSyncDone));
        OnPropertyChanged(nameof(IsStepSyncActive));
        OnPropertyChanged(nameof(IsStepFinishDone));
    }

    private async Task RefreshStatusAsync(bool showFeedback, bool checkUpdates)
    {
        if (IsBusy || _refreshInFlight)
            return;

        _refreshInFlight = true;
        try
        {
            await RefreshStatusCoreAsync();

            if (checkUpdates)
            {
                await CheckForUpdatesAsync(forceToast: true, showFeedback: showFeedback);
                return;
            }

            if (showFeedback)
            {
                ShowFeedback(new OperationResult(
                    true,
                    false,
                    FeedbackTone.Success,
                    "STATUS ATUALIZADO",
                    "Perfil, acesso e processo do Rocket League foram verificados."));
            }
        }
        finally
        {
            _refreshInFlight = false;
        }
    }

    private async Task CheckForUpdatesAsync(bool forceToast, bool showFeedback)
    {
        UpdateCheckResult update = await UpdateCheckService.CheckLatestAsync(force: forceToast);
        if (!update.Success)
        {
            if (showFeedback)
            {
                ShowFeedback(new OperationResult(
                    false,
                    true,
                    FeedbackTone.Warning,
                    "GITHUB INDISPONÍVEL",
                    update.Message));
            }

            return;
        }

        if (update.UpdateAvailable)
        {
            LastUpdated = $"NOVA {update.LatestTag} · local {update.CurrentVersion}";
            if (forceToast || !UpdateCheckService.WasDismissed(update.LatestTag))
                UpdateToastWindow.ShowUpdate(update);

            if (showFeedback)
            {
                ShowFeedback(new OperationResult(
                    true,
                    false,
                    FeedbackTone.Warning,
                    "VERSÃO NOVA NO GITHUB",
                    update.Message + " Use BAIXAR no popup ou abra Releases."));
            }

            return;
        }

        LastUpdated = DateTime.Now.ToString("'ATUALIZADO' HH:mm:ss") + " · " + update.CurrentVersion;
        if (showFeedback)
        {
            ShowFeedback(new OperationResult(
                true,
                false,
                FeedbackTone.Success,
                "ÚLTIMA VERSÃO",
                update.Message));
        }
    }

    private async Task RefreshStatusCoreAsync()
    {
        OptimizerStatus status = await _service.GetStatusAsync();
        AppliedMode = status.AppliedMode;
        StateLabel = status.StateLabel;
        IsWritable = status.IsWritable;
        IsRocketLeagueOpen = status.IsRocketLeagueOpen;
        IsProtected = status.IsProtected;
        IsAdministrator = status.IsAdministrator;
        ConfigExists = status.ConfigExists;

        // Textos curtos em PT-BR — cabem nos cards sem cortar.
        WritableLabel = status.IsWritable ? "OK" : "BLOQUEADA";
        WritableHint = status.IsWritable
            ? "Podemos salvar o perfil"
            : "Sem permissão — Corrigir";

        RocketLeagueLabel = status.IsRocketLeagueOpen ? "ABERTO" : "FECHADO";
        RocketLeagueHint = status.IsRocketLeagueOpen
            ? "Feche o jogo pra aplicar"
            : "Pode aplicar um modo";

        // Travar INI = arquivo só leitura. Nos modos atuais fica DESLIGADO
        // de propósito pra o menu de vídeo do jogo funcionar.
        ProtectedLabel = status.IsProtected ? "LIGADO" : "DESLIGADO";
        ProtectedHint = status.IsProtected
            ? "Só leitura — modo travado"
            : "Normal — vídeo livre no jogo";

        AdministratorLabel = status.IsAdministrator ? "SIM" : "NÃO";
        AdministratorHint = status.IsAdministrator
            ? "Rodando como admin"
            : "Abra como admin se falhar";

        ConfigPath = string.IsNullOrWhiteSpace(status.ConfigPath)
            ? "Perfil ainda não encontrado — abra o Rocket League uma vez."
            : status.ConfigPath;
        LastUpdated = DateTime.Now.ToString("'ATUALIZADO' HH:mm:ss");
    }

    private void Navigate(OptimizerSection section) => SelectedSection = section;

    private void ShowFeedback(OperationResult result)
    {
        _feedbackTone = result.Tone;
        FeedbackTitle = result.Title;
        FeedbackMessage = result.Message;
        IsFeedbackVisible = true;
        OnPropertyChanged(nameof(IsFeedbackSuccess));
        OnPropertyChanged(nameof(IsFeedbackWarning));
        OnPropertyChanged(nameof(IsFeedbackError));
    }

    private void ShowUnexpectedError(Exception ex)
    {
        AppMeta.Log($"GUI command: {ex.GetType().Name}: {ex.Message}");
        _feedbackTone = FeedbackTone.Error;
        FeedbackTitle = "ERRO NA INTERFACE";
        FeedbackMessage = "A ação foi interrompida com segurança. Consulte " + AppMeta.LogFile;
        IsFeedbackVisible = true;
        IsBusy = false;
        OnPropertyChanged(nameof(IsFeedbackSuccess));
        OnPropertyChanged(nameof(IsFeedbackWarning));
        OnPropertyChanged(nameof(IsFeedbackError));
    }

    private void DismissFeedback() => IsFeedbackVisible = false;

    private void CancelConfirmation()
    {
        _pendingAction = null;
        IsConfirmationVisible = false;
    }

    private void RaiseCommandStates()
    {
        foreach (AsyncRelayCommand command in _operationCommands)
            command.RaiseCanExecuteChanged();

        if (CancelConfirmationCommand is RelayCommand cancel)
            cancel.RaiseCanExecuteChanged();
    }

    private void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        if (!IsBusy)
            _ = RefreshStatusSafeAsync();
    }

    private async Task RefreshStatusSafeAsync()
    {
        try
        {
            await RefreshStatusAsync(showFeedback: false, checkUpdates: false);
        }
        catch (Exception ex)
        {
            AppMeta.Log($"GUI refresh: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static string GetOperationTitle(OptimizerAction action) =>
        action switch
        {
            OptimizerAction.Completo => "APLICANDO MODO COMPLETO",
            OptimizerAction.Criador => "APLICANDO MODO CRIADOR",
            OptimizerAction.Remover => "REMOVENDO OTIMIZAÇÃO",
            OptimizerAction.CopiarComando => "COPIANDO COMANDO",
            OptimizerAction.CorrigirPermissoes => "CORRIGINDO PERMISSÕES",
            OptimizerAction.RepararPerfil => "REPARANDO PERFIL",
            OptimizerAction.RecuperarBoot => "RECUPERANDO BOOT",
            OptimizerAction.RepararEac => "REPARANDO EASY ANTI-CHEAT",
            OptimizerAction.Diagnostico => "GERANDO PACOTE DE LOGS",
            OptimizerAction.CorrigirTudo => "CORRIGINDO TUDO",
            OptimizerAction.RestaurarPresets => "RESTAURANDO PRESETS",
            OptimizerAction.CorrigirSave => "CORRIGINDO SAVE / LOAD FAILURE",
            _ => "EXECUTANDO OPERAÇÃO",
        };

    private static bool TryGetConfirmation(
        OptimizerAction action,
        out string title,
        out string message,
        out string actionLabel)
    {
        (title, message, actionLabel) = action switch
        {
            OptimizerAction.Remover => (
                "REMOVER OTIMIZAÇÃO DO SISTEMA?",
                "Para o watcher automático, restaura o INI stock/original, limpa o cache Epic e remove marcas Gutty. Presets e garagem ficam no cofre. Não apaga a pasta de backups.",
                "REMOVER DO SISTEMA"),
            OptimizerAction.RecuperarBoot => (
                "JOGO NÃO ABRE — RECUPERAR BOOT?",
                "Fecha o RL, remove o otimizador (INI stock), limpa boot-killers, põe saves suspeitos em quarentena e repara Easy Anti-Cheat (erro 30005). A garagem fica no Best. Só reaplique COMPLETO/CRIADOR depois do jogo abrir.",
                "DESBLOQUEAR JOGO"),
            OptimizerAction.RepararEac => (
                "ERRO EAC 30005 — REPARAR ANTI-CHEAT?",
                "Reinstala o serviço EasyAntiCheat_EOS (CreateService 1072). Não mexe no INI. Se o Windows marcou o serviço para apagar, pode ser preciso reiniciar o PC.",
                "REPARAR EAC"),
            OptimizerAction.CorrigirTudo => (
                "JOGO NÃO ABRE — CORRIGIR TUDO?",
                "Mesmo caminho nuclear do Recuperar Boot: prioridade é o Rocket League voltar a abrir. NÃO reaplica COMPLETO/CRIADOR em cima do perfil partido.",
                "DESBLOQUEAR AGORA"),
            OptimizerAction.RestaurarPresets => (
                "RESTAURAR PRESETS?",
                "Vamos repor a maior garagem guardada (cofre Best) nas contas Epic/Steam e quarentenar o remote Steam Cloud. Depois abra o jogo OFFLINE na 1ª sessão.",
                "RESTAURAR BACKUP"),
            OptimizerAction.CorrigirSave => (
                "LOAD FAILURE — LIMPAR SAVE STEAM?",
                "Fecha Steam+RL, desliga Cloud no localconfig, limpa SaveData (sem repor Best — isso fazia o aviso voltar). Depois abre o RL e se o aviso aparecer clica NEW SAVE (tutorial às vezes é normal; rank/itens online ficam). Só depois RESTAURAR PRESETS.",
                "LIMPAR SAVE"),
            _ => (string.Empty, string.Empty, string.Empty),
        };

        return title.Length > 0;
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
