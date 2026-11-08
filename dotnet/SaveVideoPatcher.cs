namespace GuttyRL;

/// <summary>
/// v25.0.31: o otimizador não grava mais .save. COMPLETO e CRIADOR ficam só no INI.
/// O método existe para os chamadores antigos não dispararem o Python.
/// </summary>
internal static class SaveVideoPatcher
{
    public static bool PatchSaveDirectory(string saveDir, string mode, Action<int, int, string>? progress = null)
    {
        AppMeta.Log("Patch de .save desligado (v25.0.31). Grafico e FPS ficam so no INI.");
        progress?.Invoke(1, 1, "INI");
        return true;
    }
}
