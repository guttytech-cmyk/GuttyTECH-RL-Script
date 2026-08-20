namespace GuttyRL;

/// <summary>Copy e regras do clique ATUALIZAR — o botão não pode parecer morto enquanto fala com o GitHub.</summary>
internal static class UpdateCheckBusyState
{
    public const string OverlayTitle = "BUSCANDO ATUALIZAÇÕES";
    public const string OverlayMessage =
        "Consultando o GitHub pela versão mais recente. O botão pulsa até responder.";
    public const string FooterChecking = "Procurando versão nova no GitHub…";

    public static string ButtonLabel(bool isChecking) =>
        isChecking ? "BUSCANDO…" : "ATUALIZAR";

    public static bool ShouldSkip(
        bool isBusy,
        bool isCheckingUpdates,
        bool refreshInFlight,
        bool userRequestedUpdateCheck)
    {
        if (isBusy || isCheckingUpdates)
            return true;

        if (refreshInFlight && !userRequestedUpdateCheck)
            return true;

        return false;
    }
}
