using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Native;

namespace Avalonia
{
    public static class AvaloniaNativePlatformExtensions
    {
        public static AppBuilder UseAvaloniaNative(this AppBuilder builder)
        {
            builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(() =>
                {
                    var platform = AvaloniaNativePlatform.Initialize(
                        AvaloniaLocator.Current.GetService<AvaloniaNativePlatformOptions>() ??
                        new AvaloniaNativePlatformOptions());

                        builder.AfterSetup (x=>
                        {
                            platform.SetupApplicationName();
                            platform.SetupApplicationMenuExporter();
                        });
                })
                .UseLifetimeOverride(type => type == typeof(ClassicDesktopStyleApplicationLifetime)
                    ? new MacOSClassicDesktopStyleApplicationLifetime() : null);

            return builder;
        }
    }

    public enum AvaloniaNativeRenderingMode
    {
        /// <summary>
        /// Avalonia would try to use native OpenGL with GPU rendering.
        /// </summary>
        OpenGl = 1,
        /// <summary>
        /// Avalonia is rendered into a framebuffer.
        /// </summary>
        Software = 2,
        /// <summary>
        /// Avalonia would try to use Metal with GPU rendering.
        /// </summary>
        [Obsolete("Experimental, unstable, not for production usage")]
        Metal = 3
    }
    
    /// <summary>
    /// OSX backend options.
    /// </summary>
    public class AvaloniaNativePlatformOptions
    {
        /// <summary>
        /// Gets or sets Avalonia rendering modes with fallbacks.
        /// The first element in the array has the highest priority.
        /// The default value is: <see cref="AvaloniaNativeRenderingMode.OpenGl"/>, <see cref="AvaloniaNativeRenderingMode.Software"/>.
        /// </summary>
        /// <remarks>
        /// If application should work on as wide range of devices as possible,
        /// at least add <see cref="AvaloniaNativeRenderingMode.Software"/> as a fallback value.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
        public IReadOnlyList<AvaloniaNativeRenderingMode> RenderingMode { get; set; } = new[]
        {
            AvaloniaNativeRenderingMode.OpenGl,
            AvaloniaNativeRenderingMode.Software
        };
        
        /// <summary>
        /// Embeds popups to the window when set to true. The default value is false.
        /// </summary>
        public bool OverlayPopups { get; set; }

        /// <summary>
        /// This property should be used in case you want to build Avalonia OSX native part by yourself
        /// and make your Avalonia app run with it. The default value is null.
        /// </summary>
        public string AvaloniaNativeLibraryPath { get; set; }
    }

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// OSX front-end options.
    /// </summary>
    public class MacOSPlatformOptions
    {
        /// <summary>
        /// Determines whether to show your application in the dock when it runs. The default value is true.
        /// </summary>
        public bool ShowInDock { get; set; } = true;

        /// <summary>
        /// By default, Avalonia adds items like Quit, Hide to the OSX Application Menu.
        /// You can prevent Avalonia from adding those items to the OSX Application Menu with this property. The default value is false.
        /// </summary>
        public bool DisableDefaultApplicationMenuItems { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether the native macOS menu bar will be enabled for the application.
        /// </summary>
        public bool DisableNativeMenus { get; set; }
        
        public bool DisableSetProcessName { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether Avalonia can install its own AppDelegate.
        /// Disabling this can be useful in some scenarios like when running as a plugin inside an existing macOS application.
        /// </summary>
        public bool DisableAvaloniaAppDelegate { get; set; }
    }
}
