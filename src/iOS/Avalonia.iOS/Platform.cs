using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia
{
    public enum iOSRenderingMode
    {
        OpenGl = 1,
        Metal
    }
    
    public class iOSPlatformOptions
    {
        /// <summary>
        /// Gets or sets Avalonia rendering modes with fallbacks.
        /// The first element in the array has the highest priority.
        /// The default value is: <see cref="iOSRenderingMode.OpenGl"/>, <see cref="iOSRenderingMode.Metal"/>. 
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
        public IReadOnlyList<iOSRenderingMode> RenderingMode { get; set; } = new[]
        {
            iOSRenderingMode.OpenGl, iOSRenderingMode.Metal
        };
    }

    public static class IOSApplicationExtensions
    {
        public static AppBuilder UseiOS(this AppBuilder builder)
        {
            return builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(iOS.Platform.Register, "iOS")
                .UseSkia();
        }
    }
}

namespace Avalonia.iOS
{
    static class Platform
    {
        public static iOSPlatformOptions? Options;
        public static IPlatformGraphics? Graphics;
        public static DisplayLinkTimer? Timer;
        internal static Compositor? Compositor { get; private set; }

        public static void Register()
        {
            Options = AvaloniaLocator.Current.GetService<iOSPlatformOptions>() ?? new iOSPlatformOptions();

            Graphics = InitializeGraphics(Options);
            Timer ??= new DisplayLinkTimer();
            var keyboard = new KeyboardDevice();

            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformGraphics>().ToConstant(Graphics)
                .Bind<ICursorFactory>().ToConstant(new CursorFactoryStub())
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoaderStub())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IRenderTimer>().ToConstant(Timer)
                .Bind<IDispatcherImpl>().ToConstant(DispatcherImpl.Instance)
                .Bind<IKeyboardDevice>().ToConstant(keyboard);

            Compositor = new Compositor(AvaloniaLocator.Current.GetService<IPlatformGraphics>());
        }

        private static IPlatformGraphics InitializeGraphics(iOSPlatformOptions opts)
        {
            if (opts.RenderingMode is null || !opts.RenderingMode.Any())
            {
                throw new InvalidOperationException($"{nameof(iOSPlatformOptions)}.{nameof(iOSPlatformOptions.RenderingMode)} must not be empty or null");
            }

            foreach (var renderingMode in opts.RenderingMode)
            {
#if !MACCATALYST
                if (renderingMode == iOSRenderingMode.OpenGl
                    && !OperatingSystem.IsMacCatalyst()
#pragma warning disable CA1422
                    && Eagl.EaglPlatformGraphics.TryCreate() is { } eaglGraphics)
#pragma warning restore CA1422
                {
                    return eaglGraphics;
                }
#endif

                if (renderingMode == iOSRenderingMode.Metal
                    && Metal.MetalPlatformGraphics.TryCreate() is { } metalGraphics)
                {
                    return metalGraphics;
                }
            }

            throw new InvalidOperationException($"{nameof(iOSPlatformOptions)}.{nameof(iOSPlatformOptions.RenderingMode)} has a value of \"{string.Join(", ", opts.RenderingMode)}\", but no options were applied.");
        }
    }
}

