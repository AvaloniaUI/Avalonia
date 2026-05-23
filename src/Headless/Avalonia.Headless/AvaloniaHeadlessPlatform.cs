using System;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using System.Collections.Generic;

namespace Avalonia.Headless
{
    public static class AvaloniaHeadlessPlatform
    {
        internal static Compositor? Compositor { get; private set; }
        private static IRenderTimer? s_renderTimer;

        private class HeadlessWindowingPlatform(PixelFormat frameBufferFormat) : IWindowingPlatform
        {
            public IWindowImpl CreateWindow() => new HeadlessWindowImpl(false, frameBufferFormat);
            public ITopLevelImpl CreateEmbeddableTopLevel() => CreateEmbeddableWindow();

            public IWindowImpl CreateEmbeddableWindow() => throw new PlatformNotSupportedException();

            public ITrayIconImpl? CreateTrayIcon() => null;

            public void GetWindowsZOrder(ReadOnlySpan<IWindowImpl> windows, Span<long> zOrder)
            {
                for (var i = 0; i < windows.Length; ++i)
                {
                    zOrder[i] = (windows[i] as HeadlessWindowImpl)?.ZOrder ?? 0;
                }
            }
        }

        internal static void Initialize(AvaloniaHeadlessPlatformOptions opts)
        {
            var clipboardImpl = new HeadlessClipboardImplStub();
            var clipboard = new Clipboard(clipboardImpl);

            s_renderTimer = opts.ShouldRenderOnUIThread
                ? new HeadlessRenderTimer(opts.Fps)
                : new SleepLoopRenderTimer(opts.Fps);

            AvaloniaLocator.CurrentMutable
                .Bind<IClipboardImpl>().ToConstant(clipboardImpl)
                .Bind<IClipboard>().ToConstant(clipboard)
                .Bind<ICursorFactory>().ToSingleton<HeadlessCursorFactoryStub>()
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IPlatformIconLoader>().ToSingleton<HeadlessIconLoaderStub>()
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<IRenderLoop>().ToConstant(Rendering.RenderLoop.FromTimer(s_renderTimer))
                .Bind<IWindowingPlatform>().ToConstant(new HeadlessWindowingPlatform(opts.FrameBufferFormat))
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<KeyGestureFormatInfo>().ToConstant(new KeyGestureFormatInfo(new Dictionary<Key, string>() { }));
            Compositor = new Compositor( null);
        }

        /// <summary>
        /// Forces renderer to process a rendering timer tick.
        /// Use this method before calling <see cref="HeadlessWindowExtensions.GetLastRenderedFrame"/>. 
        /// </summary>
        /// <param name="count">Count of frames to be ticked on the timer.</param>
        /// <exception cref="NotSupportedException">Thrown when the current render timer setup doesn't support forcing ticks.</exception>
        public static void ForceRenderTimerTick(int count = 1)
        {
            if (s_renderTimer is SleepLoopRenderTimer)
                throw new NotSupportedException(
                    "Can't force render timer tick with current setup." +
                    "Set ShouldRenderOnUIThread to true in AvaloniaHeadlessPlatformOptions to enable ForceRenderTimerTick.");

            ForceRenderTimerTickCore();
        }

        internal static void ForceRenderTimerTickCore(int count = 1)
        {
            if (s_renderTimer is not HeadlessRenderTimer timer)
            {
                return;
            }

            for (var c = 0; c < count; c++)
            {
                timer.ForceTick();
            }
        }
    }

    /// <summary>
    /// Options for configuring the Avalonia headless platform.
    /// </summary>
    public class AvaloniaHeadlessPlatformOptions
    {
        /// <summary>
        /// Gets or sets the number of frames per second at which the renderer should run.
        /// Default 60.
        /// </summary>
        public int Fps { get; set; } = 60;

        /// <summary>
        /// Render directly on the UI thread instead of using a dedicated render thread.
        /// This can be usable if your device don't have multiple cores to begin with.
        /// This setting is false by default.
        /// </summary>
        /// <remarks>
        /// Disabling this option will make <see cref="AvaloniaHeadlessPlatform.ForceRenderTimerTick"/> unusable,
        /// and the renderer will tick based on the internal timer, so it can be less deterministic in tests.
        /// </remarks>
        public bool ShouldRenderOnUIThread { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to use headless drawing mode, which allows rendering without creating an actual window.
        /// </summary>
        /// <remarks>
        /// Disable this option if you are using Avalonia.Skia or another drawing backend.
        /// </remarks>
        public bool UseHeadlessDrawing { get; set; } = true;

        /// <summary>
        /// Gets or sets the pixel format to be used for the headless Window framebuffers.
        /// </summary>
        public PixelFormat FrameBufferFormat { get; set; } = PixelFormat.Rgba8888;
    }

    public static class AvaloniaHeadlessPlatformExtensions
    {
        public static AppBuilder UseHeadless(this AppBuilder builder, AvaloniaHeadlessPlatformOptions opts)
        {
            if(opts.UseHeadlessDrawing)
                builder = builder.UseRenderingSubsystem(HeadlessPlatformRenderInterface.Initialize, "Headless");
            return builder
                .UseStandardRuntimePlatformSubsystem()
                .UseWindowingSubsystem(() => AvaloniaHeadlessPlatform.Initialize(opts), "Headless")
                .UseHarfBuzz();
        }
    }
}
