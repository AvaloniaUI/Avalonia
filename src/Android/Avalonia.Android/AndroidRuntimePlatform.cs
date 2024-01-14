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
        private static readonly Lazy<RuntimePlatformInfo> Info = new(() =>
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
            context.PackageManager.HasSystemFeature("org.chromium.arc") ||
            context.PackageManager.HasSystemFeature("org.chromium.arc.device_management");

        private static bool IsRunningOnTv(Context context) => 
            context.PackageManager.HasSystemFeature(PackageManager.FeatureLeanback);

        public override RuntimePlatformInfo GetRuntimeInfo() => Info.Value;
    }
}
