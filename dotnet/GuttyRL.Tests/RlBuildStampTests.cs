using Xunit;

namespace GuttyRL.Tests;

public class RlBuildStampTests
{
    [Fact]
    public void First_sight_does_not_reapply()
    {
        Assert.False(RlBuildStamp.ShouldReapply(null, "2.76.0.0"));
        Assert.False(RlBuildStamp.ShouldReapply("", "2.76.0.0"));
        Assert.False(RlBuildStamp.ShouldReapply("2.76.0.0", null));
    }

    [Fact]
    public void Same_build_does_not_reapply()
    {
        Assert.False(RlBuildStamp.ShouldReapply("2.76.0.0", "2.76.0.0"));
        Assert.False(RlBuildStamp.ShouldReapply(" 2.76.0.0 ", "2.76.0.0"));
    }

    [Fact]
    public void New_build_reapplies()
    {
        Assert.True(RlBuildStamp.ShouldReapply("2.75.0.0", "2.76.0.0"));
    }
}
