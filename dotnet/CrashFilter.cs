namespace GuttyRL;

/// <summary>
/// WPF + single-file: ao fechar, a CRT nativa descarrega e dispara DllNotFoundException
/// em __scrt_uninitialize_type_info. Nao e VC++ faltando — o app ja fechou.
/// </summary>
internal static class CrashFilter
{
    public static bool IsHarmlessShutdownNativeUnload(Exception? ex) =>
        IsHarmlessShutdownNativeUnload(ex, Environment.HasShutdownStarted, ex?.StackTrace);

    public static bool IsHarmlessShutdownNativeUnload(
        Exception? ex,
        bool shutdownStarted,
        string? stackTrace)
    {
        if (ex is not (DllNotFoundException or BadImageFormatException))
            return false;

        if (shutdownStarted)
            return true;

        if (string.IsNullOrEmpty(stackTrace))
            return false;

        return stackTrace.Contains("__scrt_uninitialize_type_info", StringComparison.Ordinal)
               || stackTrace.Contains("ModuleUninitializer.SingletonDomainUnload", StringComparison.Ordinal)
               || stackTrace.Contains("__std_type_info_destroy_list", StringComparison.Ordinal)
               || stackTrace.Contains("_app_exit_callback", StringComparison.Ordinal);
    }
}
