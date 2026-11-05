using Xunit;

namespace GuttyRL.Tests;

public class IniAuditTests
{
    [Fact]
    public void Audit_dos_templates_passa()
    {
        Assert.Equal(0, IniAudit.Run());
    }

    [Fact]
    public void Completo_mantem_hdr_e_pintura_do_carro()
    {
        string ini = CompletoForce.Apply(Templates.Completo);
        Assert.False(CompletoForce.HasDrift(ini));
        Assert.Contains("FloatingPointRenderTargets=True", ini);
        Assert.Contains("bAllowHighQualityMaterials=True", ini);
        Assert.Contains("SHSecondaryLighting=True", ini);
        Assert.Contains("DetailMode=2", ini);
    }
}
