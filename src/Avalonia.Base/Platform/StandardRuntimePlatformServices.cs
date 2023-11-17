using System.Reflection;
using Avalonia.Compatibility;
using Avalonia.Platform.Internal;
using Avalonia.Platform.Interop;

namespace Avalonia.Platform;

internal static class StandardRuntimePlatformServices
{
    public static void Register(Assembly? assembly = null)
    {
        AssetLoader.RegisterResUriParsers();
        AvaloniaLocator.CurrentMutable
            .Bind<IRuntimePlatform>().ToSingleton<StandardRuntimePlatform>()
            .Bind<IAssetLoader>().ToConstant(new StandardAssetLoader(assembly));
    }
}
