using System.ComponentModel;

namespace GuttyRL;

internal enum AppLanguage
{
    Portuguese,
    English,
}

/// <summary>Idioma da UI. Padrão: português. Persistido em language.tag.</summary>
internal static class UiLanguage
{
    public static event Action? Changed;

    public static AppLanguage Language { get; private set; } = AppLanguage.Portuguese;

    public static bool IsEnglish => Language == AppLanguage.English;

    public static UiLanguageState State { get; } = new();

    public static void Load()
    {
        try
        {
            string path = Path.Combine(AppMeta.GuttyDir, "language.tag");
            if (!File.Exists(path))
                return;

            string raw = File.ReadAllText(path).Trim();
            Language = raw.Equals("en", StringComparison.OrdinalIgnoreCase)
                ? AppLanguage.English
                : AppLanguage.Portuguese;
        }
        catch
        {
            Language = AppLanguage.Portuguese;
        }
    }

    public static void Use(AppLanguage language, bool persist = true)
    {
        if (Language == language)
            return;

        Language = language;
        if (persist)
            Save();

        State.Bump();
        Changed?.Invoke();
    }

    public static void Toggle() =>
        Use(IsEnglish ? AppLanguage.Portuguese : AppLanguage.English);

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(
                Path.Combine(AppMeta.GuttyDir, "language.tag"),
                IsEnglish ? "en" : "pt");
        }
        catch
        {
        }
    }
}

internal sealed class UiLanguageState : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public int Revision { get; private set; }

    public void Bump()
    {
        Revision++;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Revision)));
    }
}
