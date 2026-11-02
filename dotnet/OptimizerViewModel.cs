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
    private bool _isBusy;
    private bool _isStarted;
    private bool _refreshInFlight;
    private bool _isCheckingUpdates;
    private int _progressValue;
    private string _operationTitle = UiCopy.Get("op.preparing");
    private string _progressMessage = UiCopy.Get("prog.wait");
    private string _progressDetail = string.Empty;
    private string _progressMessageKey = "prog.wait";
    private string? _progressDetailKey;
    private string _appliedMode = UiCopy.Get("state.loading");
    private string _stateLabel = UiCopy.Get("state.reading");
    private string _writableLabel = "…";
    private string _writableHint = UiCopy.Get("hint.writable");
    private string _rocketLeagueLabel = "…";
    private string _rocketLeagueHint = UiCopy.Get("hint.game");
    private string _protectedLabel = "…";
    private string _protectedHint = UiCopy.Get("hint.protect");
    private string _administratorLabel = "…";
    private string _administratorHint = UiCopy.Get("hint.admin");
    private string _configPath = UiCopy.Get("dash.pathSearching");
    private string _lastUpdated = UiCopy.Get("dash.now");
    private OptimizerStatus? _status;
    private OperationResult? _lastResult;
    private OptimizerAction? _runningAction;
    private FooterMode _footer = FooterMode.Now;
    private string _footerA = "";
    private string _footerB = "";
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
    private string _confirmationActionLabel = "";
    private OptimizerAction? _pendingAction;

    private enum FooterMode
    {
        Now,
        Clock,
        Checking,
        NewVersion,
        UpToDate,
    }

    public OptimizerViewModel(OptimizerService service)
    {
        _service = service;
        LaunchCommandText = service.LaunchCommand;

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
            () => !IsBusy && !IsCheckingUpdates && !IsConfirmationVisible);
        _operationCommands.Add((AsyncRelayCommand)RefreshStatusCommand);

        _refreshTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(5),
        };
        _refreshTimer.Tick += OnRefreshTimerTick;
        UiLanguage.Changed += OnLanguageChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

    public bool IsCheckingUpdates
    {
        get => _isCheckingUpdates;
        private set
        {
            if (!SetProperty(ref _isCheckingUpdates, value))
                return;

            OnPropertyChanged(nameof(RefreshButtonLabel));
            RaiseCommandStates();
        }
    }

    public string RefreshButtonLabel =>
        UiCopy.Get(IsCheckingUpdates ? "btn.searching" : "btn.refresh");

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
        UiLanguage.Changed -= OnLanguageChanged;
        _refreshTimer.Stop();
        _refreshTimer.Tick -= OnRefreshTimerTick;
    }

    private void OnLanguageChanged()
    {
        var app = System.Windows.Application.Current;
        if (app?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(OnLanguageChanged);
            return;
        }

        OnPropertyChanged(nameof(RefreshButtonLabel));
        PublishFooter();
        if (_status is not null)
            PaintStatus(_status);
        else
        {
            AppliedMode = UiCopy.Get("state.loading");
            StateLabel = UiCopy.Get("state.reading");
            WritableHint = UiCopy.Get("hint.writable");
            RocketLeagueHint = UiCopy.Get("hint.game");
            ProtectedHint = UiCopy.Get("hint.protect");
            AdministratorHint = UiCopy.Get("hint.admin");
            ConfigPath = UiCopy.Get("dash.pathSearching");
        }
        if (_runningAction is OptimizerAction running)
            OperationTitle = GetOperationTitle(running);
        else
            OperationTitle = UiCopy.Get("op.preparing");
        ProgressMessage = UiCopy.Get(_progressMessageKey);
        ProgressDetail = string.IsNullOrEmpty(_progressDetailKey) ? "" : UiCopy.Get(_progressDetailKey);
        OnPropertyChanged(nameof(IsProgressDetailVisible));
        if (_lastResult is not null && IsFeedbackVisible)
        {
            FeedbackTitle = _lastResult.Title;
            FeedbackMessage = _lastResult.Message;
        }
        if (_pendingAction is OptimizerAction pending && IsConfirmationVisible
            && TryGetConfirmation(pending, out string title, out string message, out string actionLabel))
        {
            ConfirmationTitle = title;
            ConfirmationMessage = message;
            ConfirmationActionLabel = actionLabel;
        }
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
            // 1a abertura apos atualizar: changelog estilo Discord.
            await WhatsNewService.TryShowOnStartupAsync();
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
        _runningAction = action;
        ProgressValue = 0;
        OperationTitle = GetOperationTitle(action);
        _progressMessageKey = "prog.wait";
        _progressDetailKey = null;
        ProgressMessage = UiCopy.Get("prog.wait");
        ProgressDetail = string.Empty;
        NotifyProgressSteps();

        var progress = new Progress<OperationProgress>(value =>
        {
            ProgressValue = Math.Clamp(value.Percentage, 0, 100);
            _progressMessageKey = value.Message;
            _progressDetailKey = value.Detail;
            ProgressMessage = UiCopy.Get(value.Message);
            ProgressDetail = string.IsNullOrEmpty(value.Detail) ? "" : UiCopy.Get(value.Detail);
        });

        try
        {
            OperationResult result = await _service.ExecuteAsync(action, progress);
            ShowFeedback(result);
            await RefreshStatusCoreAsync();
        }
        finally
        {
            _runningAction = null;
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
        bool userCheck = checkUpdates && showFeedback;
        if (UpdateCheckBusyState.ShouldSkip(IsBusy, IsCheckingUpdates, _refreshInFlight, userCheck))
            return;

        if (userCheck)
            BeginCheckingUpdatesUi();

        while (userCheck && _refreshInFlight)
            await Task.Delay(40);

        _refreshInFlight = true;
        try
        {
            await RefreshStatusCoreAsync();

            if (checkUpdates)
            {
                if (userCheck)
                {
                    _footer = FooterMode.Checking;
                    PublishFooter();
                }

                await CheckForUpdatesAsync(forceToast: true, showFeedback: showFeedback);
                return;
            }

            if (showFeedback)
            {
                ShowFeedback(new OperationResult(
                    true,
                    false,
                    FeedbackTone.Success,
                    "fb.statusTitle",
                    "fb.statusBody"));
            }
        }
        finally
        {
            _refreshInFlight = false;
            if (userCheck)
                EndCheckingUpdatesUi();
        }
    }

    private void BeginCheckingUpdatesUi()
    {
        _footer = FooterMode.Checking;
        PublishFooter();
        IsCheckingUpdates = true;
        ShowFeedback(new OperationResult(
            true,
            false,
            FeedbackTone.Warning,
            "upd.overlayTitle",
            "upd.overlayBody"));
    }

    private void EndCheckingUpdatesUi() => IsCheckingUpdates = false;

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
                    "fb.githubDownTitle",
                    "raw.pass",
                    update.Message));
            }

            return;
        }

        if (update.UpdateAvailable)
        {
            _footer = FooterMode.NewVersion;
            _footerA = update.LatestTag ?? "";
            _footerB = update.CurrentVersion;
            PublishFooter();
            if (forceToast || !UpdateCheckService.WasDismissed(update.LatestTag))
                UpdateToastWindow.ShowUpdate(update);

            if (showFeedback)
            {
                ShowFeedback(new OperationResult(
                    true,
                    false,
                    FeedbackTone.Warning,
                    "fb.newTitle",
                    "fb.newBody",
                    update.Message));
            }

            return;
        }

        _footer = FooterMode.UpToDate;
        _footerA = DateTime.Now.ToString("HH:mm:ss");
        _footerB = update.CurrentVersion;
        PublishFooter();
        if (showFeedback)
        {
            // Ja atualizado: ainda assim mostra o changelog da versao atual (pedido Discord).
            if (!string.IsNullOrWhiteSpace(update.ReleaseNotes))
            {
                ChangelogWindow.Show(
                    update.LatestTag ?? update.CurrentVersion,
                    update.ReleaseNotes!,
                    subtitle: UiCopy.Get("log.already"));
            }

            ShowFeedback(new OperationResult(
                true,
                false,
                FeedbackTone.Success,
                "fb.latestTitle",
                "raw.pass",
                update.Message));
        }
    }

    private async Task RefreshStatusCoreAsync()
    {
        OptimizerStatus status = await _service.GetStatusAsync();
        _status = status;
        PaintStatus(status);
        _footer = FooterMode.Clock;
        _footerA = DateTime.Now.ToString("HH:mm:ss");
        PublishFooter();
    }

    private void PaintStatus(OptimizerStatus status)
    {
        AppliedMode = LocalMode(status.AppliedMode);
        StateLabel = LocalState(status.StateLabel);
        IsWritable = status.IsWritable;
        IsRocketLeagueOpen = status.IsRocketLeagueOpen;
        IsProtected = status.IsProtected;
        IsAdministrator = status.IsAdministrator;
        ConfigExists = status.ConfigExists;

        WritableLabel = status.IsWritable ? UiCopy.Get("card.ok") : UiCopy.Get("card.blocked");
        WritableHint = status.IsWritable ? UiCopy.Get("hint.canWrite") : UiCopy.Get("hint.noPerm");

        RocketLeagueLabel = status.IsRocketLeagueOpen ? UiCopy.Get("card.open") : UiCopy.Get("card.closed");
        RocketLeagueHint = status.IsRocketLeagueOpen ? UiCopy.Get("hint.closeGame") : UiCopy.Get("hint.canApply");

        IsProtected = status.IsProtected;
        if (status.IsProtected)
        {
            if (status.IsWatcherActive)
            {
                ProtectedLabel = UiCopy.Get("card.active");
                ProtectedHint = UiCopy.Get("hint.watcher");
            }
            else
            {
                ProtectedLabel = UiCopy.Get("card.modeOnly");
                ProtectedHint = UiCopy.Get("hint.watcherStarting");
            }
        }
        else
        {
            ProtectedLabel = UiCopy.Get("card.off");
            ProtectedHint = UiCopy.Get("hint.noMode");
        }

        AdministratorLabel = status.IsAdministrator ? UiCopy.Get("card.yes") : UiCopy.Get("card.no");
        AdministratorHint = status.IsAdministrator ? UiCopy.Get("hint.adminOk") : UiCopy.Get("hint.adminBad");

        ConfigPath = string.IsNullOrWhiteSpace(status.ConfigPath)
            ? UiCopy.Get("dash.pathMissing")
            : status.ConfigPath;
    }

    private static string LocalMode(string mode) => mode switch
    {
        "COMPLETO" => UiCopy.Get("mode.completo"),
        "CRIADOR" => UiCopy.Get("mode.criador"),
        "ORIGINAL" => UiCopy.Get("mode.original"),
        _ => mode,
    };

    private static string LocalState(string label) => label switch
    {
        "FPS máximo ativo" => UiCopy.Get("state.fps"),
        "Visual + perf ativo" => UiCopy.Get("state.creator"),
        "Original (não otimizado)" => UiCopy.Get("state.original"),
        "Otimizado (versão antiga)" => UiCopy.Get("state.legacy"),
        "Perfil do Rocket League ainda não localizado" => UiCopy.Get("state.missing"),
        "INI ausente — abra o Rocket League uma vez" => UiCopy.Get("state.noini"),
        _ => label,
    };

    private void PublishFooter()
    {
        LastUpdated = _footer switch
        {
            FooterMode.Checking => UiCopy.Get("upd.footerChecking"),
            FooterMode.NewVersion => UiCopy.Format("upd.footerNew", _footerA, _footerB),
            FooterMode.UpToDate => UiCopy.Format("upd.footerOk", _footerA, _footerB),
            FooterMode.Clock => UiCopy.Format("dash.updated", _footerA),
            _ => UiCopy.Get("dash.now"),
        };
    }

    private void ShowFeedback(OperationResult result)
    {
        _lastResult = result;
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
        _lastResult = new OperationResult(false, false, FeedbackTone.Error, "fb.uiErrorTitle", "fb.uiErrorBody", AppMeta.LogFile);
        FeedbackTitle = _lastResult.Title;
        FeedbackMessage = _lastResult.Message;
        IsFeedbackVisible = true;
        IsBusy = false;
        IsCheckingUpdates = false;
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
        UiCopy.Get(action switch
        {
            OptimizerAction.Completo => "op.completo",
            OptimizerAction.Criador => "op.criador",
            OptimizerAction.Remover => "op.remove",
            OptimizerAction.CopiarComando => "op.copy",
            OptimizerAction.CorrigirPermissoes => "op.perm",
            OptimizerAction.RepararPerfil => "op.profile",
            OptimizerAction.RecuperarBoot => "op.boot",
            OptimizerAction.RepararEac => "op.eac",
            OptimizerAction.Diagnostico => "op.diag",
            OptimizerAction.CorrigirTudo => "op.all",
            OptimizerAction.RestaurarPresets => "op.presets",
            OptimizerAction.CorrigirSave => "op.save",
            _ => "op.generic",
        });

    private static bool TryGetConfirmation(
        OptimizerAction action,
        out string title,
        out string message,
        out string actionLabel)
    {
        (title, message, actionLabel) = action switch
        {
            OptimizerAction.Remover => (
                UiCopy.Get("confirm.removeTitle"),
                UiCopy.Get("confirm.removeBody"),
                UiCopy.Get("confirm.removeAction")),
            OptimizerAction.RecuperarBoot => (
                UiCopy.Get("confirm.bootTitle"),
                UiCopy.Get("confirm.bootBody"),
                UiCopy.Get("confirm.bootAction")),
            OptimizerAction.RepararEac => (
                UiCopy.Get("confirm.eacTitle"),
                UiCopy.Get("confirm.eacBody"),
                UiCopy.Get("confirm.eacAction")),
            OptimizerAction.CorrigirTudo => (
                UiCopy.Get("confirm.allTitle"),
                UiCopy.Get("confirm.allBody"),
                UiCopy.Get("confirm.allAction")),
            OptimizerAction.RestaurarPresets => (
                UiCopy.Get("confirm.presetsTitle"),
                UiCopy.Get("confirm.presetsBody"),
                UiCopy.Get("confirm.presetsAction")),
            OptimizerAction.CorrigirSave => (
                UiCopy.Get("confirm.saveTitle"),
                UiCopy.Get("confirm.saveBody"),
                UiCopy.Get("confirm.saveAction")),
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
