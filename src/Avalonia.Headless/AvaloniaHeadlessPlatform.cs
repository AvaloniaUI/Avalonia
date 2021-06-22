﻿using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;

namespace Avalonia.Headless
{
    public static class AvaloniaHeadlessPlatform
    {
        class RenderTimer : DefaultRenderTimer
        {
            private readonly int _framesPerSecond;
            private Action _forceTick; 
            protected override IDisposable StartCore(Action<TimeSpan> tick)
            {
                bool cancelled = false;
                var st = Stopwatch.StartNew();
                _forceTick = () => tick(st.Elapsed);
                DispatcherTimer.Run(() =>
                {
                    if (cancelled)
                        return false;
                    tick(st.Elapsed);
                    return !cancelled;
                }, TimeSpan.FromSeconds(1.0 / _framesPerSecond), DispatcherPriority.Render);
                return Disposable.Create(() =>
                {
                    _forceTick = null;
                    cancelled = true;
                });
            }

            public RenderTimer(int framesPerSecond) : base(framesPerSecond)
            {
                _framesPerSecond = framesPerSecond;
            }

            public void ForceTick() => _forceTick?.Invoke();
        }

        class HeadlessWindowingPlatform : IWindowingPlatform
        {
            public IWindowImpl CreateWindow() => new HeadlessWindowImpl(false);

            public IWindowImpl CreateEmbeddableWindow() => throw new PlatformNotSupportedException();

            public IPopupImpl CreatePopup() => new HeadlessWindowImpl(true);
        }
        
        internal static void Initialize()
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformThreadingInterface>().ToConstant(new HeadlessPlatformThreadingInterface())
                .Bind<IClipboard>().ToSingleton<HeadlessClipboardStub>()
                .Bind<ICursorFactory>().ToSingleton<HeadlessCursorFactoryStub>()
                .Bind<IPlatformSettings>().ToConstant(new HeadlessPlatformSettingsStub())
                .Bind<ISystemDialogImpl>().ToSingleton<HeadlessSystemDialogsStub>()
                .Bind<IPlatformIconLoader>().ToSingleton<HeadlessIconLoaderStub>()
                .Bind<IKeyboardDevice>().ToConstant(new KeyboardDevice())
                .Bind<IRenderLoop>().ToConstant(new RenderLoop())
                .Bind<IRenderTimer>().ToConstant(new RenderTimer(60))
                .Bind<IFontManagerImpl>().ToSingleton<HeadlessFontManagerStub>()
                .Bind<ITextShaperImpl>().ToSingleton<HeadlessTextShaperStub>()
                .Bind<IWindowingPlatform>().ToConstant(new HeadlessWindowingPlatform())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>();
        }


        public static void ForceRenderTimerTick(int count = 1)
        {
            var timer = AvaloniaLocator.Current.GetService<IRenderTimer>() as RenderTimer;
            for (var c = 0; c < count; c++)
                timer?.ForceTick();

        }
    }
    
    public static class AvaloniaHeadlessPlatformExtensions
    {
        public static T UseHeadless<T>(this T builder, bool headlessDrawing = true) 
            where T : AppBuilderBase<T>, new()
        {
            if (headlessDrawing)
                builder.UseRenderingSubsystem(HeadlessPlatformRenderInterface.Initialize, "Headless");
            return builder.UseWindowingSubsystem(AvaloniaHeadlessPlatform.Initialize, "Headless");
        }
    }
}
