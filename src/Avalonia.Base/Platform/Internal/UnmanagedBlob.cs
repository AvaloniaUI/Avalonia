using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.Platform.Internal;

internal class UnmanagedBlob : IUnmanagedBlob
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

    public UnmanagedBlob(int size)
    {
        try
        {
            if (size <= 0)
                throw new ArgumentException("Positive number required", nameof(size));
            _address = Marshal.AllocHGlobal(size);
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
                Marshal.FreeHGlobal(_address);
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
        if (Thread.CurrentThread.ManagedThreadId == GCThread?.ManagedThreadId)
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
}
