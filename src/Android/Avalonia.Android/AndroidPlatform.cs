using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Android;
using Avalonia.Android.Platform;
using Avalonia.Android.Platform.Input;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL.Egl;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.OpenGL;

namespace Avalonia
{
    public static class AndroidApplicationExtensions
    {
        public static AppBuilder UseAndroid(this AppBuilder builder)
        {
            return builder
                .UseAndroidRuntimePlatformSubsystem()
                .UseWindowingSubsystem(() => AndroidPlatform.Initialize(), "Android")
                .UseSkia();
        }
    }

    /// <summary>
    /// Represents the rendering mode for platform graphics.
    /// </summary>
    public enum AndroidRenderingMode
    {
        /// <summary>
        /// Avalonia is rendered into a framebuffer.
        /// </summary>
        Software = 1,

        /// <summary>
        /// Enables android EGL rendering.
        /// </summary>
        Egl = 2
    }

    public sealed class AndroidPlatformOptions
    {
        /// <summary>
        /// Gets or sets Avalonia rendering modes with fallbacks.
        /// The first element in the array has the highest priority.
        /// The default value is: <see cref="AndroidRenderingMode.Egl"/>, <see cref="AndroidRenderingMode.Software"/>.
        /// </summary>
        /// <remarks>
        /// If application should work on as wide range of devices as possible, at least add <see cref="AndroidRenderingMode.Software"/> as a fallback value.
        /// </remarks>
        /// <exception cref="System.InvalidOperationException">Thrown if no values were matched.</exception>
        public IReadOnlyList<AndroidRenderingMode> RenderingMode { get; set; } = new[]
        {
            AndroidRenderingMode.Egl, AndroidRenderingMode.Software
        };
    }
}

namespace Avalonia.Android
{
    class AndroidPlatform
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public static AndroidPlatformOptions Options { get; private set; }

        internal static Compositor Compositor { get; private set; }

        public static void Initialize()
        {
            Options = AvaloniaLocator.Current.GetService<AndroidPlatformOptions>() ?? new AndroidPlatformOptions();

            AvaloniaLocator.CurrentMutable
                .Bind<ICursorFactory>().ToTransient<CursorFactory>()
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IPlatformSettings>().ToSingleton<AndroidPlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(new AndroidThreadingInterface())
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoaderStub>()
                .Bind<IRenderTimer>().ToConstant(new ChoreographerTimer())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();

            var graphics = InitializeGraphics(Options);
            if (graphics is not null)
            {
                AvaloniaLocator.CurrentMutable.Bind<IPlatformGraphics>().ToConstant(graphics);
            }

            Compositor = new Compositor(graphics);
            AvaloniaLocator.CurrentMutable.Bind<Compositor>().ToConstant(Compositor);
        }
        
        private static IPlatformGraphics InitializeGraphics(AndroidPlatformOptions opts)
        {
            if (opts.RenderingMode is null || !opts.RenderingMode.Any())
            {
                throw new InvalidOperationException($"{nameof(AndroidPlatformOptions)}.{nameof(AndroidPlatformOptions.RenderingMode)} must not be empty or null");
            }

            foreach (var renderingMode in opts.RenderingMode)
            {
                if (renderingMode == AndroidRenderingMode.Software)
                {
                    return null;
                }

                if (renderingMode == AndroidRenderingMode.Egl)
                {
                    if (EglPlatformGraphics.TryCreate() is { } egl)
                    {
                        return egl;
                    }
                }
            }

            throw new InvalidOperationException($"{nameof(AndroidPlatformOptions)}.{nameof(AndroidPlatformOptions.RenderingMode)} has a value of \"{string.Join(", ", opts.RenderingMode)}\", but no options were applied.");
        }
    }
}
