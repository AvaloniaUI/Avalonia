namespace Tmds.DBus.Protocol;

static class DBusEnvironment
{
    public static string? UserId
    {
        get
        {
            if (PlatformDetection.IsWindows())
            {
#if NET6_0_OR_GREATER
                return System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value;
#else
                throw new NotSupportedException("Cannot determine Windows UserId. You must manually assign it.");
#endif
            }
            else
            {
                return geteuid().ToString();
            }
        }
    }

    private static string? _machineId;

    public static string MachineId
    {
        get
        {
            if (_machineId == null)
            {
                const string MachineUuidPath = @"/var/lib/dbus/machine-id";

                if (File.Exists(MachineUuidPath))
                {
                    _machineId = Guid.Parse(File.ReadAllText(MachineUuidPath).Substring(0, 32)).ToString("N");
                }
                else
                {
                    _machineId = Guid.Empty.ToString("N");
                }
            }

            return _machineId;
        }
    }

    [DllImport("libc")]
    internal static extern uint geteuid();
}