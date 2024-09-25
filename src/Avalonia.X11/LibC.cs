using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.X11;

internal static class LibC
{
    [DllImport("libc", SetLastError = true)]
    public static extern IntPtr shmat(int shmid, IntPtr shmaddr, int shmflg);

    //    #define IPC_CREAT	01000		/* create key if key does not exist */
    // #define IPC_PRIVATE	((key_t) 0)	/* private key */

    public const int IPC_CREAT = 01000;
    public const int IPC_PRIVATE = 0;

    /*
     * int shmget(key_t key, size_t size, int shmflg);
     */
    [DllImport("libc", SetLastError = true)]
    public static extern int shmget(int key, IntPtr size, int shmflg);
}
