using Xunit;

namespace GuttyRL.Tests;

/// <summary>
/// Caso MENĐONÇA: conta <c>gusta</c> com RL no OneDrive + perfil antigo
/// <c>Gustavo</c> com Documentos local. O v25.0.13 preferia o perfil errado,
/// pinava esse INI e o restore nunca migrava o OneDrive do jogo real.
/// </summary>
public class RlPathPolicyTests
{
    private const string Gusta = @"C:\Users\gusta";
    private const string Gustavo = @"C:\Users\Gustavo";

    private static readonly string GustaOneDriveIni =
        Gusta + @"\OneDrive\Documentos\My Games\Rocket League\TAGame\Config\TASystemSettings.ini";

    private static readonly string GustaLocalIni =
        Gusta + @"\Documents\My Games\Rocket League\TAGame\Config\TASystemSettings.ini";

    private static readonly string GustavoLocalIni =
        Gustavo + @"\Documents\My Games\Rocket League\TAGame\Config\TASystemSettings.ini";

    [Fact]
    public void Does_not_scan_other_profiles_when_current_user_has_onedrive_rl()
    {
        Assert.False(RlPathPolicy.ShouldScanOtherProfiles([GustaOneDriveIni]));
    }

    [Fact]
    public void Scans_other_profiles_only_when_current_user_has_no_rl()
    {
        Assert.True(RlPathPolicy.ShouldScanOtherProfiles([]));
        Assert.True(RlPathPolicy.ShouldScanOtherProfiles([""]));
    }

    [Fact]
    public void Rank_prefers_current_onedrive_over_foreign_local_documents()
    {
        IReadOnlyList<string> ranked = RlPathPolicy.RankInis(
            currentUserInis: [GustaOneDriveIni],
            otherProfileInis: [GustavoLocalIni],
            userProfile: Gusta);

        Assert.Equal(GustaOneDriveIni, ranked[0]);
        Assert.DoesNotContain(GustavoLocalIni, ranked);
    }

    [Fact]
    public void Rank_prefers_current_local_documents_over_onedrive()
    {
        IReadOnlyList<string> ranked = RlPathPolicy.RankInis(
            currentUserInis: [GustaOneDriveIni, GustaLocalIni],
            otherProfileInis: [GustavoLocalIni],
            userProfile: Gusta);

        Assert.Equal(GustaLocalIni, ranked[0]);
    }

    [Fact]
    public void Pin_from_foreign_profile_is_unusable()
    {
        Assert.False(RlPathPolicy.IsPinnedUsable(GustavoLocalIni, Gusta));
    }

    [Fact]
    public void Pin_from_onedrive_is_unusable()
    {
        Assert.False(RlPathPolicy.IsPinnedUsable(GustaOneDriveIni, Gusta));
    }

    [Fact]
    public void Pin_from_current_local_documents_is_usable()
    {
        Assert.True(RlPathPolicy.IsPinnedUsable(GustaLocalIni, Gusta));
    }

    [Fact]
    public void Relocate_only_current_user_onedrive_not_foreign_profile()
    {
        Assert.True(RlPathPolicy.NeedsRelocation(GustaOneDriveIni, Gusta));
        Assert.False(RlPathPolicy.NeedsRelocation(GustavoLocalIni, Gusta));
        Assert.False(RlPathPolicy.NeedsRelocation(GustaLocalIni, Gusta));
        Assert.False(RlPathPolicy.CanRelocateOffOneDrive(GustavoLocalIni, Gusta));
        Assert.False(RlPathPolicy.CanRelocateOffOneDrive(null, Gusta));
        Assert.True(RlPathPolicy.CanRelocateOffOneDrive(GustaOneDriveIni, Gusta));
    }

    [Fact]
    public void Normal_documents_user_is_left_alone()
    {
        const string ziel = @"C:\Users\User\Documents\My Games\Rocket League\TAGame\Config\TASystemSettings.ini";
        Assert.True(RlPathPolicy.IsPinnedUsable(ziel, @"C:\Users\User"));
        Assert.False(RlPathPolicy.NeedsRelocation(ziel, @"C:\Users\User"));
        Assert.False(RlPathPolicy.CanRelocateOffOneDrive(ziel, @"C:\Users\User"));
        Assert.Equal(ziel, RlPathPolicy.RankInis([ziel], [GustavoLocalIni], @"C:\Users\User")[0]);
    }

    [Fact]
    public void Copy_source_is_current_onedrive_not_foreign_profile()
    {
        string? src = RlPathPolicy.ChooseCopySourceIni(
            chosenIni: GustavoLocalIni,
            currentUserInis: [GustaOneDriveIni],
            userProfile: Gusta);

        Assert.Equal(GustaOneDriveIni, src);
    }

    [Fact]
    public void Copy_source_is_null_when_only_foreign_profile_exists()
    {
        string? src = RlPathPolicy.ChooseCopySourceIni(
            chosenIni: GustavoLocalIni,
            currentUserInis: [],
            userProfile: Gusta);

        Assert.Null(src);
    }

    [Fact]
    public void Local_documents_skips_onedrive_and_foreign_profiles()
    {
        string picked = RlPathPolicy.PickLocalDocuments(
            Gusta,
            [
                Gusta + @"\OneDrive\Documentos",
                Gustavo + @"\Documents",
                Gusta + @"\Documents",
            ]);

        Assert.Equal(Gusta + @"\Documents", picked);
    }

    [Fact]
    public void Local_documents_falls_back_to_profile_documents()
    {
        string picked = RlPathPolicy.PickLocalDocuments(
            Gusta,
            [Gusta + @"\OneDrive\Documentos", Gustavo + @"\Documents"]);

        Assert.Equal(Gusta + @"\Documents", picked);
        Assert.False(RlPathPolicy.IsOneDrivePath(picked));
        Assert.True(RlPathPolicy.IsUnderProfile(picked, Gusta));
    }

    [Fact]
    public void Gusta_is_not_treated_as_prefix_of_gustavo()
    {
        Assert.False(RlPathPolicy.IsUnderProfile(GustavoLocalIni, Gusta));
        Assert.False(RlPathPolicy.IsUnderProfile(GustaLocalIni, Gustavo));
    }
}
