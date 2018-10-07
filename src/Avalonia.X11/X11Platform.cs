using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.X11;
using static Avalonia.X11.XLib;
namespace Avalonia.X11
{
    public class AvaloniaX11Platform
    {
        public void Initialize()
        {
            Display = XOpenDisplay(IntPtr.Zero);
            DeferredDisplay = XOpenDisplay(IntPtr.Zero);
            if (Display == IntPtr.Zero)
                throw new Exception("XOpenDisplay failed");


            AvaloniaLocator.CurrentMutable.BindToSelf(this)
                .Bind<IPlatformThreadingInterface>().ToConstant(new X11PlatformThreading(Display));

        }

        public IntPtr DeferredDisplay { get; set; }
        public IntPtr Display { get; set; }
    }
}

namespace Avalonia
{
    public static class AvaloniaX11PlatformExtensions
    {
        public static T UseX11<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(() => new AvaloniaX11Platform().Initialize());
            return builder;
        }
    }

}
