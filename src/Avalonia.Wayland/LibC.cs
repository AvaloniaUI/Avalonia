using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland
{
    internal static class LibC
    {
        [DllImport("libc", SetLastError = true)]
        public static extern int close(int fd);

        [DllImport("libc", SetLastError = true)]
        public static extern int read(int fd, IntPtr buffer, nint count);

        [DllImport("libc", SetLastError = true)]
        public static extern int write(int fd, IntPtr buffer, nint count);

        [DllImport("libc", SetLastError = true)]
        public static extern unsafe int pipe2(int* fds, FileDescriptorFlags flags);

        [DllImport("libc", SetLastError = true)]
        public static extern IntPtr mmap(IntPtr addr, nint length, MemoryProtection prot, SharingType flags, int fd, nint offset);

        [DllImport("libc", SetLastError = true)]
        public static extern int munmap(IntPtr addr, nint length);

        [DllImport("libc", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int memfd_create(string name, MemoryFileCreation flags);

        [DllImport("libc", SetLastError = true)]
        public static extern int ftruncate(int fd, long size);

        [DllImport("libc", SetLastError = true)]
        public static extern int fcntl(int fd, FileSealCommand cmd, FileSeals flags);

        [DllImport("libc", SetLastError = true)]
        public static extern unsafe int poll(pollfd* fds, nuint nfds, int timeout);
    }

    internal enum Errno
    {
        EINTR = 4,
        EAGAIN = 11,
        EPIPE = 32
    }

    [Flags]
    internal enum MemoryProtection
    {
        PROT_NONE = 0,
        PROT_READ = 1,
        PROT_WRITE = 2,
        PROT_EXEC = 4
    }

    internal enum SharingType
    {
        MAP_SHARED = 1,
        MAP_PRIVATE = 2
    }

    [Flags]
    internal enum MemoryFileCreation : uint
    {
        MFD_CLOEXEC = 1,
        MFD_ALLOW_SEALING = 2,
        MFD_HUGETLB = 4
    }

    internal enum FileSealCommand
    {
        F_ADD_SEALS = 1024 + 9,
        F_GET_SEALS = 1024 + 10
    }

    [Flags]
    internal enum FileSeals
    {
        F_SEAL_SEAL = 1,
        F_SEAL_SHRINK = 2,
        F_SEAL_GROW = 4,
        F_SEAL_WRITE = 8,
        F_SEAL_FUTURE_WRITE = 16
    }

    [Flags]
    internal enum FileDescriptorFlags
    {
        O_RDONLY = 0,
        O_NONBLOCK = 2048,
        O_DIRECT = 40000,
        O_CLOEXEC = 2000000
    }

    [Flags]
    internal enum EpollEvents : short
    {
        EPOLLIN = 1,
        EPOLLPRI = 2,
        EPOLLOUT = 4,
        EPOLLRDNORM = 64,
        EPOLLRDBAND = 128,
        EPOLLWRNORM = 256,
        EPOLLWRBAND = 512,
        EPOLLMSG = 1024,
        EPOLLERR = 8,
        EPOLLHUP = 16,
        EPOLLRDHUP = 8192
    }

    internal enum EpollCommands
    {
        EPOLL_CTL_ADD = 1,
        EPOLL_CTL_DEL = 2,
        EPOLL_CTL_MOD = 3
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct pollfd
    {
        public int fd; // file descriptor
        public EpollEvents events; // requested events
        public readonly EpollEvents revents; // returned events
    }
}
