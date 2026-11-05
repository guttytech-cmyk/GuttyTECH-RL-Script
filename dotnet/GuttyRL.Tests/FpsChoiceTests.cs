using Xunit;

namespace GuttyRL.Tests;

public class FpsChoiceTests
{
    [Theory]
    [InlineData("False", "360", 360)]
    [InlineData("false", "360.000000", 360)]
    [InlineData("False", "144", 144)]
    [InlineData("False", "240.0", 240)]
    [InlineData("True", "360", 360)]
    [InlineData("True", "144", 144)]
    [InlineData("True", "240.0", 240)]
    public void Limite_do_menu_e_respeitado(string uncapped, string custom, int esperado)
    {
        Assert.True(FpsChoice.IsUserCap(uncapped, custom, out int cap));
        Assert.Equal(esperado, cap);
    }

    [Theory]
    [InlineData("True", "0")]
    [InlineData("False", "0")]
    [InlineData("False", "60")]
    [InlineData("False", "10000")]
    [InlineData("False", "")]
    [InlineData(null, null)]
    [InlineData(null, "60")]
    public void Padrao_do_jogo_nao_conta_como_limite(string? uncapped, string? custom)
    {
        Assert.False(FpsChoice.IsUserCap(uncapped, custom, out _));
    }

    [Fact]
    public void Stamp_troca_so_o_fps_e_mantem_o_resto()
    {
        string ini = "[SystemSettings]\r\nUncappedFramerate=True\r\nCustomFPS=0\r\nUseVsync=False\r\n"
                     + "[SystemSettingsMobile]\r\nUncappedFramerate=True\r\nCustomFPS=0\r\n";
        string outIni = FpsChoice.Stamp(ini, 360);
        Assert.Contains("UncappedFramerate=False", outIni);
        Assert.Contains("CustomFPS=360", outIni);
        Assert.Contains("UseVsync=False", outIni);
        Assert.DoesNotContain("UncappedFramerate=True", outIni);
        Assert.DoesNotContain("CustomFPS=0", outIni);
        Assert.True(FpsChoice.TryRead(outIni, out int cap));
        Assert.Equal(360, cap);
    }
}
