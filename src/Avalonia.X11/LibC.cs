using System;
using System.Runtime.InteropServices;

namespace Avalonia.X11;

internal static class LibC
{
    [DllImport("libc", SetLastError = true)]
    public static extern IntPtr shmat(int shmid, IntPtr shmaddr, int shmflg);

    /// <summary>
    /// create key if key does not exist
    /// </summary>
    public const int IPC_CREAT = 01000;

    /// <summary>
    /// private key
    /// </summary>
    public const int IPC_PRIVATE = 0;

    [DllImport("libc", SetLastError = true)]
    public static extern int shmget(int key, IntPtr size, int shmflg);
}
