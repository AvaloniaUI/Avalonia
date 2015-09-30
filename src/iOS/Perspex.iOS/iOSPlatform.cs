using Foundation;
using Perspex.Controls.Platform;
using Perspex.Input;
using Perspex.Input.Platform;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using Perspex.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;

namespace Perspex.iOS
{
    public class iOSPlatform : IPlatformThreadingInterface, IPlatformSettings
    {
        private static readonly iOSPlatform s_instance = new iOSPlatform();

        public iOSPlatform()
        {
        }

        // this does not make sense on iOS??
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);  // Gtk.Settings.Default.DoubleClickTime);

        public event Action Signaled;

        public static void Initialize()
        {
            PerspexLocator.CurrentMutable
                .Bind<IWindowImpl>().ToTransient<WindowImpl>()
                .Bind<IPopupImpl>().ToTransient<PopupImpl>()
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(CursorFactory.Instance)
                .Bind<IKeyboardDevice>().ToSingleton<iOSKeyboardDevice>()
                .Bind<IMouseDevice>().ToSingleton<iOSMouseDevice>()
                .Bind<IPlatformSettings>().ToConstant(s_instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(s_instance)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogImpl>();

            Rendering.iOSPlatformRender.Initialize();
            SharedPlatform.Register();
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            // noop on iOS
        }

        public void Signal()
        {
            EnsureInvokedOnMainThread(() => Signaled?.Invoke());
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            // hopefully this implementation is satisfactory, or do we need a
            // platform specific/native implementation?
            return Observable.Timer(interval, interval).Subscribe(_ => tick());
        }

        // We could share this out if necessary
        static NSObject Invoker = new NSObject();
        private static void EnsureInvokedOnMainThread(Action action)
        {
            if (NSThread.Current.IsMainThread)
            {
                action();
                return;
            }
            Invoker.BeginInvokeOnMainThread(() => action());
        }
    }
}
