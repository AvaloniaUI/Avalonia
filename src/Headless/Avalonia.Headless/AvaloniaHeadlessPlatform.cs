using System;
using System.Diagnostics;
using Avalonia.Controls.Platform;
using Avalonia.Reactive;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;

namespace Avalonia.Headless
{
    public static class AvaloniaHeadlessPlatform
    {
        internal static Compositor? Compositor { get; private set; }

        private class RenderTimer : DefaultRenderTimer
        {
            private readonly int _framesPerSecond;
            private Action? _forceTick; 
            protected override IDisposable StartCore(Action<TimeSpan> tick)
            {
                var st = Stopwatch.StartNew();
                _forceTick = () => tick(st.Elapsed);

                var timer = new DispatcherTimer(DispatcherPriority.UiThreadRender)
                {
                    Interval = TimeSpan.FromSeconds(1.0 / _framesPerSecond),
                    Tag = "HeadlessRenderTimer"
                };
                timer.Tick += (s, e) => tick(st.Elapsed);
                timer.Start();

                return Disposable.Create(() =>
                {
                    _forceTick = null;
                    timer.Stop();
                });
            }

            public RenderTimer(int framesPerSecond) : base(framesPerSecond)
            {
                _framesPerSecond = framesPerSecond;
            }

            public override bool RunsInBackground => false;

            public void ForceTick() => _forceTick?.Invoke();
        }

        private class HeadlessWindowingPlatform : IWindowingPlatform
        {
            readonly PixelFormat _frameBufferFormat;
            public HeadlessWindowingPlatform(PixelFormat frameBufferFormat)
            {
                _frameBufferFormat = frameBufferFormat;
            }
            public IWindowImpl CreateWindow() => new HeadlessWindowImpl(false, _frameBufferFormat);

            public IWindowImpl CreateEmbeddableWindow() => throw new PlatformNotSupportedException();

            public IPopupImpl CreatePopup() => new HeadlessWindowImpl(true, _frameBufferFormat);

            public ITrayIconImpl? CreateTrayIcon() => null;
        }
        
        internal static void Initialize(AvaloniaHeadlessPlatformOptions opts)
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IDispatcherImpl>().ToConstant(new ManagedDispatcherImpl(null))
                .Bind<IClipboard>().ToSingleton<HeadlessClipboardStub>()
                .Bind<ICursorFactory>().ToSingleton<HeadlessCursorFactoryStub>()
                .Bind<IPlatformSettings>().ToSingleton<DefaultPlatformSettings>()
                .Bind<IPlatformIconLoader>().ToSingleton<HeadlessIconLoaderStub>()
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<IRenderTimer>().ToConstant(new RenderTimer(60))
                .Bind<IWindowingPlatform>().ToConstant(new HeadlessWindowingPlatform(opts.FrameBufferFormat))
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();
            Compositor = new Compositor( null);
        }

        /// <summary>
        /// Forces renderer to process a rendering timer tick.
        /// Use this method before calling <see cref="HeadlessWindowExtensions.GetLastRenderedFrame"/>. 
        /// </summary>
        /// <param name="count">Count of frames to be ticked on the timer.</param>
        public static void ForceRenderTimerTick(int count = 1)
        {
            var timer = AvaloniaLocator.Current.GetService<IRenderTimer>() as RenderTimer;
            for (var c = 0; c < count; c++)
                timer?.ForceTick();

        }
    }

    public class AvaloniaHeadlessPlatformOptions
    {
        public bool UseHeadlessDrawing { get; set; } = true;
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
                .UseWindowingSubsystem(() => AvaloniaHeadlessPlatform.Initialize(opts), "Headless");
        }
    }
}
