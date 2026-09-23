namespace GuttyRL;

internal enum OptimizerAction
{
    Completo,
    Criador,
    Remover,
    CopiarComando,
    CorrigirPermissoes,
    RepararPerfil,
    RecuperarBoot,
    Diagnostico,
    CorrigirTudo,
    RestaurarPresets,
    CorrigirSave,
    RepararEac,
}

internal enum OptimizerSection
{
    Dashboard,
    Otimizacao,
    Recuperacao,
    Sistema,
}

internal enum FeedbackTone
{
    Success,
    Warning,
    Error,
}

internal sealed record OptimizerStatus(
    string AppliedMode,
    string StateLabel,
    bool IsWritable,
    bool IsRocketLeagueOpen,
    string ConfigPath,
    bool IsProtected,
    bool IsAdministrator,
    bool ConfigExists,
    bool IsWatcherActive = false);

internal sealed record OperationProgress(
    int Percentage,
    string Message,
    string? Detail = null);

internal sealed record OperationResult(
    bool Success,
    bool IsNoOp,
    FeedbackTone Tone,
    string TitleKey,
    string MessageKey,
    string Arg0 = "",
    string Arg1 = "",
    string Arg2 = "")
{
    public string Title => UiCopy.Get(TitleKey);

    public string Message =>
        Arg0.Length == 0 && Arg1.Length == 0 && Arg2.Length == 0
            ? UiCopy.Get(MessageKey)
            : UiCopy.Format(MessageKey, Arg0, Arg1, Arg2);
}
