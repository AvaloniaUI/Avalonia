using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Perspex.Controls.Platform;
using Perspex.Input.Platform;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;

namespace Perspex.Android
{
    public class AndroidPlatform : IPlatformThreadingInterface, IPlatformSettings
    {
        private static readonly AndroidPlatform instance = new AndroidPlatform();

        public static void Initialize()
        {
            PerspexLocator.CurrentMutable
                .Bind<IWindowImpl>().ToTransient<WindowImpl>()
                .Bind<IPopupImpl>().ToTransient<PopupImpl>()
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IPlatformSettings>().ToConstant(instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(instance)
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>();



            SharedPlatform.Register();
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            throw new NotImplementedException();
        }

        public void Signal()
        {
            throw new NotImplementedException();
        }

        public event Action Signaled;
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);
    }
}