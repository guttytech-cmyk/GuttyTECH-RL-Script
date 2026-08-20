using Xunit;

namespace GuttyRL.Tests;

public class CrashFilterTests
{
    private const string ShutdownStack =
        """
           at __std_type_info_destroy_list(__type_info_node*)
           at __scrt_uninitialize_type_info()
           at _app_exit_callback()
           at <CrtImplementationDetails>.ModuleUninitializer.SingletonDomainUnload(Object source, EventArgs arguments)
        """;

    [Fact]
    public void Ignores_crt_unload_dll_not_found_on_close()
    {
        var ex = new DllNotFoundException("Dll was not found.");
        Assert.True(CrashFilter.IsHarmlessShutdownNativeUnload(ex, shutdownStarted: false, ShutdownStack));
    }

    [Fact]
    public void Ignores_any_native_dll_miss_after_shutdown_started()
    {
        var ex = new DllNotFoundException("PresentationNative_cor3.dll");
        Assert.True(CrashFilter.IsHarmlessShutdownNativeUnload(ex, shutdownStarted: true, stackTrace: "at Something.Else()"));
    }

    [Fact]
    public void Still_reports_missing_dll_at_startup()
    {
        var ex = new DllNotFoundException("PresentationNative_cor3.dll");
        Assert.False(CrashFilter.IsHarmlessShutdownNativeUnload(ex, shutdownStarted: false, stackTrace: "at GuttyRL.App..ctor()"));
    }

    [Fact]
    public void Does_not_swallow_normal_exceptions()
    {
        var ex = new InvalidOperationException("boom");
        Assert.False(CrashFilter.IsHarmlessShutdownNativeUnload(ex, shutdownStarted: true, stackTrace: ShutdownStack));
    }
}
