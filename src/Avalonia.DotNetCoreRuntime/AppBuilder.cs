﻿using System;
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
        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilder"/> class.
        /// </summary>
        public AppBuilder()
            : base(new StandardRuntimePlatform(),
                  builder => StandardRuntimePlatformServices.Register(builder.Instance?.GetType()
                      ?.GetTypeInfo().Assembly))
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
            //Helpers are extracted to separate methods to take the advantage of the fact
            //that CLR doesn't try to load dependencies before referencing method is jitted
            if (RuntimePlatform.GetRuntimeInfo().OperatingSystem == OperatingSystemType.WinNT)
                LoadWin32();
            else
                LoadGtk3();
            this.UseSkia();

            return this;
        }

        void LoadWin32() => this.UseWin32();
        void LoadGtk3() => this.UseGtk3();
    }
}