using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace GuttyRL;

public partial class MainWindow : Window
{
    private readonly OptimizerViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new OptimizerViewModel(OptimizerService.Instance);
        DataContext = _viewModel;
        UiLanguage.Changed += OnLanguageChanged;
        UpdateLanguageVisual();
        UpdateMaximizeVisual();
    }

    private void OnPortugueseClick(object sender, RoutedEventArgs e) =>
        UiLanguage.Use(AppLanguage.Portuguese);

    private void OnEnglishClick(object sender, RoutedEventArgs e) =>
        UiLanguage.Use(AppLanguage.English);

    private void OnLanguageChanged()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(OnLanguageChanged);
            return;
        }

        UpdateLanguageVisual();
        UpdateMaximizeVisual();
    }

    private void UpdateLanguageVisual()
    {
        bool english = UiLanguage.IsEnglish;
        PortugueseButton.Tag = english ? null : "on";
        EnglishButton.Tag = english ? "on" : null;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _viewModel.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        UiLanguage.Changed -= OnLanguageChanged;
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeClick(object sender, RoutedEventArgs e) =>
        ToggleMaximize();

    private void OnCloseClick(object sender, RoutedEventArgs e) =>
        Close();

    private void OnWindowStateChanged(object? sender, EventArgs e) =>
        UpdateMaximizeVisual();

    private void ToggleMaximize() =>
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;

    private void UpdateMaximizeVisual()
    {
        bool maximized = WindowState == WindowState.Maximized;
        MaximizeButton.ToolTip = maximized ? UiCopy.Get("cap.restore") : UiCopy.Get("cap.max");
        MaximizeIcon.Data = Geometry.Parse(
            maximized
                ? "M3,1 H9 V7 H8 V2 H3 Z M1,3 H7 V9 H1 Z"
                : "M1,1 H9 V9 H1 Z");
    }
}
