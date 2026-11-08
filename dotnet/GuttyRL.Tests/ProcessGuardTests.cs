using Xunit;

namespace GuttyRL.Tests;

public class ProcessGuardTests
{
    [Fact]
    public void Nao_mata_o_proprio_processo()
    {
        Assert.False(ProcessGuard.ShouldStop(10, 10, "GuttyTECH_RL", null));
    }

    [Fact]
    public void Mata_outro_GuttyTECH_RL()
    {
        Assert.True(ProcessGuard.ShouldStop(20, 10, "GuttyTECH_RL", null));
        Assert.True(ProcessGuard.ShouldStop(21, 10, "GuttyRL", null));
    }

    [Fact]
    public void Nao_mata_python_do_sistema()
    {
        Assert.False(ProcessGuard.ShouldStop(30, 10, "python", @"C:\Python311\python.exe"));
    }

    [Fact]
    public void Mata_python_da_pasta_do_otimizador()
    {
        Assert.True(ProcessGuard.ShouldStop(
            31, 10, "python",
            @"C:\Users\tiago\GuttyTECH\RL-Optimizer-v22\runtime\25.0.30\py311\python.exe"));
    }
}
