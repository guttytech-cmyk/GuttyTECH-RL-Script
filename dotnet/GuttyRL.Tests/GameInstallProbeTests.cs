using Xunit;

namespace GuttyRL.Tests;

public class GameInstallProbeTests
{
    [Fact]
    public void ParseEpicItem_reads_install_location_and_incomplete_flag()
    {
        const string json = """
            {
              "AppName": "Sugar",
              "DisplayName": "Rocket League®",
              "InstallLocation": "D:\\Users\\Otoniel\\Desktop\\JOSIEL\\JOGO\\Rocket League",
              "bIsIncompleteInstall": true
            }
            """;

        Assert.True(GameInstallProbe.LooksLikeRocketLeague(json));
        Assert.True(GameInstallProbe.IsIncompleteInstall(json));
        Assert.Equal(
            @"D:\Users\Otoniel\Desktop\JOSIEL\JOGO\Rocket League",
            GameInstallProbe.ParseInstallLocation(json));
    }

    [Fact]
    public void ParseSteamLibraryPaths_unescapes_vdf_paths()
    {
        const string vdf = """
            "libraryfolders"
            {
            	"0"
            	{
            		"path"		"C:\\Program Files (x86)\\Steam"
            		"apps" { "252950"		"1" }
            	}
            	"1"
            	{
            		"path"		"D:\\Games"
            	}
            }
            """;

        IReadOnlyList<string> paths = GameInstallProbe.ParseSteamLibraryPaths(vdf);
        Assert.Contains(@"C:\Program Files (x86)\Steam", paths);
        Assert.Contains(@"D:\Games", paths);
    }

    [Fact]
    public void Scan_finds_eac_next_to_old_gutty_exe_in_custom_folder()
    {
        string root = @"D:\Users\Otoniel\Desktop\JOSIEL\JOGO\Rocket League";
        string exe = Path.Combine(root, "GuttyTECH_RL.exe");
        FakeFs fs = FakeFs.WithGame(root, hasExe: true, hasEac: true);

        GameInstallProbe.Report report = GameInstallProbe.Scan(fs.ToFs(), exe, extraRoots: []);

        Assert.Equal(GameInstallProbe.InstallVerdict.Ok, report.Verdict);
        Assert.Equal(GameInstallProbe.EacSetupPath(root), report.EacSetupPath);
        Assert.Equal(GameInstallProbe.RocketLeagueExePath(root), report.RocketLeagueExe);
    }

    [Fact]
    public void Scan_marks_missing_when_epic_points_to_gone_folder()
    {
        const string manifestDir = @"C:\ProgramData\Epic\EpicGamesLauncher\Data\Manifests";
        const string item = manifestDir + @"\sugar.item";
        const string loc = @"D:\Games\rocketleague";
        var fs = new FakeFs();
        fs.Dirs.Add(manifestDir);
        fs.Texts[item] = """
            { "AppName": "Sugar", "InstallLocation": "D:\\Games\\rocketleague", "bIsIncompleteInstall": false }
            """;

        GameInstallProbe.Report report = GameInstallProbe.Scan(fs.ToFs(), processPath: null, extraRoots: []);

        Assert.Equal(GameInstallProbe.InstallVerdict.Missing, report.Verdict);
        Assert.Null(report.EacSetupPath);
        Assert.Contains(report.Roots, r => r.Path == loc && !r.DirectoryExists);
        Assert.Contains(report.Notes, n => n.Contains("Sugar", StringComparison.OrdinalIgnoreCase)
                                           || n.Contains(loc, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Scan_marks_incomplete_when_exe_exists_but_eac_setup_is_gone()
    {
        string root = @"C:\Program Files\Epic Games\rocketleague";
        FakeFs fs = FakeFs.WithGame(root, hasExe: true, hasEac: false);

        GameInstallProbe.Report report = GameInstallProbe.Scan(fs.ToFs(), processPath: null, extraRoots: [root]);

        Assert.Equal(GameInstallProbe.InstallVerdict.Incomplete, report.Verdict);
        Assert.NotNull(report.RocketLeagueExe);
        Assert.Null(report.EacSetupPath);
    }

    [Fact]
    public void SuggestedAction_for_missing_install_is_verify_files_not_save_repair()
    {
        var report = new GameInstallProbe.Report(
            GameInstallProbe.InstallVerdict.Missing,
            RocketLeagueExe: null,
            EacSetupPath: null,
            Roots: [],
            Notes: ["epic loc ausente"]);

        string action = GameInstallProbe.SuggestedAction(report);
        Assert.Contains("Verificar arquivos", action, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Corrigir Save", action, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Nao use Recuperar Boot", action, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormatText_includes_every_candidate_and_verdict()
    {
        var report = new GameInstallProbe.Report(
            GameInstallProbe.InstallVerdict.Missing,
            RocketLeagueExe: null,
            EacSetupPath: null,
            Roots:
            [
                new GameInstallProbe.RootHit(
                    @"D:\Users\Otoniel\JOGO\Rocket League",
                    "exe-parent",
                    DirectoryExists: false,
                    HasRocketLeagueExe: false,
                    HasEacSetup: false),
            ],
            Notes: ["manifest InstallLocation inexistente"]);

        string text = GameInstallProbe.FormatText(report);
        Assert.Contains("NAO_INSTALADO", text);
        Assert.Contains("exe-parent", text);
        Assert.Contains(@"D:\Users\Otoniel\JOGO\Rocket League", text);
        Assert.Contains("manifest InstallLocation inexistente", text);
    }

    private sealed class FakeFs
    {
        public HashSet<string> Dirs { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> Texts { get; } = new(StringComparer.OrdinalIgnoreCase);

        public GameInstallProbe.Fs ToFs() => new(
            Dirs.Contains,
            Files.Contains,
            p => Texts.TryGetValue(p, out string? t) ? t : null,
            (dir, pattern) =>
            {
                string ext = pattern.StartsWith("*.", StringComparison.Ordinal) ? pattern[1..] : pattern;
                return Texts.Keys.Where(k =>
                    k.StartsWith(dir.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)
                    && (pattern == "*" || k.EndsWith(ext, StringComparison.OrdinalIgnoreCase)));
            });

        public static FakeFs WithGame(string root, bool hasExe, bool hasEac)
        {
            var fs = new FakeFs();
            fs.Dirs.Add(root);
            fs.Dirs.Add(Path.Combine(root, "Binaries", "Win64"));
            if (hasExe)
                fs.Files.Add(GameInstallProbe.RocketLeagueExePath(root));
            if (hasEac)
            {
                fs.Dirs.Add(Path.Combine(root, "Binaries", "Win64", "EasyAntiCheat"));
                fs.Files.Add(GameInstallProbe.EacSetupPath(root));
            }

            return fs;
        }
    }
}
