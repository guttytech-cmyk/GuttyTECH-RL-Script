using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GuttyRL;

/// <summary>Consulta releases do GitHub e compara com a versão local.</summary>
internal static class UpdateCheckService
{
    private static readonly HttpClient Http = CreateClient();
    private static readonly object Gate = new();
    private static DateTime _lastCheckUtc = DateTime.MinValue;
    private static UpdateCheckResult? _lastResult;

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
            using var request = new HttpRequestMessage(HttpMethod.Get, AppMeta.GitHubReleasesLatestApi);
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
                    $"GitHub respondeu {(int)response.StatusCode}. Tenta de novo em instantes."));
            }

            GitHubReleaseDto? release = JsonSerializer.Deserialize(body, AppJsonContext.Default.GitHubReleaseDto);
            if (release is null || string.IsNullOrWhiteSpace(release.TagName))
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

            string latest = NormalizeTag(release.TagName);
            string current = NormalizeTag(AppMeta.Version);
            bool newer = IsNewer(latest, current);
            string? download = PickExeAsset(release);

            string message = newer
                ? $"Tem versão nova no GitHub: {latest} (você está em {current})."
                : $"Você já está na última: {current}.";

            return Cache(new UpdateCheckResult(
                true,
                newer,
                current,
                latest,
                release.Name,
                string.IsNullOrWhiteSpace(release.HtmlUrl) ? AppMeta.GitHubReleasesPage : release.HtmlUrl,
                download,
                message));
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
                "Sem ligação ao GitHub agora. Verifica a internet e tenta ATUALIZAR de novo."));
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

    internal static string NormalizeTag(string tag)
    {
        tag = tag.Trim();
        if (tag.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            tag = tag[1..];
        return tag;
    }

    internal static bool IsNewer(string latest, string current)
    {
        if (TryParseVersion(latest, out Version latestV) && TryParseVersion(current, out Version currentV))
            return latestV > currentV;

        return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseVersion(string text, out Version version)
    {
        Match match = Regex.Match(text, @"^\d+(\.\d+){0,3}");
        if (match.Success && Version.TryParse(match.Value, out version!))
            return true;

        version = new Version(0, 0);
        return false;
    }
}
