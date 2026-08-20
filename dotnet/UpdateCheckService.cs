using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;

namespace GuttyRL;

/// <summary>Consulta releases do GitHub e compara com a versão local.</summary>
internal static class UpdateCheckService
{
    private static readonly HttpClient Http = CreateClient();
    private static readonly object Gate = new();
    private static DateTime _lastCheckUtc = DateTime.MinValue;
    private static UpdateCheckResult? _lastResult;
    private static List<ChangelogRelease> _lastReleases = new();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GuttyTECH-RL-Optimizer/" + AppMeta.Version.TrimStart('v'));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");
        return client;
    }

    public static async Task<UpdateCheckResult> CheckLatestAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        lock (Gate)
        {
            if (!force
                && _lastResult is not null
                && DateTime.UtcNow - _lastCheckUtc < TimeSpan.FromMinutes(5))
            {
                return _lastResult;
            }
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, AppMeta.GitHubReleasesApi);
            using HttpResponseMessage response = await Http.SendAsync(request, cancellationToken);
            string body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Cache(new UpdateCheckResult(
                    false,
                    false,
                    AppMeta.Version,
                    null,
                    null,
                    AppMeta.GitHubReleasesPage,
                    null,
                    $"GitHub respondeu {(int)response.StatusCode}. Tente de novo em instantes."));
            }

            List<GitHubReleaseDto>? releases = JsonSerializer.Deserialize(
                body,
                AppJsonContext.Default.ListGitHubReleaseDto);
            if (releases is null || releases.Count == 0)
            {
                return Cache(new UpdateCheckResult(
                    false,
                    false,
                    AppMeta.Version,
                    null,
                    null,
                    AppMeta.GitHubReleasesPage,
                    null,
                    "Não consegui ler as releases no GitHub."));
            }

            List<ChangelogRelease> mapped = releases
                .Select(item => new ChangelogRelease(
                    item.TagName,
                    item.Name,
                    item.Body,
                    item.Draft,
                    item.Prerelease))
                .ToList();

            lock (Gate)
                _lastReleases = mapped;

            ChangelogRelease? newest = ChangelogRange.SelectRange(mapped, afterVersion: "0.0.0").FirstOrDefault();
            if (newest is null)
            {
                return Cache(new UpdateCheckResult(
                    false,
                    false,
                    AppMeta.Version,
                    null,
                    null,
                    AppMeta.GitHubReleasesPage,
                    null,
                    "Não consegui ler a release mais recente no GitHub."));
            }

            string latest = ChangelogRange.Normalize(newest.Tag);
            string current = ChangelogRange.Normalize(AppMeta.Version);
            bool newer = ChangelogRange.IsNewer(latest, current);

            GitHubReleaseDto? latestDto = releases.FirstOrDefault(item =>
                string.Equals(
                    ChangelogRange.Normalize(item.TagName),
                    latest,
                    StringComparison.OrdinalIgnoreCase));
            string? download = latestDto is null ? null : PickExeAsset(latestDto);

            IReadOnlyList<ChangelogRelease> missed = ChangelogRange.SelectRange(mapped, afterVersion: current);
            string notes = missed.Count > 0
                ? ChangelogRange.Format(missed)
                : ReleaseNotesFormatter.FormatForUi(newest.Body, newest.Tag, newest.Name);

            string message = newer
                ? $"Tem versão nova no GitHub: v{latest} (você está em v{current}). Abaixo está o que mudou desde a sua versão."
                : $"Você já está na última: v{current}.";

            return Cache(new UpdateCheckResult(
                true,
                newer,
                current,
                latest,
                newest.Name,
                string.IsNullOrWhiteSpace(latestDto?.HtmlUrl) ? AppMeta.GitHubReleasesPage : latestDto!.HtmlUrl,
                download,
                message,
                notes));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            AppMeta.Log("UPDATE-CHECK: " + ex.Message);
            return Cache(new UpdateCheckResult(
                false,
                false,
                AppMeta.Version,
                null,
                null,
                AppMeta.GitHubReleasesPage,
                null,
                "Sem conexão com o GitHub agora. Verifique a internet e tente ATUALIZAR de novo."));
        }
    }

    public static bool WasDismissed(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return false;

        try
        {
            if (!File.Exists(AppMeta.UpdateDismissedFile))
                return false;
            string saved = File.ReadAllText(AppMeta.UpdateDismissedFile).Trim();
            return string.Equals(NormalizeTag(saved), NormalizeTag(tag), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void Dismiss(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return;

        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(AppMeta.UpdateDismissedFile, NormalizeTag(tag));
        }
        catch (Exception ex)
        {
            AppMeta.Log("UPDATE-DISMISS: " + ex.Message);
        }
    }

    public static void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            AppMeta.Log("UPDATE-OPEN: " + ex.Message);
        }
    }

    public static async Task<string?> DownloadLatestAsync(
        string downloadUrl,
        string tag,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop) || !Directory.Exists(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string fileName = $"GuttyTECH_RL-{NormalizeTag(tag)}.exe";
        string dest = Path.Combine(desktop, fileName);
        string temp = dest + ".partial";

        progress?.Report("Baixando " + fileName + "…");
        using var response = await Http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using (Stream input = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (FileStream output = File.Create(temp))
        {
            await input.CopyToAsync(output, cancellationToken);
        }

        if (File.Exists(dest))
            File.Delete(dest);
        File.Move(temp, dest);

        AppMeta.Log("UPDATE-DOWNLOAD: " + dest);
        progress?.Report("Download concluído no Desktop");
        return dest;
    }

    private static UpdateCheckResult Cache(UpdateCheckResult result)
    {
        lock (Gate)
        {
            _lastCheckUtc = DateTime.UtcNow;
            _lastResult = result;
        }

        return result;
    }

    private static string? PickExeAsset(GitHubReleaseDto release)
    {
        if (release.Assets is null || release.Assets.Count == 0)
            return null;

        GitHubAssetDto? preferred = release.Assets.FirstOrDefault(a =>
            a.Name.Equals("GuttyTECH_RL.exe", StringComparison.OrdinalIgnoreCase));
        preferred ??= release.Assets.FirstOrDefault(a =>
            a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

        return preferred?.BrowserDownloadUrl;
    }

    internal static string FormatCachedRange(string afterVersion, string? untilVersion = null)
    {
        List<ChangelogRelease> snapshot;
        lock (Gate)
            snapshot = _lastReleases.ToList();

        return ChangelogRange.Format(ChangelogRange.SelectRange(snapshot, afterVersion, untilVersion));
    }

    internal static string NormalizeTag(string tag) => ChangelogRange.Normalize(tag);

    internal static bool IsNewer(string latest, string current) => ChangelogRange.IsNewer(latest, current);
}
