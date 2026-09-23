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
}
