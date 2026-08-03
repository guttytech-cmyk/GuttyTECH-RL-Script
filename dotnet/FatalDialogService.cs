using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shell;

namespace GuttyRL;

/// <summary>Modal WPF próprio para falhas fatais de inicialização.</summary>
internal static class FatalDialogService
{
    public static bool TryShow(string title, string message, string logPath)
    {
        try
        {
            Application? application = Application.Current;
            if (application?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
                return dispatcher.Invoke(() => TryShow(title, message, logPath));

            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                return false;

            ResourceDictionary resources = LoadTheme();
            Brush background = Get<Brush>(resources, "BackgroundBrush");
            Brush surface = Get<Brush>(resources, "SurfaceRaisedBrush");
            Brush border = Get<Brush>(resources, "BorderStrongBrush");
            Brush cta = Get<Brush>(resources, "CtaBrush");
            Brush primary = Get<Brush>(resources, "TextPrimaryBrush");
            Brush secondary = Get<Brush>(resources, "TextSecondaryBrush");
            Brush transparent = Get<Brush>(resources, "TransparentBrush");
            FontFamily titleFont = Get<FontFamily>(resources, "TitleFontFamily");
            FontFamily bodyFont = Get<FontFamily>(resources, "BodyFontFamily");

            var window = new Window
            {
                Title = "GUTTYTECH · FALHA SEGURA",
                Width = 540,
                Height = 330,
                MinWidth = 540,
                MinHeight = 330,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                Background = background,
                Foreground = primary,
                ShowInTaskbar = true,
                Topmost = true,
                FontFamily = bodyFont,
            };

            WindowChrome.SetWindowChrome(window, new WindowChrome
            {
                CaptionHeight = 44,
                ResizeBorderThickness = new Thickness(0),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                UseAeroCaptionButtons = false,
            });

            var closeButton = new Button
            {
                Content = "×",
                Width = 44,
                Height = 40,
                Background = transparent,
                Foreground = primary,
                BorderThickness = new Thickness(0),
                FontSize = 18,
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            WindowChrome.SetIsHitTestVisibleInChrome(closeButton, true);
            closeButton.Click += (_, _) => window.Close();

            var titleBar = new Grid
            {
                Height = 44,
                Background = surface,
            };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition());
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleBar.Children.Add(new TextBlock
            {
                Text = "GUTTYTECH  /  PROTEÇÃO DE STARTUP",
                Margin = new Thickness(18, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = titleFont,
                FontSize = 16,
                Foreground = primary,
            });
            Grid.SetColumn(closeButton, 1);
            titleBar.Children.Add(closeButton);

            var body = new StackPanel
            {
                Margin = new Thickness(28, 24, 28, 24),
            };
            body.Children.Add(new Border
            {
                Width = 42,
                Height = 5,
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = cta,
                CornerRadius = new CornerRadius(3),
            });
            body.Children.Add(new TextBlock
            {
                Text = title.ToUpperInvariant(),
                Margin = new Thickness(0, 18, 0, 0),
                FontFamily = titleFont,
                FontSize = 30,
                Foreground = primary,
            });
            body.Children.Add(new TextBlock
            {
                Text = message,
                Margin = new Thickness(0, 8, 0, 0),
                FontSize = 13,
                Foreground = secondary,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 72,
            });
            body.Children.Add(new TextBlock
            {
                Text = "LOG: " + logPath,
                Margin = new Thickness(0, 18, 0, 0),
                FontSize = 11,
                Foreground = secondary,
                TextWrapping = TextWrapping.Wrap,
            });

            var closeAction = new Button
            {
                Content = "FECHAR COM SEGURANÇA",
                Margin = new Thickness(0, 20, 0, 0),
                Padding = new Thickness(18, 10, 18, 10),
                HorizontalAlignment = HorizontalAlignment.Left,
                Background = cta,
                Foreground = primary,
                BorderBrush = cta,
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            closeAction.Click += (_, _) => window.Close();
            body.Children.Add(closeAction);

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(new RowDefinition());
            layout.Children.Add(titleBar);
            Grid.SetRow(body, 1);
            layout.Children.Add(body);

            window.Content = new Border
            {
                Background = background,
                BorderBrush = border,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = layout,
            };

            window.ShowDialog();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ResourceDictionary LoadTheme()
    {
        if (Application.Current?.Resources is { } appResources
            && appResources.Contains("BackgroundBrush"))
            return appResources;

        return new ResourceDictionary
        {
            Source = new Uri(
                "pack://application:,,,/GuttyTECH_RL;component/Themes/Theme.xaml",
                UriKind.Absolute),
        };
    }

    private static T Get<T>(ResourceDictionary resources, string key) where T : class =>
        (resources[key] ?? throw new InvalidOperationException($"Recurso visual ausente: {key}")) as T
        ?? throw new InvalidOperationException($"Recurso visual inválido: {key}");
}
