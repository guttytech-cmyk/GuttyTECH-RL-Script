using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

namespace GuttyRL;

public partial class UpdateToastWindow : Window
{
    private readonly UpdateToastViewModel _model;
    private readonly DispatcherTimer _autoCloseTimer;

    internal UpdateToastWindow(UpdateCheckResult update)
    {
        InitializeComponent();
        _model = new UpdateToastViewModel(update);
        DataContext = _model;

        _autoCloseTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(45),
        };
        _autoCloseTimer.Tick += (_, _) =>
        {
            _autoCloseTimer.Stop();
            Close();
        };

        Loaded += OnLoaded;
        Closed += (_, _) => _autoCloseTimer.Stop();
    }

    internal static void ShowUpdate(UpdateCheckResult update)
    {
        if (!update.UpdateAvailable)
            return;

        var app = Application.Current;
        if (app?.Dispatcher is null)
            return;

        if (!app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => ShowUpdate(update));
            return;
        }

        var toast = new UpdateToastWindow(update);
        toast.Show();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLayout();
        PositionBottomRight();
        _autoCloseTimer.Start();
        Opacity = 0;
        var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220));
        BeginAnimation(OpacityProperty, fade);
    }

    private void PositionBottomRight()
    {
        Rect work = SystemParameters.WorkArea;
        double width = ActualWidth > 0 ? ActualWidth : Width;
        double height = ActualHeight > 0 ? ActualHeight : 180;
        Left = work.Right - width - 16;
        Top = work.Bottom - height - 16;
    }

    private void OnDismissClick(object sender, RoutedEventArgs e)
    {
        UpdateCheckService.Dismiss(_model.Tag);
        Close();
    }

    private async void OnDownloadClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_model.DownloadUrl) && !string.IsNullOrWhiteSpace(_model.Tag))
            {
                _model.Detail = "A baixar para o Desktop…";
                string? path = await UpdateCheckService.DownloadLatestAsync(_model.DownloadUrl, _model.Tag);
                if (!string.IsNullOrWhiteSpace(path))
                {
                    UpdateCheckService.Dismiss(_model.Tag);
                    UpdateCheckService.OpenUrl(path);
                    Close();
                    return;
                }
            }

            UpdateCheckService.OpenUrl(_model.ReleaseUrl);
            UpdateCheckService.Dismiss(_model.Tag);
            Close();
        }
        catch (Exception ex)
        {
            AppMeta.Log("UPDATE-TOAST download: " + ex.Message);
            _model.Detail = "Download falhou — abrindo a página da release.";
            UpdateCheckService.OpenUrl(_model.ReleaseUrl);
        }
    }

    private sealed class UpdateToastViewModel : INotifyPropertyChanged
    {
        private string _detail;

        public UpdateToastViewModel(UpdateCheckResult update)
        {
            Tag = update.LatestTag ?? "";
            DownloadUrl = update.DownloadUrl;
            ReleaseUrl = update.ReleaseUrl;
            Message = update.Message;
            _detail = string.IsNullOrWhiteSpace(update.ReleaseName)
                ? "Baixa o .exe novo, fecha este app e abre o arquivo do Desktop."
                : update.ReleaseName!;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Tag { get; }
        public string? DownloadUrl { get; }
        public string? ReleaseUrl { get; }
        public string Message { get; }

        public string Detail
        {
            get => _detail;
            set
            {
                if (_detail == value) return;
                _detail = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Detail)));
            }
        }
    }
}
