using System;
using Android.Content.PM;
using Android.Content;
using Avalonia.Platform;
using App = Android.App.Application;
using System.Reflection;

namespace Avalonia
{
    internal static class AndroidRuntimePlatformServices
    {
        public static AppBuilder UseAndroidRuntimePlatformSubsystem(this AppBuilder builder)
        {
            builder.UseRuntimePlatformSubsystem(() => Register(builder.ApplicationType?.Assembly), nameof(AndroidRuntimePlatform));
            return builder;
        }

        public static void Register(Assembly? assembly = null)
        {
            AssetLoader.RegisterResUriParsers();
            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToSingleton<AndroidRuntimePlatform>()
                .Bind<IAssetLoader>().ToConstant(new StandardAssetLoader(assembly));
        }
    }


    internal class AndroidRuntimePlatform : StandardRuntimePlatform
    {
        private static readonly Lazy<RuntimePlatformInfo> s_info = new(() =>
        {
            var isDesktop = IsRunningOnDesktop(App.Context);
            var isTv = IsRunningOnTv(App.Context);

            return new RuntimePlatformInfo
            {
                IsDesktop = isDesktop,
                IsMobile = !isTv && !isDesktop,
                IsTV = isTv
            };
        });

        private static bool IsRunningOnDesktop(Context context) =>
            context.PackageManager is { } packageManager &&
            (packageManager.HasSystemFeature("org.chromium.arc") ||
             packageManager.HasSystemFeature("org.chromium.arc.device_management"));

        private static bool IsRunningOnTv(Context context) => 
            context.PackageManager?.HasSystemFeature(PackageManager.FeatureLeanback) == true;

        public override RuntimePlatformInfo GetRuntimeInfo() => s_info.Value;
    }
}
