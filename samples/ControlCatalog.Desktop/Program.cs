using Perspex.Logging.Serilog;
using Serilog;
using System;
using System.Linq;
using Perspex;
using System.Reflection;
using Perspex.Platform;


// Not sure where the best home for this is
namespace Perspex
{
    public static class SharedApplicationExtensions
    {
        // For true Portable apps we need to select the PCL assembly NOT the host platform exe. Unfortunately
        // Win32 subsystem registers one by default (the wrong one) and so this can override that.
        //
        public static AppT UseAssetAssembly<AppT>(this AppT app, Assembly assembly) where AppT : Application
        {
            // Asset loading searches our own assembly?
            PerspexLocator.CurrentMutable.GetService<IAssetLoader>().SetDefaultAssembly(assembly);
            return app;
        }
    }
}

namespace ControlCatalog
{
    internal class Program
    {
        static void Main(string[] args)
        {
            InitializeLogging();

            new App()
                .ConfigureRenderSystem(args)
                .UseAssetAssembly(typeof(App).Assembly)
                .LoadFromXaml()
                .RunWithMainWindow<MainWindow>();
        }

        // This will be made into a runtime configuration extension soon!
        private static void InitializeLogging()
        {
#if DEBUG
            SerilogLogger.Initialize(new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.Trace(outputTemplate: "{Area}: {Message}")
                .CreateLogger());
#endif
        }

    }

    /////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Experimental: Would like to move this into a shared location once I figure out the best place for it 
    // considering all common libraries are PCL and do not have access to Environment.OSVersion.Platform
    // nor do they have access to the platform specific render/subsystem extensions.
    // 
    // Perhaps via DI we register each system with a priority/rank
    //
    public static class RenderSystemExtensions
    {
        [Flags]
        enum RenderSystem
        {
            None = 0,
            GTK = 1,
            Skia = 2,
            Direct2D = 4
        };

        /// <summary>
        /// Default (Optimal) render system for a particular platform
        /// </summary>
        /// <returns></returns>
        private static RenderSystem DefaultRenderSystem()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return RenderSystem.GTK;

                case PlatformID.Unix:
                    return RenderSystem.GTK;

                case PlatformID.Win32Windows:
                    return RenderSystem.Direct2D;
            }

            return RenderSystem.None;
        }

        /// <summary>
        /// Returns an array of avalidable rendering systems in priority order
        /// </summary>
        /// <returns></returns>
        private static RenderSystem[] AvailableRenderSystems()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.MacOSX:
                    return new RenderSystem[] { RenderSystem.GTK, RenderSystem.Skia };

                case PlatformID.Unix:
                    return new RenderSystem[] { RenderSystem.GTK, RenderSystem.Skia };

                case PlatformID.Win32Windows:
                    return new RenderSystem[] { RenderSystem.Direct2D, RenderSystem.Skia, RenderSystem.GTK };
            }

            return new RenderSystem[0];
        }

        /// <summary>
        /// Selects the optimal render system for desktop platforms. Supports cmd line overrides
        /// </summary>
        /// <param name="app"></param>
        /// <param name="args"></param>
        public static TApp ConfigureRenderSystem<TApp>(this TApp app, string[] args) where TApp : Application
        {
            // So this all works great under Windows where it can support
            // ALL configurations. But on OSX/Unix we cannot use Direct2D
            //
            if (args.Contains("--gtk") || DefaultRenderSystem() == RenderSystem.GTK)
            {
                app.UseGtk();
                app.UseCairo();
            }
            else
            {
                app.UseWin32();

                if (args.Contains("--skia") || DefaultRenderSystem() == RenderSystem.Skia)
                {
                    app.UseSkia();
                }
                else
                {
                    app.UseDirect2D();
                }
            }

            return app;
        }
    }
}
