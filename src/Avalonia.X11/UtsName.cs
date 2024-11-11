using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.X11;

internal struct UtsName : IDisposable
{
    // https://github.com/torvalds/linux/blob/master/include/uapi/linux/utsname.h
    /*
       #define __NEW_UTS_LEN 64

       struct new_utsname
       {
        char sysname[__NEW_UTS_LEN + 1];
        char nodename[__NEW_UTS_LEN + 1];
        char release[__NEW_UTS_LEN + 1];
        char version[__NEW_UTS_LEN + 1];
        char machine[__NEW_UTS_LEN + 1];
        char domainname[__NEW_UTS_LEN + 1];
       };
     */

    private UtsName(IntPtr buffer)
    {
        _buffer = buffer;
    }

    public static UtsName GetUtsName()
    {
        var ntsNameStructSize = (UtsLength + 1) * FieldCount;

        IntPtr buffer = Marshal.AllocHGlobal(ntsNameStructSize);
        try
        {
            if (uname(buffer) != 0)
            {
                throw new InvalidOperationException("uname failed");
            }

            return new UtsName(buffer);
        }
        catch
        {
            Marshal.FreeHGlobal(buffer);
            throw;
        }
    }

    private const int SystemNameFieldIndex = 0;
    public ReadOnlySpan<byte> SystemNameSpan => GetValue(SystemNameFieldIndex);
    public string SystemName => GetAsciiString(SystemNameSpan);

    private const int NodeNameFieldIndex = 1;
    public ReadOnlySpan<byte> NodeNameSpan => GetValue(NodeNameFieldIndex);
    public string NodeName => GetAsciiString(NodeNameSpan);

    private const int ReleaseFieldIndex = 2;
    public ReadOnlySpan<byte> ReleaseSpan => GetValue(ReleaseFieldIndex);
    public string Release => GetAsciiString(ReleaseSpan);

    private const int VersionFieldIndex = 3;
    public ReadOnlySpan<byte> VersionSpan => GetValue(VersionFieldIndex);
    public string Version => GetAsciiString(VersionSpan);

    private const int MachineFieldIndex = 4;
    public ReadOnlySpan<byte> MachineSpan => GetValue(MachineFieldIndex);
    public string Machine => GetAsciiString(MachineSpan);

    private const int DomainNameFieldIndex = 5;
    public ReadOnlySpan<byte> DomainNameSpan => GetValue(DomainNameFieldIndex);
    public string DomainName => GetAsciiString(DomainNameSpan);

    private const int UtsLength = 64;

    private const int FieldCount = 6;

    private static string GetAsciiString(ReadOnlySpan<byte> value)
    {
#if NET6_0_OR_GREATER
        return Encoding.ASCII.GetString(value);
#else
        unsafe
        {
            fixed (byte* ptr = value)
            {
                return Encoding.ASCII.GetString(ptr, value.Length);
            }
        }
#endif
    }

    private ReadOnlySpan<byte> GetValue(int fieldIndex)
    {
        var startOffset = (UtsLength + 1) * fieldIndex;
        var length = 0;
        while (Marshal.ReadByte(_buffer, startOffset + length) != 0)
        {
            length++;
        }

        unsafe
        {
            return new ReadOnlySpan<byte>((byte*)_buffer + startOffset, length);
        }
    }

    [DllImport("libc")]
    private static extern int uname(IntPtr buf);

    private readonly IntPtr _buffer;

    public void Dispose()
    {
        Marshal.FreeHGlobal(_buffer);
    }
}
