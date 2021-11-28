using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Platform;

namespace Avalonia.Blazor
{
    class BlazorRuntimePlatform : IRuntimePlatform
    {
        public static IRuntimePlatform Instance = new BlazorRuntimePlatform();
        public IDisposable StartSystemTimer(TimeSpan interval, Action tick)
        {
            return new Timer(_ => tick(), null, interval, interval);
        }

        public RuntimePlatformInfo GetRuntimeInfo()
        {
            return new RuntimePlatformInfo
            {
                IsDesktop = false,
                IsMobile = false,
                IsMono = true,
                IsUnix = false,
                IsCoreClr = false,
                IsDotNetFramework = false
            };
        }

        class BasicBlob : IUnmanagedBlob
        {
            private IntPtr _data;
            public BasicBlob(int size)
            {
                _data = Marshal.AllocHGlobal(size);
                Size = size;
            }
            public void Dispose()
            {
                if (_data != IntPtr.Zero)
                    Marshal.FreeHGlobal(_data);
                _data = IntPtr.Zero;
            }

            public IntPtr Address => _data;
            public int Size { get; }
            public bool IsDisposed => _data == IntPtr.Zero;
        }
        
        public IUnmanagedBlob AllocBlob(int size)
        {
            return new BasicBlob(size);
        }

        public static void RegisterServices(AvaloniaBlazorAppBuilder builder)
        {
            AssetLoader.RegisterResUriParsers();
            AvaloniaLocator.CurrentMutable.Bind<IRuntimePlatform>().ToConstant(Instance);
            AvaloniaLocator.CurrentMutable.Bind<IAssetLoader>().ToConstant(new AssetLoader());
        }
    }
}