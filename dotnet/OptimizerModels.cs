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
    bool ConfigExists);

internal sealed record OperationProgress(int Percentage, string Message);

internal sealed record OperationResult(
    bool Success,
    bool IsNoOp,
    FeedbackTone Tone,
    string Title,
    string Message);
