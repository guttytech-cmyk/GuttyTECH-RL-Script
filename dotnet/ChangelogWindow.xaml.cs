using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace GuttyRL;

public partial class ChangelogWindow : Window
{
    private readonly string? _markShownVersion;

    internal ChangelogWindow(string tag, string notes, string? subtitle, string? markShownVersion)
    {
        InitializeComponent();
        _markShownVersion = markShownVersion;
        IReadOnlyList<ChangelogVersionCard> cards = ChangelogRange.ParseCards(notes);
        DataContext = new ChangelogWindowModel
        {
            TitleText = string.IsNullOrWhiteSpace(tag) ? "O QUE MUDOU" : tag.ToUpperInvariant(),
            SubtitleText = string.IsNullOrWhiteSpace(subtitle)
                ? "Atualizou? Aqui está o que mudou nas últimas versões."
                : subtitle,
            Versions = cards,
        };
        Loaded += OnLoaded;
    }

    internal static void Show(string tag, string notes, string? subtitle = null, string? markShownVersion = null)
    {
        var app = Application.Current;
        if (app?.Dispatcher is null)
            return;

        if (!app.Dispatcher.CheckAccess())
        {
            app.Dispatcher.Invoke(() => Show(tag, notes, subtitle, markShownVersion));
            return;
        }

        var win = new ChangelogWindow(tag, notes, subtitle, markShownVersion)
        {
            Owner = app.MainWindow is { IsVisible: true } mw ? mw : null,
        };
        win.Show();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Opacity = 0;
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
    }

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_markShownVersion))
            WhatsNewService.MarkShown(_markShownVersion);
        Close();
    }
}
