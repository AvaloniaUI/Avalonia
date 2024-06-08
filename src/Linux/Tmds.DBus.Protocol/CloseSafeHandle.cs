namespace Tmds.DBus.Protocol;

sealed class CloseSafeHandle : SafeHandle
{
    public CloseSafeHandle() :
        base(new IntPtr(-1), ownsHandle: true)
    { }

    public override bool IsInvalid
        => handle == new IntPtr(-1);

    protected override bool ReleaseHandle()
        => close(handle.ToInt32()) == 0;

    [DllImport("libc", SetLastError = true)]
    internal static extern int close(int fd);
}
