using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.Shared.PlatformSupport
{
    static class StandardRuntimePlatformServices
    {
        public static void Register(Assembly assembly = null)
        {
            var standardPlatform = new StandardRuntimePlatform();
            AssetLoader.RegisterResUriParsers();
            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToConstant(standardPlatform)
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(assembly))
                .Bind<IDynamicLibraryLoader>().ToConstant(
#if __IOS__
                    new IOSLoader()
#else
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? (IDynamicLibraryLoader)new Win32Loader()
                        : new UnixLoader()
#endif
                );
        }
    }
}
