using Perspex.Controls.Platform;
using Perspex.Input;
using Perspex.Input.Platform;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;

namespace Perspex.iOS
{
    public class iOSPlatform : IPlatformThreadingInterface, IPlatformSettings
    {
        private static readonly iOSPlatform s_instance = new iOSPlatform();

        public iOSPlatform()
        {
            //Gtk.Application.Init();
        }

        // this does not make sense on iOS??
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);  // Gtk.Settings.Default.DoubleClickTime);

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

        public bool HasMessages()
        {
            return false; // Gtk.Application.EventsPending();
        }

        public void ProcessMessage()
        {
            //Gtk.Application.RunIteration();
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            var result = true;

            //var handle = GLib.Timeout.Add(
            //    (uint)interval.TotalMilliseconds,
            //    () =>
            //    {
            //        tick();
            //        return result;
            //    });

            return Disposable.Create(() => result = false);
        }

        public void Wake()
        {
        }
    }
}
