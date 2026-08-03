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
        UpdateMaximizeVisual();
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        _viewModel.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
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
        MaximizeButton.ToolTip = maximized ? "Restaurar" : "Maximizar";
        MaximizeIcon.Data = Geometry.Parse(
            maximized
                ? "M3,1 H9 V7 H8 V2 H3 Z M1,3 H7 V9 H1 Z"
                : "M1,1 H9 V9 H1 Z");
    }
}
