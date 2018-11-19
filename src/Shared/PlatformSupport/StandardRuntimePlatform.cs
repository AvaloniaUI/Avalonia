// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Shared.PlatformSupport
{
    internal partial class StandardRuntimePlatform : IRuntimePlatform
    {
        public IDisposable StartSystemTimer(TimeSpan interval, Action tick)
        {
            return new Timer(_ => tick(), null, interval, interval);
        }

        public IUnmanagedBlob AllocBlob(int size) => new UnmanagedBlob(this, size);
        
        class UnmanagedBlob : IUnmanagedBlob
        {
            private readonly StandardRuntimePlatform _plat;
            private IntPtr _address;
            private readonly object _lock = new object();
#if DEBUG
            private static readonly List<string> Backtraces = new List<string>();
            private static Thread GCThread;
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
            
            public UnmanagedBlob(StandardRuntimePlatform plat, int size)
            {
                if (size <= 0)
                    throw new ArgumentException("Positive number required", nameof(size));
                _plat = plat;
                _address = plat.Alloc(size);
                GC.AddMemoryPressure(size);
                Size = size;
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
                        _plat.Free(_address, Size);
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
                    lock(_lock)
                        if (!IsDisposed)
                            Console.Error.WriteLine("Native blob disposal from finalizer thread\nBacktrace: "
                                                    + Environment.StackTrace
                                                    + "\n\nBlob created by " + _backtrace);
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
        
        
        
#if NET461 || NETCOREAPP2_0
        [DllImport("libc", SetLastError = true)]
        private static extern IntPtr mmap(IntPtr addr, IntPtr length, int prot, int flags, int fd, IntPtr offset);
        [DllImport("libc", SetLastError = true)]
        private static extern int munmap(IntPtr addr, IntPtr length);
        [DllImport("libc", SetLastError = true)]
        private static extern long sysconf(int name);

        private bool? _useMmap;
        private bool UseMmap 
            => _useMmap ?? ((_useMmap = GetRuntimeInfo().OperatingSystem == OperatingSystemType.Linux)).Value;
        
        IntPtr Alloc(int size)
        {
            if (UseMmap)
            {
                var rv = mmap(IntPtr.Zero, new IntPtr(size), 3, 0x22, -1, IntPtr.Zero);
                if (rv.ToInt64() == -1 || (ulong) rv.ToInt64() == 0xffffffff)
                {
                    var errno = Marshal.GetLastWin32Error();
                    throw new Exception("Unable to allocate memory: " + errno);
                }
                return rv;
            }
            else
                return Marshal.AllocHGlobal(size);
        }

        void Free(IntPtr ptr, int len)
        {
            if (UseMmap)
            {
                if (munmap(ptr, new IntPtr(len)) == -1)
                {
                    var errno = Marshal.GetLastWin32Error();
                    throw new Exception("Unable to free memory: " + errno);
                }
            }
            else
                Marshal.FreeHGlobal(ptr);
        }
#else
        IntPtr Alloc(int size) => Marshal.AllocHGlobal(size);
        void Free(IntPtr ptr, int len) => Marshal.FreeHGlobal(ptr);
#endif
    }
}
