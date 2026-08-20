using Xunit;

namespace GuttyRL.Tests;

public class ChangelogRangeTests
{
    private static readonly ChangelogRelease[] Sample =
    [
        new("v25.0.17", "v25.0.17 — popup", "## Corrigido\n- toast TwoWay\n\n## Como atualizar\n1. Baixe o exe"),
        new("v25.0.16", "v25.0.16 — buscar", "## Melhorado\n- botão BUSCANDO"),
        new("v25.0.15", "v25.0.15 — dll", "## Corrigido\n- erro falso de DLL"),
        new("v25.0.10", "v25.0.10 — changelog", "## Novo\n- popup Discord"),
        new("v25.0.9", "v25.0.9 — old", "## Novo\n- coisa antiga"),
    ];

    [Fact]
    public void From_v10_includes_every_newer_version_and_skips_current()
    {
        var selected = ChangelogRange.SelectRange(Sample, afterVersion: "25.0.10");
        Assert.Equal(["25.0.17", "25.0.16", "25.0.15"], selected.Select(r => ChangelogRange.Normalize(r.Tag)).ToArray());
    }

    [Fact]
    public void Startup_range_stops_at_the_installed_version()
    {
        var selected = ChangelogRange.SelectRange(Sample, afterVersion: "25.0.10", untilVersion: "25.0.16");
        Assert.Equal(["25.0.16", "25.0.15"], selected.Select(r => ChangelogRange.Normalize(r.Tag)).ToArray());
    }

    [Fact]
    public void Already_on_latest_selects_nothing()
    {
        Assert.Empty(ChangelogRange.SelectRange(Sample, afterVersion: "25.0.17"));
    }

    [Fact]
    public void Format_lists_newest_first_with_version_headers()
    {
        var selected = ChangelogRange.SelectRange(Sample, afterVersion: "25.0.10");
        string text = ChangelogRange.Format(selected);

        int i17 = text.IndexOf("v25.0.17", StringComparison.Ordinal);
        int i16 = text.IndexOf("v25.0.16", StringComparison.Ordinal);
        int i15 = text.IndexOf("v25.0.15", StringComparison.Ordinal);
        Assert.True(i17 >= 0 && i16 > i17 && i15 > i16, text);
        Assert.Contains("toast TwoWay", text, StringComparison.Ordinal);
        Assert.Contains("botão BUSCANDO", text, StringComparison.Ordinal);
        Assert.DoesNotContain("popup Discord", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Format_skips_como_atualizar_blocks()
    {
        var selected = ChangelogRange.SelectRange(Sample, afterVersion: "25.0.16");
        string text = ChangelogRange.Format(selected);
        Assert.Contains("toast TwoWay", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Baixe o exe", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Como atualizar", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Skips_drafts_and_prereleases()
    {
        ChangelogRelease[] mixed =
        [
            new("v25.0.18", "draft", "- secret", Draft: true),
            new("v25.0.17-beta", "pre", "- beta", Prerelease: true),
            new("v25.0.16", "ok", "- real"),
        ];
        var selected = ChangelogRange.SelectRange(mixed, afterVersion: "25.0.10");
        Assert.Single(selected);
        Assert.Equal("25.0.16", ChangelogRange.Normalize(selected[0].Tag));
    }

    [Fact]
    public void BuildCards_marks_newest_and_splits_sections()
    {
        var selected = ChangelogRange.SelectRange(Sample, afterVersion: "25.0.10");
        IReadOnlyList<ChangelogVersionCard> cards = ChangelogRange.BuildCards(selected);

        Assert.Equal(3, cards.Count);
        Assert.Equal("v25.0.17", cards[0].VersionLabel);
        Assert.True(cards[0].IsLatest);
        Assert.False(cards[1].IsLatest);
        Assert.Contains(cards[0].Sections.SelectMany(s => s.Lines), line => line.Contains("toast TwoWay", StringComparison.Ordinal));
        Assert.Contains(cards[1].Sections.SelectMany(s => s.Lines), line => line.Contains("BUSCANDO", StringComparison.Ordinal));
        Assert.DoesNotContain(
            cards.SelectMany(c => c.Sections).SelectMany(s => s.Lines),
            line => line.Contains("Baixe o exe", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ParseCards_rebuilds_structure_from_formatted_notes()
    {
        var selected = ChangelogRange.SelectRange(Sample, afterVersion: "25.0.10");
        string text = ChangelogRange.Format(selected);
        IReadOnlyList<ChangelogVersionCard> cards = ChangelogRange.ParseCards(text);

        Assert.Equal(["v25.0.17", "v25.0.16", "v25.0.15"], cards.Select(c => c.VersionLabel).ToArray());
        Assert.True(cards[0].IsLatest);
        Assert.Contains(cards[0].Sections.SelectMany(s => s.Lines), line => line.Contains("toast TwoWay", StringComparison.Ordinal));
    }
}
