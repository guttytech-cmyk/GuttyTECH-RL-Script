using Xunit;

namespace GuttyRL.Tests;

public class UiCopyTests
{
    [Fact]
    public void Catalog_has_portuguese_and_english_for_every_key()
    {
        IReadOnlyList<string> bad = UiCopy.Audit();
        Assert.True(bad.Count == 0, string.Join(", ", bad));
    }

    [Fact]
    public void Default_is_portuguese_and_english_roundtrips()
    {
        AppLanguage previous = UiLanguage.Language;
        try
        {
            UiLanguage.Use(AppLanguage.Portuguese, persist: false);
            Assert.Equal("Visão Geral", UiCopy.Get("nav.overview"));
            Assert.Equal("ATUALIZAR", UiCopy.Get("btn.refresh"));

            UiLanguage.Use(AppLanguage.English, persist: false);
            Assert.Equal("Overview", UiCopy.Get("nav.overview"));
            Assert.Equal("REFRESH", UiCopy.Get("btn.refresh"));
            Assert.Equal("Account 1/4", UiCopy.Format("prog.account", 1, 4));
        }
        finally
        {
            UiLanguage.Use(previous, persist: false);
        }
    }

    [Fact]
    public void Every_xaml_tx_key_exists_in_catalog_and_panel_is_single_page()
    {
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            if (File.Exists(Path.Combine(dir, "OptimizerPanel.xaml")))
                break;
            dir = Path.GetDirectoryName(dir);
        }

        Assert.NotNull(dir);
        var missing = new List<string>();
        foreach (string path in Directory.GetFiles(dir!, "*.xaml", SearchOption.TopDirectoryOnly))
        {
            string xaml = File.ReadAllText(path);
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(xaml, @"\{ui:Tx\s+(?<k>[^\s\}]+)\s*\}"))
            {
                string key = m.Groups["k"].Value;
                if (!UiCopy.HasKey(key))
                    missing.Add($"{Path.GetFileName(path)}: {key}");
            }
        }

        Assert.True(missing.Count == 0, "Chaves {ui:Tx} ausentes no catálogo:\n" + string.Join('\n', missing));

        string panelXaml = File.ReadAllText(Path.Combine(dir!, "OptimizerPanel.xaml"));
        Assert.DoesNotContain("IsDashboardVisible", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsOptimizationVisible", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsRecoveryVisible", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsSystemVisible", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsProtocolOpen", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsRecoveryOpen", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsLaunchOpen", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("IsSupportOpen", panelXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("ToggleProtocolCommand", panelXaml, StringComparison.Ordinal);
    }
}
