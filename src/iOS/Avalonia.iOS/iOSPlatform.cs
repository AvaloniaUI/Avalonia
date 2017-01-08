using System;
using System.Reflection;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.iOS;
using Avalonia.Platform;
using Avalonia.Shared.PlatformSupport;
using Avalonia.Skia;
using UIKit;
using Avalonia.Controls;

namespace Avalonia
{
    public static class iOSApplicationExtensions
    {
        public static T UseiOS<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            builder.UseWindowingSubsystem(iOSPlatform.Initialize, "iOS");
            return builder;
        }

        // TODO: Can we merge this with UseSkia somehow once HW/platform cleanup is done?
        public static T UseSkiaViewHost<T>(this T builder) where T : AppBuilderBase<T>, new()
        {
            var window = new UIWindow(UIScreen.MainScreen.Bounds);
            var controller = new AvaloniaViewController(window);
            window.RootViewController = controller;
            window.MakeKeyAndVisible();

            AvaloniaLocator.CurrentMutable
                .Bind<IWindowingPlatform>().ToConstant(new WindowingPlatformImpl(controller.AvaloniaView));

            SkiaPlatform.Initialize();

            return builder;
        }
    }
}

namespace Avalonia.iOS
{
    // TODO: Perhaps we should make this class handle all these interfaces directly, like we 
    // do for Win32 and Gtk platforms
    //
    public class iOSPlatform //: IPlatformThreadingInterface, IPlatformSettings, IWindowingPlatform
    {
        internal static MouseDevice MouseDevice;
        internal static KeyboardDevice KeyboardDevice;

        public static void Initialize()
        {
            MouseDevice = new MouseDevice();
            KeyboardDevice = new KeyboardDevice();

            AvaloniaLocator.CurrentMutable
                .Bind<IRuntimePlatform>().ToSingleton<StandardRuntimePlatform>()
                .Bind<IClipboard>().ToTransient<Clipboard>()
                // TODO: what does this look like for iOS??
                //.Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IMouseDevice>().ToConstant(MouseDevice)
                .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                .Bind<IPlatformThreadingInterface>().ToConstant(PlatformThreadingInterface.Instance)
                .Bind<IPlatformIconLoader>().ToSingleton<PlatformIconLoader>();
        }
    }
}
