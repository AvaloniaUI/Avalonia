using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Rendering;

namespace Avalonia
{
    public static class SkiaApplicationExtensions
    {
        public static T UseSkia<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseRenderingSubsystem(Skia.SkiaPlatform.Initialize, "Skia");
            return builder;
        }
    }
}

namespace Avalonia.Skia
{
    public static class SkiaPlatform
    {
        private static bool s_forceSoftwareRendering;

        public static void Initialize()
        {
            var renderInterface = new PlatformRenderInterface();
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformRenderInterface>().ToConstant(renderInterface);
        }

        public static bool ForceSoftwareRendering
        {
            get { return s_forceSoftwareRendering; }
            set
            {
                s_forceSoftwareRendering = value;

                // TODO: I left this property here as place holder. Do we still need the ability to Force software rendering? 
                // Is it even possible with SkiaSharp? Perhaps kekekes can answer as part of the HW accel work. 
                // 
                throw new NotImplementedException();
            }
        }
    }
}
