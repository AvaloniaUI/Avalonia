using System.Net.Sockets;

namespace Tmds.DBus.Protocol;

using SizeT = System.UIntPtr;
using SSizeT = System.IntPtr;

static class SocketExtensions
{
    public static ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> memory, UnixFdCollection? fdCollection)
    {
        if (fdCollection is null)
        {
            return socket.ReceiveAsync(memory, SocketFlags.None);
        }
        else
        {
            return socket.ReceiveWithHandlesAsync(memory, fdCollection);
        }
    }

    private async static ValueTask<int> ReceiveWithHandlesAsync(this Socket socket, Memory<byte> memory, UnixFdCollection fdCollection)
    {
        while (true)
        {
            await socket.ReceiveAsync(new Memory<byte>(), SocketFlags.None).ConfigureAwait(false);

            int rv = recvmsg(socket, memory, fdCollection);

            if (rv >= 0)
            {
                return rv;
            }
            else
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == EAGAIN || errno == EINTR)
                {
                    continue;
                }

                throw new SocketException(errno);
            }
        }
    }

    public static ValueTask SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, IReadOnlyList<SafeHandle>? handles)
    {
        if (handles is null || handles.Count == 0)
        {
            return SendAsync(socket, buffer);
        }
        else
        {
            return socket.SendAsyncWithHandlesAsync(buffer, handles);
        }
    }

    private static async ValueTask SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer)
    {
        while (buffer.Length > 0)
        {
            int sent = await socket.SendAsync(buffer, SocketFlags.None).ConfigureAwait(false);
            buffer = buffer.Slice(sent);
        }
    }

    private static ValueTask SendAsyncWithHandlesAsync(this Socket socket, ReadOnlyMemory<byte> buffer, IReadOnlyList<SafeHandle> handles)
    {
        socket.Blocking = false;
        do
        {
            int rv = sendmsg(socket, buffer, handles);
            if (rv > 0)
            {
                if (buffer.Length == rv)
                {
                    return default;
                }
                return SendAsync(socket, buffer.Slice(rv));
            }
            else
            {
                int errno = Marshal.GetLastWin32Error();
                if (errno == EINTR)
                {
                    continue;
                }
                // TODO (low prio): handle EAGAIN.
                return new ValueTask(Task.FromException(new SocketException(errno)));
            }
        } while (true);
    }

    private static unsafe int sendmsg(Socket socket, ReadOnlyMemory<byte> buffer, IReadOnlyList<SafeHandle> handles)
    {
        fixed (byte* ptr = buffer.Span)
        {
            IOVector* iovs = stackalloc IOVector[1];
            iovs[0].Base = ptr;
            iovs[0].Length = (SizeT)buffer.Length;

            Msghdr msg = new Msghdr();
            msg.msg_iov = iovs;
            msg.msg_iovlen = (SizeT)1;

            var fdm = new cmsg_fd();
            int size = sizeof(Cmsghdr) + 4 * handles.Count;
            msg.msg_control = &fdm;
            msg.msg_controllen = (SizeT)size;
            fdm.hdr.cmsg_len = (SizeT)size;
            fdm.hdr.cmsg_level = SOL_SOCKET;
            fdm.hdr.cmsg_type = SCM_RIGHTS;

            SafeHandle handle = socket.GetSafeHandle();
            int handleRefsAdded = 0;
            bool refAdded = false;
            try
            {
                handle.DangerousAddRef(ref refAdded);
                for (int i = 0, j = 0; i < handles.Count; i++)
                {
                    bool added = false;
                    SafeHandle h = handles[i];
                    h.DangerousAddRef(ref added);
                    handleRefsAdded++;
                    fdm.fds[j++] = h.DangerousGetHandle().ToInt32();
                }

                return (int)sendmsg(handle.DangerousGetHandle().ToInt32(), new IntPtr(&msg), 0);
            }
            finally
            {
                for (int i = 0; i < handleRefsAdded; i++)
                {
                    SafeHandle h = handles[i];
                    h.DangerousRelease();
                }

                if (refAdded)
                    handle.DangerousRelease();
            }
        }
    }

    private static unsafe int recvmsg(Socket socket, Memory<byte> buffer, UnixFdCollection handles)
    {
        fixed (byte* buf = buffer.Span)
        {
            IOVector iov = new IOVector();
            iov.Base = buf;
            iov.Length = (SizeT)buffer.Length;

            Msghdr msg = new Msghdr();
            msg.msg_iov = &iov;
            msg.msg_iovlen = (SizeT)1;

            cmsg_fd cm = new cmsg_fd();
            msg.msg_control = &cm;
            msg.msg_controllen = (SizeT)sizeof(cmsg_fd);

            var handle = socket.GetSafeHandle();
            bool refAdded = false;
            try
            {
                handle.DangerousAddRef(ref refAdded);

                int rv = (int)recvmsg(handle.DangerousGetHandle().ToInt32(), new IntPtr(&msg), 0);

                if (rv >= 0)
                {
                    if (cm.hdr.cmsg_level == SOL_SOCKET && cm.hdr.cmsg_type == SCM_RIGHTS)
                    {
                        int msgFdCount = ((int)cm.hdr.cmsg_len - sizeof(Cmsghdr)) / sizeof(int);
                        for (int i = 0; i < msgFdCount; i++)
                        {
                            handles.AddHandle(new IntPtr(cm.fds[i]));
                        }
                    }
                }
                return rv;
            }
            finally
            {
                if (refAdded)
                    handle.DangerousRelease();
            }
        }
    }

    const int SOL_SOCKET = 1;
    const int EINTR = 4;
    //const int EBADF = 9;
    static readonly int EAGAIN = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 35 : 11;
    const int SCM_RIGHTS = 1;

    private unsafe struct Msghdr
    {
        public IntPtr msg_name; //optional address
        public uint msg_namelen; //size of address
        public IOVector* msg_iov; //scatter/gather array
        public SizeT msg_iovlen; //# elements in msg_iov
        public void* msg_control; //ancillary data, see below
        public SizeT msg_controllen; //ancillary data buffer len
        public int msg_flags; //flags on received message
    }

    private unsafe struct IOVector
    {
        public void* Base;
        public SizeT Length;
    }

    private struct Cmsghdr
    {
        public SizeT cmsg_len; //data byte count, including header
        public int cmsg_level; //originating protocol
        public int cmsg_type; //protocol-specific type
    }

    private unsafe struct cmsg_fd
    {
        public Cmsghdr hdr;
        public fixed int fds[64];
    }

    [DllImport("libc", SetLastError = true)]
    public static extern SSizeT sendmsg(int sockfd, IntPtr msg, int flags);

    [DllImport("libc", SetLastError = true)]
    public static extern SSizeT recvmsg(int sockfd, IntPtr msg, int flags);
}
