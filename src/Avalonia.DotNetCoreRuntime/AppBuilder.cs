using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;

namespace Avalonia
{
    public sealed class AppBuilder : AppBuilderBase<AppBuilder>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        public AppBuilder()
            : base(new StandardRuntimePlatform(), () => StandardRuntimePlatformServices.Register())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        /// <param name="app">The <see cref="Application"/> instance.</param>
        public AppBuilder(Application app) : this()
        {
            Instance = app;
        }

        /// <summary>
        /// Instructs the <see cref="AppBuilder"/> to use the best settings for the platform.
        /// </summary>
        /// <returns>An <see cref="AppBuilder"/> instance.</returns>
        public AppBuilder UsePlatformDetect()
        {
            //We don't have the ability to load every assembly right now, so we are
            //stuck with manual configuration  here
            if (RuntimePlatform.GetRuntimeInfo().OperatingSystem == OperatingSystemType.WinNT)
                this.UseWin32();
            else
            {
                //TODO: Register GTK3
            }
            //TODO: Register Skia#

            return this;
        }
    }
}