using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;

namespace Avalonia.Web.Blazor
{
    internal class BlazorRuntimePlatform : IRuntimePlatform
    {
        public static readonly IRuntimePlatform Instance = new BlazorRuntimePlatform();
        
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

        private class BasicBlob : IUnmanagedBlob
        {
            public BasicBlob(int size)
            {
                Address = Marshal.AllocHGlobal(size);
                Size = size;
            }
            public void Dispose()
            {
                if (Address != IntPtr.Zero)
                    Marshal.FreeHGlobal(Address);
                Address = IntPtr.Zero;
            }

            public IntPtr Address { get; private set; }

            public int Size { get; }
            public bool IsDisposed => Address == IntPtr.Zero;
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
