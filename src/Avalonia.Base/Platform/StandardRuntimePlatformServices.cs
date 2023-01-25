using System.Reflection;
using Avalonia.Compatibility;
using Avalonia.Platform.Internal;
using Avalonia.Platform.Interop;

namespace Avalonia.Platform
{
    public static class StandardRuntimePlatformServices
    {
        public static void Register(Assembly? assembly = null)
        {
            var standardPlatform = new StandardRuntimePlatform();

            AssetLoader.RegisterResUriParsers();
            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToConstant(standardPlatform)
                .Bind<IAssetLoader>().ToConstant(new AssetLoader(assembly))
                .Bind<IDynamicLibraryLoader>().ToConstant(
#if NET6_0_OR_GREATER
                    new Net6Loader()
#else
                    OperatingSystemEx.IsWindows() ? (IDynamicLibraryLoader)new Win32Loader()
                        : OperatingSystemEx.IsMacOS() || OperatingSystemEx.IsLinux() || OperatingSystemEx.IsAndroid() ? new UnixLoader()
                        : new NotSupportedLoader()
#endif
                );
        }
    }
}
