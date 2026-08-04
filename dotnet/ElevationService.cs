using System.Runtime.InteropServices;
using System.Security.Principal;

namespace GuttyRL;

/// <summary>Deteccao robusta de processo elevado (UAC / TokenElevation).</summary>
internal static class ElevationService
{
    public static bool IsAdministrator()
    {
        try
        {
            if (IsProcessElevated())
                return true;
        }
        catch { }

        try
        {
            using var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                return true;

            // Fallback: SID Builtin Administrators no token (alguns hosts mentem no IsInRole).
            var adminSid = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            if (id.Groups is { } groups)
            {
                foreach (IdentityReference group in groups)
                {
                    try
                    {
                        if (adminSid.Equals(group.Translate(typeof(SecurityIdentifier))))
                            return true;
                    }
                    catch { }
                }
            }
        }
        catch { }

        return false;
    }

    private static bool IsProcessElevated()
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, out IntPtr token))
            return false;

        try
        {
            var elevation = new TOKEN_ELEVATION();
            int size = Marshal.SizeOf<TOKEN_ELEVATION>();
            if (!GetTokenInformation(
                    token,
                    TokenElevation,
                    ref elevation,
                    size,
                    out _))
            {
                return false;
            }

            return elevation.TokenIsElevated != 0;
        }
        finally
        {
            CloseHandle(token);
        }
    }

    private const int TOKEN_QUERY = 0x0008;
    private const int TokenElevation = 20;

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_ELEVATION
    {
        public int TokenIsElevated;
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, int desiredAccess, out IntPtr tokenHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetTokenInformation(
        IntPtr tokenHandle,
        int tokenInformationClass,
        ref TOKEN_ELEVATION tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
