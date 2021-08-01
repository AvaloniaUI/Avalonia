using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.OpenGL;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Shared.PlatformSupport;

namespace Avalonia.iOS
{
    static class Platform
    {
        public static EaglFeature GlFeature;
        public static DisplayLinkTimer Timer;
        class PlatformSettings : IPlatformSettings
        {
            public Size DoubleClickSize { get; } = new Size(10, 10);
            public TimeSpan DoubleClickTime { get; } = TimeSpan.FromMilliseconds(500);
        }
        
        public static void Register()
        {
            GlFeature ??= new EaglFeature();
            Timer ??= new DisplayLinkTimer();
            var keyboard = new KeyboardDevice();
            var softKeyboard = new SoftKeyboardHelper();
            AvaloniaLocator.CurrentMutable
                .Bind<IPlatformOpenGlInterface>().ToConstant(GlFeature)
                .Bind<ICursorFactory>().ToConstant(new CursorFactoryStub())
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformStub())
                .Bind<IClipboard>().ToConstant(new ClipboardImpl())
                .Bind<IPlatformSettings>().ToConstant(new PlatformSettings())
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoaderStub())
                .Bind<PlatformHotkeyConfiguration>().ToSingleton<PlatformHotkeyConfiguration>()
                .Bind<IRenderLoop>().ToSingleton<RenderLoop>()
                .Bind<IRenderTimer>().ToConstant(Timer)
                .Bind<IPlatformThreadingInterface>().ToConstant(new PlatformThreadingInterface())
                .Bind<IKeyboardDevice>().ToConstant(keyboard);
            keyboard.PropertyChanged += (_, changed) =>
            {
                if (changed.PropertyName == nameof(KeyboardDevice.FocusedElement))
                    softKeyboard.UpdateKeyboard(keyboard.FocusedElement);
            };
        }


    }
}

