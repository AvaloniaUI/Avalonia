using System.Reflection;
using Avalonia.Platform;
using Avalonia.Platform.Interop;

namespace Avalonia.PlatformSupport
{
    public static class StandardRuntimePlatformServices
    {
        public static void Register(Assembly? assembly = null)
        {
            var standardPlatform = new StandardRuntimePlatform();
            var os = standardPlatform.GetRuntimeInfo().OperatingSystem;

            AssetLoader.RegisterResUriParsers();
            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToConstant(standardPlatform)
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(assembly))
                .Bind<IDynamicLibraryLoader>().ToConstant(
                    os switch
                    {
                        OperatingSystemType.WinNT => new Win32Loader(),
                        OperatingSystemType.OSX => new UnixLoader(),
                        OperatingSystemType.Linux => new UnixLoader(),
                        OperatingSystemType.Android => new UnixLoader(),
                        // iOS, WASM, ...
                        _ => (IDynamicLibraryLoader)new NotSupportedLoader()
                    }
                );
        }
    }
}
