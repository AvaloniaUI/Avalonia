using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using System.Runtime.InteropServices;

namespace Avalonia
{
    public sealed class AppBuilder : AppBuilderBase<AppBuilder>
    {
        public AppBuilder() : base(new StandardRuntimePlatform(), () => StandardRuntimePlatformServices.Register())
        {
        }

        public AppBuilder(Application app) : this()
        {
            Instance = app;
        }

        public AppBuilder UsePlatformDetect()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                UseRenderingSubsystem("Avalonia.Cairo");
                UseWindowingSubsystem("Avalonia.Gtk");
            }
            else
            {
                UseRenderingSubsystem("Avalonia.Direct2D1");
                UseWindowingSubsystem("Avalonia.Win32");
            }

            return this;
        }
    }
}
