using System.Text.RegularExpressions;
using Xunit;

namespace GuttyRL.Tests;

public class WpfBindingSafetyTests
{
    [Fact]
    public void TextBox_text_bindings_must_not_default_to_two_way()
    {
        string dir = FindDotnetDir();
        var failures = new List<string>();
        foreach (string path in Directory.GetFiles(dir, "*.xaml", SearchOption.TopDirectoryOnly))
        {
            string xaml = File.ReadAllText(path);
            foreach (Match box in Regex.Matches(xaml, @"<TextBox\b(?<attrs>[\s\S]*?)(?:/>|>)"))
            {
                Match bind = Regex.Match(box.Groups["attrs"].Value, @"Text=""\{Binding (?<b>[^}]+)\}""");
                if (!bind.Success)
                    continue;

                string expression = bind.Groups["b"].Value;
                if (expression.Contains("Mode=OneWay", StringComparison.Ordinal)
                    || expression.Contains("Mode=OneTime", StringComparison.Ordinal))
                    continue;

                failures.Add($"{Path.GetFileName(path)}: Text=\"{{Binding {expression}}}\"");
            }
        }

        Assert.True(
            failures.Count == 0,
            "TextBox.Text é TwoWay por padrão e crasha em propriedade só-leitura. Use Mode=OneWay.\n"
            + string.Join('\n', failures));
    }

    private static string FindDotnetDir()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            if (File.Exists(Path.Combine(dir, "UpdateToastWindow.xaml")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("UpdateToastWindow.xaml não encontrado a partir de " + AppContext.BaseDirectory);
    }
}
