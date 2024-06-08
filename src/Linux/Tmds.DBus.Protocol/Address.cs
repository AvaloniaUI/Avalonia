using System.IO.MemoryMappedFiles;

namespace Tmds.DBus.Protocol;

public static class Address
{
    private static bool _systemAddressResolved = false;
    private static string? _systemAddress = null;
    private static bool _sessionAddressResolved = false;
    private static string? _sessionAddress = null;

    public static string? System
    {
        get
        {
            if (_systemAddressResolved)
            {
                return _systemAddress;
            }

            _systemAddress = Environment.GetEnvironmentVariable("DBUS_SYSTEM_BUS_ADDRESS");

            if (string.IsNullOrEmpty(_systemAddress) && !PlatformDetection.IsWindows())
            {
                _systemAddress = "unix:path=/var/run/dbus/system_bus_socket";
            }

            _systemAddressResolved = true;
            return _systemAddress;
        }
    }

    public static string? Session
    {
        get
        {
            if (_sessionAddressResolved)
            {
                return _sessionAddress;
            }

            _sessionAddress = Environment.GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS");

            if (string.IsNullOrEmpty(_sessionAddress))
            {
                if (PlatformDetection.IsWindows())
                {
                    _sessionAddress = GetSessionBusAddressFromSharedMemory();
                }
                else
                {
                    _sessionAddress = GetSessionBusAddressFromX11();
                }
            }

            _sessionAddressResolved = true;
            return _sessionAddress;
        }
    }

    private static string? GetSessionBusAddressFromX11()
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")))
        {
            var display = XOpenDisplay(null);
            if (display == IntPtr.Zero)
            {
                return null;
            }
            string username;
            unsafe
            {
                const int BufLen = 1024;
                byte* stackBuf = stackalloc byte[BufLen];
                Passwd passwd;
                IntPtr result;
                getpwuid_r(getuid(), out passwd, stackBuf, BufLen, out result);
                if (result != IntPtr.Zero)
                {
                    username = Marshal.PtrToStringAnsi(passwd.Name)!;
                }
                else
                {
                    return null;
                }
            }
            var machineId = DBusEnvironment.MachineId.Replace("-", string.Empty);
            var selectionName = $"_DBUS_SESSION_BUS_SELECTION_{username}_{machineId}";
            var selectionAtom = XInternAtom(display, selectionName, false);
            if (selectionAtom == IntPtr.Zero)
            {
                return null;
            }
            var owner = XGetSelectionOwner(display, selectionAtom);
            if (owner == IntPtr.Zero)
            {
                return null;
            }
            var addressAtom = XInternAtom(display, "_DBUS_SESSION_BUS_ADDRESS", false);
            if (addressAtom == IntPtr.Zero)
            {
                return null;
            }

            IntPtr actualReturnType;
            IntPtr actualReturnFormat;
            IntPtr nrItemsReturned;
            IntPtr bytesAfterReturn;
            IntPtr propReturn;

            int rv = XGetWindowProperty(display, owner, addressAtom, 0, 1024, false, (IntPtr)31 /* XA_STRING */,
                out actualReturnType, out actualReturnFormat, out nrItemsReturned, out bytesAfterReturn, out propReturn);
            string? address = rv == 0 ? Marshal.PtrToStringAnsi(propReturn) : null;
            if (propReturn != IntPtr.Zero)
            {
                XFree(propReturn);
            }

            XCloseDisplay(display);

            return address;
        }
        else
        {
            return null;
        }
    }

    private static string? GetSessionBusAddressFromSharedMemory()
    {
        string? result = ReadSharedMemoryString("DBusDaemonAddressInfo", 255);
        if (string.IsNullOrEmpty(result))
        {
            result = ReadSharedMemoryString("DBusDaemonAddressInfoDebug", 255);
        }
        return result;
    }

    private static string? ReadSharedMemoryString(string id, long maxlen = -1)
    {
        if (!PlatformDetection.IsWindows())
        {
            return null;
        }
        MemoryMappedFile? shmem;
        try
        {
            shmem = MemoryMappedFile.OpenExisting(id);
        }
        catch
        {
            shmem = null;
        }
        if (shmem == null)
        {
            return null;
        }

        MemoryMappedViewStream s = shmem.CreateViewStream();
        long len = s.Length;
        if (maxlen >= 0 && len > maxlen)
        {
            len = maxlen;
        }
        if (len == 0)
        {
            return string.Empty;
        }
        if (len > int.MaxValue)
        {
            len = int.MaxValue;
        }
        byte[] bytes = new byte[len];
        int count = s.Read(bytes, 0, (int)len);
        if (count <= 0)
        {
            return string.Empty;
        }

        count = 0;
        while (count < len && bytes[count] != 0)
        {
            count++;
        }

        return Encoding.UTF8.GetString(bytes, 0, count);
    }

    struct Passwd
    {
        public IntPtr Name;
        public IntPtr Password;
        public uint UserID;
        public uint GroupID;
        public IntPtr UserInfo;
        public IntPtr HomeDir;
        public IntPtr Shell;
    }

    [DllImport("libc")]
    private static extern unsafe int getpwuid_r(uint uid, out Passwd pwd, byte* buf, int bufLen, out IntPtr result);
    [DllImport("libc")]
    private static extern uint getuid();

    [DllImport("libX11")]
    private static extern IntPtr XOpenDisplay(string? name);
    [DllImport("libX11")]
    private static extern int XCloseDisplay(IntPtr display);
    [DllImport("libX11")]
    private static extern IntPtr XInternAtom(IntPtr display, string atom_name, bool only_if_exists);
    [DllImport("libX11")]
    private static extern int XGetWindowProperty(IntPtr display, IntPtr w, IntPtr property,
        int long_offset, int long_length, bool delete, IntPtr req_type,
        out IntPtr actual_type_return, out IntPtr actual_format_return,
        out IntPtr nitems_return, out IntPtr bytes_after_return, out IntPtr prop_return);
    [DllImport("libX11")]
    private static extern int XFree(IntPtr data);
    [DllImport("libX11")]
    private static extern IntPtr XGetSelectionOwner(IntPtr display, IntPtr Atom);
}
