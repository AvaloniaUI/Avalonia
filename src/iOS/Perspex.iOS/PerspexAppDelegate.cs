using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Perspex.Controls.Platform;
using Perspex.Input;
using Perspex.Input.Platform;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using Perspex.Skia;
using UIKit;

namespace Perspex.iOS
{
    public class PerspexAppDelegate : UIApplicationDelegate
    {
        static bool _initialized = false;
        internal static MouseDevice MouseDevice;
        internal static KeyboardDevice KeyboardDevice;

        protected void InitPerspex(Type appType)
        {
            if(_initialized)
                return;
            _initialized = true;
            
            var window = new UIWindow(UIScreen.MainScreen.Bounds);
            var controller = new PerspexViewController(window);
            window.RootViewController = controller;
            window.MakeKeyAndVisible();

            Application.RegisterPlatformCallback(() =>
            {
                MouseDevice = new MouseDevice();
                KeyboardDevice = new KeyboardDevice();
                SharedPlatform.Register(appType.Assembly);
                PerspexLocator.CurrentMutable
                    .Bind<IClipboard>().ToTransient<Clipboard>()
                    //.Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                    .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                    .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                    .Bind<IMouseDevice>().ToConstant(MouseDevice)
                    .Bind<IPlatformSettings>().ToSingleton<PlatformSettings>()
                    .Bind<IPlatformThreadingInterface>().ToConstant(new PlatformThreadingInterface())
                    .Bind<IWindowImpl>().ToConstant(controller.PerspexView);
                SkiaPlatform.Initialize();
            });
        }
    }
}
