using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Compatibility;

namespace Avalonia.Platform.Internal;

internal class UnmanagedBlob : IDisposable
{
    private IntPtr _address;
    private readonly object _lock = new object();
#if DEBUG
    private static readonly List<string> Backtraces = new List<string>();
    private static Thread? GCThread;
    private readonly string _backtrace;
    private static readonly object _btlock = new object();

    class GCThreadDetector
    {
        ~GCThreadDetector()
        {
            GCThread = Thread.CurrentThread;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Spawn() => new GCThreadDetector();

    static UnmanagedBlob()
    {
        Spawn();
        GC.WaitForPendingFinalizers();
    }
#endif
    public static bool SuppressFinalizerWarning { get; set; }

    public UnmanagedBlob(int size)
    {
        try
        {
            if (size <= 0)
                throw new ArgumentException("Positive number required", nameof(size));
            _address = Alloc(size);
            GC.AddMemoryPressure(size);
            Size = size;
        }
        catch
        {
            GC.SuppressFinalize(this);
            throw;
        }
#if DEBUG
        _backtrace = Environment.StackTrace;
        lock (_btlock)
            Backtraces.Add(_backtrace);
#endif
    }

    void DoDispose()
    {
        lock (_lock)
        {
            if (!IsDisposed)
            {
#if DEBUG
                lock (_btlock)
                    Backtraces.Remove(_backtrace);
#endif
                Free(_address, Size);
                GC.RemoveMemoryPressure(Size);
                IsDisposed = true;
                _address = IntPtr.Zero;
                Size = 0;
            }
        }
    }

    public void Dispose()
    {
#if DEBUG
        if (!SuppressFinalizerWarning && Thread.CurrentThread.ManagedThreadId == GCThread?.ManagedThreadId)
        {
            lock (_lock)
            {
                if (!IsDisposed)
                {
                    Console.Error.WriteLine("Native blob disposal from finalizer thread\nBacktrace: "
                                         + Environment.StackTrace
                                         + "\n\nBlob created by " + _backtrace);
                }
            }
        }
#endif
        DoDispose();
        GC.SuppressFinalize(this);
    }

    ~UnmanagedBlob()
    {
#if DEBUG
        Console.Error.WriteLine("Undisposed native blob created by " + _backtrace);
#endif
        DoDispose();
    }

    public IntPtr Address => IsDisposed ? throw new ObjectDisposedException("UnmanagedBlob") : _address;
    public int Size { get; private set; }
    public bool IsDisposed { get; private set; }

    [DllImport("libc")]
    private static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, IntPtr offset);
    [DllImport("libc")]
    private static extern int munmap(IntPtr addr, IntPtr length);

    // Could be replaced with https://github.com/dotnet/runtime/issues/40892 when it will be available.
    private IntPtr Alloc(int size)
    {
        if (!OperatingSystemEx.IsLinux())
        {
            return Marshal.AllocHGlobal(size);
        }
        else
        {
            var rv = mmap(IntPtr.Zero, new IntPtr(size), 3, 0x22, -1, IntPtr.Zero);
            if (rv.ToInt64() == -1 || (ulong)rv.ToInt64() == 0xffffffff)
            {
#if NET6_0_OR_GREATER
                var errno = Marshal.GetLastSystemError();
                throw new Exception("Unable to allocate memory: " + errno);
#else
                throw new Exception("Unable to allocate memory");
#endif
            }
            return rv;
        }
    }

    private void Free(IntPtr ptr, int len)
    {
        if (!OperatingSystemEx.IsLinux())
        {
            Marshal.FreeHGlobal(ptr);
        }
        else
        {
            if (munmap(ptr, new IntPtr(len)) == -1)
            {
#if NET6_0_OR_GREATER
                var errno = Marshal.GetLastSystemError();
                throw new Exception("Unable to free memory: " + errno);
#else
                throw new Exception("Unable to free memory");
#endif
            }
        }
    }
}
