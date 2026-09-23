using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace GuttyRL;

/// <summary>Texto localizado que atualiza quando o idioma muda. Uso: {ui:Tx chave}</summary>
public sealed class TxExtension : MarkupExtension
{
    public TxExtension()
    {
    }

    public TxExtension(string key) => Key = key;

    public string Key { get; set; } = "";

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding(nameof(UiLanguageState.Revision))
        {
            Source = UiLanguage.State,
            Mode = BindingMode.OneWay,
            Converter = TxConverter.Instance,
            ConverterParameter = Key,
        };
        return binding.ProvideValue(serviceProvider);
    }
}

internal sealed class TxConverter : IValueConverter
{
    public static readonly TxConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        UiCopy.Get(parameter as string ?? "");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
