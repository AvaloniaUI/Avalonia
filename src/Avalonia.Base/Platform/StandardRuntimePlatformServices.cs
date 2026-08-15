using System.Reflection;

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
