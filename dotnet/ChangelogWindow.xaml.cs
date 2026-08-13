using System.Windows;
using System.Windows.Media.Animation;

namespace GuttyRL;

public partial class ChangelogWindow : Window
{
    private readonly string? _markShownVersion;

    internal ChangelogWindow(string tag, string notes, string? subtitle, string? markShownVersion)
    {
        InitializeComponent();
        _markShownVersion = markShownVersion;
        TitleText.Text = string.IsNullOrWhiteSpace(tag) ? "O QUE MUDOU" : tag.ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(subtitle))
            SubtitleText.Text = subtitle;
        // WPF nao renderiza Markdown — remove **, `, links, etc.
        NotesBox.Text = ReleaseNotesFormatter.StripMarkdown(notes.Trim());
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

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_markShownVersion))
            WhatsNewService.MarkShown(_markShownVersion);
        Close();
    }
}
