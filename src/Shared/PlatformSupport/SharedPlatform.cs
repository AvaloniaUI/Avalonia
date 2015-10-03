using Perspex.Platform;

namespace Perspex.Shared.PlatformSupport
{
    internal static class SharedPlatform
    {
        public static void Register()
        {
            PerspexLocator.CurrentMutable
                .Bind<IPclPlatformWrapper>().ToSingleton<PclPlatformWrapper>()
                .Bind<IAssetLoader>().ToSingleton<AssetLoader>();
        }
    }
}