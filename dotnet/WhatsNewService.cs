namespace GuttyRL;

/// <summary>Persiste e mostra o changelog apos atualizar (estilo Discord).</summary>
internal static class WhatsNewService
{
    private static string ShownFile => Path.Combine(AppMeta.GuttyDir, "whatsnew-shown.tag");
    private static string PendingFile => Path.Combine(AppMeta.GuttyDir, "pending-whatsnew.txt");

    public static void SavePending(string tag, string notes)
    {
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            string payload = UpdateCheckService.NormalizeTag(tag) + "\n---\n" + notes.Trim();
            File.WriteAllText(PendingFile, payload);
        }
        catch (Exception ex)
        {
            AppMeta.Log("WHATSNEW-SAVE: " + ex.Message);
        }
    }

    public static string? ReadShownVersion()
    {
        try
        {
            if (!File.Exists(ShownFile))
                return null;
            string saved = File.ReadAllText(ShownFile).Trim();
            return saved.Length == 0 ? null : saved;
        }
        catch
        {
            return null;
        }
    }

    public static bool WasShown(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return false;
        try
        {
            if (!File.Exists(ShownFile)) return false;
            string saved = File.ReadAllText(ShownFile).Trim();
            return string.Equals(
                UpdateCheckService.NormalizeTag(saved),
                UpdateCheckService.NormalizeTag(version),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static void MarkShown(string? version)
    {
        if (string.IsNullOrWhiteSpace(version)) return;
        try
        {
            Directory.CreateDirectory(AppMeta.GuttyDir);
            File.WriteAllText(ShownFile, UpdateCheckService.NormalizeTag(version));
            try { if (File.Exists(PendingFile)) File.Delete(PendingFile); } catch { }
        }
        catch (Exception ex)
        {
            AppMeta.Log("WHATSNEW-MARK: " + ex.Message);
        }
    }

    /// <summary>Na 1a abertura da versao nova: mostra changelog (pending ou GitHub).</summary>
    public static async Task TryShowOnStartupAsync()
    {
        string current = AppMeta.Version;
        if (WasShown(current))
            return;

        string? notes = null;
        string tag = current;

        try
        {
            if (File.Exists(PendingFile))
            {
                string raw = File.ReadAllText(PendingFile);
                int sep = raw.IndexOf("\n---\n", StringComparison.Ordinal);
                if (sep > 0)
                {
                    string pendingTag = raw[..sep].Trim();
                    string body = raw[(sep + 5)..].Trim();
                    if (string.Equals(
                            UpdateCheckService.NormalizeTag(pendingTag),
                            UpdateCheckService.NormalizeTag(current),
                            StringComparison.OrdinalIgnoreCase)
                        && body.Length > 0)
                    {
                        notes = body;
                        tag = pendingTag;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AppMeta.Log("WHATSNEW-PENDING: " + ex.Message);
        }

        if (string.IsNullOrWhiteSpace(notes))
        {
            UpdateCheckResult update = await UpdateCheckService.CheckLatestAsync(force: false);
            string? since = ReadShownVersion();
            if (!string.IsNullOrWhiteSpace(since)
                && !string.Equals(
                    UpdateCheckService.NormalizeTag(since),
                    UpdateCheckService.NormalizeTag(current),
                    StringComparison.OrdinalIgnoreCase))
            {
                string ranged = UpdateCheckService.FormatCachedRange(since, current);
                if (!string.IsNullOrWhiteSpace(ranged))
                {
                    notes = ranged;
                    tag = current;
                }
            }

            if (string.IsNullOrWhiteSpace(notes)
                && update.Success
                && !string.IsNullOrWhiteSpace(update.ReleaseNotes)
                && string.Equals(
                    UpdateCheckService.NormalizeTag(update.LatestTag ?? ""),
                    UpdateCheckService.NormalizeTag(current),
                    StringComparison.OrdinalIgnoreCase))
            {
                notes = update.ReleaseNotes;
                tag = update.LatestTag ?? current;
            }
            else if (string.IsNullOrWhiteSpace(notes))
            {
                // So marca como visto se ja estamos na ultima e nao ha notas —
                // se ainda ha update pendente, nao marca (mostra apos atualizar).
                if (update.Success && !update.UpdateAvailable)
                    MarkShown(current);
                return;
            }
        }

        string? sinceLabel = ReadShownVersion();
        string subtitle = !string.IsNullOrWhiteSpace(sinceLabel)
            && !string.Equals(
                UpdateCheckService.NormalizeTag(sinceLabel),
                UpdateCheckService.NormalizeTag(current),
                StringComparison.OrdinalIgnoreCase)
            ? "O que mudou desde a v" + UpdateCheckService.NormalizeTag(sinceLabel) + ":"
            : "Atualizou? Aqui está o que mudou nas versões novas.";

        ChangelogWindow.Show(
            tag,
            notes,
            subtitle: subtitle,
            markShownVersion: current);
    }
}
