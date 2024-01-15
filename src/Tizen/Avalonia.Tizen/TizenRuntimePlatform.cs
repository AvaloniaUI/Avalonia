using System.Reflection;
using Avalonia.Platform;

namespace Avalonia.Tizen;

internal static class TizenRuntimePlatformServices
{
    public static AppBuilder UseTizenRuntimePlatformSubsystem(this AppBuilder builder)
    {
        builder.UseRuntimePlatformSubsystem(() => Register(builder.ApplicationType?.Assembly), nameof(TizenRuntimePlatform));
        return builder;
    }

    public static void Register(Assembly? assembly = null)
    {
        AssetLoader.RegisterResUriParsers();
        AvaloniaLocator.CurrentMutable
            .Bind<IRuntimePlatform>().ToSingleton<TizenRuntimePlatform>()
            .Bind<IAssetLoader>().ToConstant(new StandardAssetLoader(assembly));
    }
}

internal class TizenRuntimePlatform : StandardRuntimePlatform
{
    private static readonly Lazy<RuntimePlatformInfo> Info = new(() =>
    {
        global::Tizen.System.Information.TryGetValue("http://tizen.org/feature/profile", out string profile);

        return new RuntimePlatformInfo
        {
            IsMobile = profile.Equals("mobile", StringComparison.OrdinalIgnoreCase),
            IsTV = profile.Equals("tv", StringComparison.OrdinalIgnoreCase),
            IsDesktop = false
        };
    });

    public override RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
}
