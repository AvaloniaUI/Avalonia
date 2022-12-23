using Avalonia.Compatibility;
using Avalonia.Controls;
using Avalonia.Logging;

namespace Avalonia
{
    public static class AppBuilderDesktopExtensions
    {
        public static AppBuilder UsePlatformDetect(this AppBuilder builder)
        {
            // We don't have the ability to load every assembly right now, so we are
            // stuck with manual configuration  here
            // Helpers are extracted to separate methods to take the advantage of the fact
            // that CLR doesn't try to load dependencies before referencing method is jitted
            // Additionally, by having a hard reference to each assembly,
            // we verify that the assemblies are in the final .deps.json file
            //  so .NET Core knows where to load the assemblies from,.
            if (OperatingSystemEx.IsWindows())
            {
                LoadWin32(builder);
                LoadSkia(builder);
            }
            else if(OperatingSystemEx.IsMacOS())
            {
                LoadAvaloniaNative(builder);
                LoadSkia(builder);
            }
            else if (OperatingSystemEx.IsLinux())
            {
                LoadX11(builder);
                LoadSkia(builder);
            }
            else
            {
                Logger.TryGet(LogEventLevel.Warning, LogArea.Platform)?.Log(builder,
                    "Avalonia.Desktop package was referenced on non-desktop platform or it isn't supported");
            }

            return builder;
        }

        static void LoadAvaloniaNative(AppBuilder builder)
             => builder.UseAvaloniaNative();
        static void LoadWin32(AppBuilder builder)
             => builder.UseWin32();

        static void LoadX11(AppBuilder builder)
             => builder.UseX11();

        static void LoadSkia(AppBuilder builder)
             => builder.UseSkia();
    }
}
