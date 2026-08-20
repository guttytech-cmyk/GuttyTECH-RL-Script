using Xunit;

namespace GuttyRL.Tests;

public class UpdateCheckBusyStateTests
{
    [Fact]
    public void Button_switches_to_searching_copy()
    {
        Assert.Equal("ATUALIZAR", UpdateCheckBusyState.ButtonLabel(false));
        Assert.Equal("BUSCANDO…", UpdateCheckBusyState.ButtonLabel(true));
    }

    [Fact]
    public void Background_refresh_skips_when_already_in_flight()
    {
        Assert.True(UpdateCheckBusyState.ShouldSkip(
            isBusy: false,
            isCheckingUpdates: false,
            refreshInFlight: true,
            userRequestedUpdateCheck: false));
    }

    [Fact]
    public void User_click_is_not_swallowed_by_background_refresh()
    {
        Assert.False(UpdateCheckBusyState.ShouldSkip(
            isBusy: false,
            isCheckingUpdates: false,
            refreshInFlight: true,
            userRequestedUpdateCheck: true));
    }

    [Fact]
    public void Busy_or_already_checking_always_skips()
    {
        Assert.True(UpdateCheckBusyState.ShouldSkip(
            isBusy: true,
            isCheckingUpdates: false,
            refreshInFlight: false,
            userRequestedUpdateCheck: true));
        Assert.True(UpdateCheckBusyState.ShouldSkip(
            isBusy: false,
            isCheckingUpdates: true,
            refreshInFlight: false,
            userRequestedUpdateCheck: true));
    }

    [Fact]
    public void Searching_copy_is_pt_br_and_says_github()
    {
        Assert.Contains("GitHub", UpdateCheckBusyState.OverlayMessage, StringComparison.Ordinal);
        Assert.Contains("atualiza", UpdateCheckBusyState.OverlayTitle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GitHub", UpdateCheckBusyState.FooterChecking, StringComparison.Ordinal);
        Assert.DoesNotContain("ligação", UpdateCheckBusyState.OverlayMessage, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Verifica ", UpdateCheckBusyState.OverlayMessage, StringComparison.Ordinal);
    }
}
