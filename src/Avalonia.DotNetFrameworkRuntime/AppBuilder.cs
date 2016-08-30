using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;

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
            var platformId = (int)Environment.OSVersion.Platform;
            if (platformId == 4 || platformId == 6)
            {
                UseRenderingSubsystem("Avalonia.Cairo");
                UseWindowingSubsystem("Avalonia.Gtk");
                WindowingSubsystemName = "Gtk";
                RenderingSubsystemName = "Cairo";
            }
            else
            {
                UseRenderingSubsystem("Avalonia.Direct2D1");
                UseWindowingSubsystem("Avalonia.Win32");
                WindowingSubsystemName = "Win32";
                RenderingSubsystemName = "Direct2D1";
            }
            return this;
        }
    }
}
