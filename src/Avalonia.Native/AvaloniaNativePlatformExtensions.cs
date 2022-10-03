using System;
using Avalonia.Controls;
using Avalonia.Native;

namespace Avalonia
{
    public static class AvaloniaNativePlatformExtensions
    {
        public static T UseAvaloniaNative<T>(this T builder)
            where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(() =>
            {
                var platform = AvaloniaNativePlatform.Initialize(
                    AvaloniaLocator.Current.GetService<AvaloniaNativePlatformOptions>() ??
                    new AvaloniaNativePlatformOptions());

                    builder.AfterSetup (x=>
                    {
                        platform.SetupApplicationName();
                        platform.SetupApplicationMenuExporter();
                    });
            });

            return builder;
        }
    }

    /// <summary>
    /// OSX backend options.
    /// </summary>
    public class AvaloniaNativePlatformOptions
    {
        /// <summary>
        /// Deferred renderer would be used when set to true. Immediate renderer when set to false. The default value is true.
        /// </summary>
        /// <remarks>
        /// Avalonia has two rendering modes: Immediate and Deferred rendering.
        /// Immediate re-renders the whole scene when some element is changed on the scene. Deferred re-renders only changed elements.
        /// </remarks>
        public bool UseDeferredRendering { get; set; } = true;
        
        /// <summary>
        /// Enables new compositing rendering with UWP-like API
        /// </summary>
        public bool UseCompositor { get; set; } = true;

        /// <summary>
        /// Determines whether to use GPU for rendering in your project. The default value is true.
        /// </summary>
        public bool UseGpu { get; set; } = true;

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
    }
}
