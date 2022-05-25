using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Avalonia.Platform
{
    public class StandardRuntimePlatform : IRuntimePlatform
    {
        public IDisposable StartSystemTimer(TimeSpan interval, Action tick)
        {
            return new Timer(_ => tick(), null, interval, interval);
        }

        public IUnmanagedBlob AllocBlob(int size) => new UnmanagedBlob(size);

        private class UnmanagedBlob : IUnmanagedBlob
        {
            private IntPtr _address;
            private readonly object _lock = new object();

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
            }

            private void DoDispose()
            {
                lock (_lock)
                {
                    if (!IsDisposed)
                    {
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
                DoDispose();
                GC.SuppressFinalize(this);
            }

            ~UnmanagedBlob()
            {
                DoDispose();
            }

            public IntPtr Address => IsDisposed ? throw new ObjectDisposedException("UnmanagedBlob") : _address;
            public int Size { get; private set; }
            public bool IsDisposed { get; private set; }
        }
        
        private static readonly Lazy<RuntimePlatformInfo> Info = new(() =>
        {
            OperatingSystemType os;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                os = OperatingSystemType.OSX;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
                os = OperatingSystemType.Linux;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                os = OperatingSystemType.WinNT;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Android")))
                os = OperatingSystemType.Android;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("iOS")))
                os = OperatingSystemType.iOS;
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("Browser")))
                os = OperatingSystemType.Browser;
            else
                throw new Exception("Unknown OS platform " + RuntimeInformation.OSDescription);

            return new RuntimePlatformInfo
            {
#if NETCOREAPP
                IsCoreClr = true,
#elif NETFRAMEWORK
                IsDotNetFramework = true,
#endif
                IsDesktop = os is OperatingSystemType.Linux or OperatingSystemType.OSX or OperatingSystemType.WinNT,
                IsMono = os is OperatingSystemType.Android or OperatingSystemType.iOS or OperatingSystemType.Browser,
                IsMobile = os is OperatingSystemType.Android or OperatingSystemType.iOS,
                IsUnix = os is OperatingSystemType.Linux or OperatingSystemType.OSX or OperatingSystemType.Android,
                IsBrowser = os == OperatingSystemType.Browser,
                OperatingSystem = os,
            };
        });


        public virtual RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
    }
}
