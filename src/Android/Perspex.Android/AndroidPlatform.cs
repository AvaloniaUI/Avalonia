using System;
using System.Reactive.Linq;
using System.Threading;
using Android.OS;
using Perspex.Android.Input;
using Perspex.Android.Rendering;
using Perspex.Controls.Platform;
using Perspex.Input;
using Perspex.Input.Platform;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;

namespace Perspex.Android
{
    public class AndroidPlatform : IPlatformThreadingInterface, IPlatformSettings
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);

        public void RunLoop(CancellationToken cancellationToken)
        {
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            return Observable.Timer(interval, interval).Subscribe(_ => tick());
        }

        public void Signal()
        {
            EnsureInvokeOnMainThread(() => Signaled?.Invoke());
        }

        public event Action Signaled;

        public static void Initialize()
        {
            PerspexLocator.CurrentMutable
                .Bind<IWindowImpl>().ToSingleton<PerspexView>()
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IMouseDevice>().ToSingleton<AndroidMouseDevice>()
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(Instance)
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>();

            AndroidPlatformRender.Initialize();
            SharedPlatform.Register();
        }


        private void EnsureInvokeOnMainThread(Action action)
        {
            var mainHandler = new Handler(global::Android.App.Application.Context.MainLooper);
            mainHandler.Post(action);
        }
    }
}