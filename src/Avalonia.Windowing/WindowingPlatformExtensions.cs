using System;
using Avalonia.Controls;

namespace Avalonia
{
    public static class WindowingPlatformExtensions
    {
        public static T UseWinit<T>(this T builder, bool? useDeferredRendering = null) where T : AppBuilderBase<T>, new()
        {
           // if (useDeferredRendering.HasValue)
           //     MonoMac.MonoMacPlatform.UseDeferredRendering = useDeferredRendering.Value;
            return builder.UseWindowingSubsystem(Windowing.WindowingPlatform.Initialize, "Winit");
        }
    }
}