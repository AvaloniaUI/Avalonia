using System;
using System.Runtime.InteropServices;
#pragma warning disable CS8981 // The type name only contains lower-cased ascii characters. Such names may become reserved for the language.

namespace Avalonia.Wayland.Server.Interop;


unsafe class UnsafeNativeMethods
{
    public struct pollfd
    {
        public int fd;
        public PollEvents events;
        public PollEvents revents;
    }
    
    
    
    
    public const int O_NONBLOCK = 2048;
    public const int O_CLOEXEC = 0x80000;

    [Flags]
    public enum PollEvents : short
    {
        POLLIN = 0x0001,
        POLLPRI = 0x0002,
        POLLOUT = 0x0004,
        POLLERR = 0x0008,
        POLLHUP = 0x0010,
        POLLNVAL = 0x0020
    }

    public enum Errno
    {
        EINTR = 4,
        EAGAIN = 11,
        EPIPE = 32,
        ECONNRESET = 104,
        EPROTO = 71,
    }

    [DllImport("libc", SetLastError = true)]
    public static extern int ppoll(pollfd* fds, IntPtr nfds, IntPtr timespec, IntPtr sigset);
    
    [DllImport("libc")]
    public static extern int pipe2(int* fds, int flags);

    [DllImport("libc")]
    public static extern IntPtr write(int fd, void* buf, IntPtr count);

    [DllImport("libc")]
    public static extern IntPtr read(int fd, void* buf, IntPtr count);
    
    [DllImport("libc")]
    public static extern int memfd_create(string name, uint flags);
    
    [DllImport("libc", SetLastError = true)]
    public static extern int ftruncate(int fd, IntPtr length);

    [DllImport("libc")]
    public static extern void close(int fd);
    
    [DllImport("libc", EntryPoint = "mmap", SetLastError = true)]
    public static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags,
        int fd, IntPtr offset);
    
    [DllImport("libc", EntryPoint = "munmap", SetLastError = true)]
    public static extern int munmap(IntPtr addr, IntPtr length);

    // libwayland-cursor.so.0

    [StructLayout(LayoutKind.Sequential)]
    public struct wl_cursor_image
    {
        public uint width;
        public uint height;
        public uint hotspot_x;
        public uint hotspot_y;
        public uint delay;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct wl_cursor
    {
        public uint image_count;
        public wl_cursor_image** images;
        public byte* name;
    }

    [DllImport("libwayland-cursor.so.0")]
    public static extern IntPtr wl_cursor_theme_load(string? name, int size, IntPtr shm);

    [DllImport("libwayland-cursor.so.0")]
    public static extern void wl_cursor_theme_destroy(IntPtr theme);

    [DllImport("libwayland-cursor.so.0")]
    public static extern wl_cursor* wl_cursor_theme_get_cursor(IntPtr theme, string name);

    [DllImport("libwayland-cursor.so.0")]
    public static extern IntPtr wl_cursor_image_get_buffer(wl_cursor_image* image);
}