using Android.OS;
using Perspex.Android.CanvasRendering;
using Perspex.Android.Platform;
using Perspex.Android.Platform.CanvasPlatform;
using Perspex.Android.Platform.Input;
using Perspex.Android.Platform.Specific;
using Perspex.Android.Platform.Specific.Helpers;
using Perspex.Android.PlatformSupport;
using Perspex.Animation;
using Perspex.Controls.Platform;
using Perspex.Input;
using Perspex.Input.Platform;
using Perspex.Platform;
using Perspex.Shared.PlatformSupport;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Perspex.Android
{
    public class AndroidPlatform : IPlatformThreadingInterface, IPlatformSettings
    {
        public static readonly AndroidPlatform Instance = new AndroidPlatform();
        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(200);

        private HashSet<int> _renderingTask = new HashSet<int>();

        public void RegisterRenderingTaskId(int taskId)
        {
            _renderingTask.Add(taskId);
        }

        public void UnRegisterRenderingTaskId(int taskId)
        {
            _renderingTask.Add(taskId);
        }

        public bool CurrentThreadIsLoopThread
        {
            get
            {
                bool res = Looper.MainLooper.Thread.Equals(Java.Lang.Thread.CurrentThread());
                if (!res)
                {
                    if (Task.CurrentId != null)
                    {
                        int id = Task.CurrentId.Value;
                        res = _renderingTask.Contains(Task.CurrentId.Value);
                    }
                }

                return res;
            }
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
        }

        private TimeSpan _animationTick = TimeSpan.FromSeconds(1.0 / Animate.FramesPerSecond);

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            //we should be carefull about the smallest amount of time the timer ticks
            //as on android for animations so far there is some issue with 60 fps
            //for now we can get set it to 16 fps with observable timer
            //and still random native exceptions causing app to stop!!!!
            //better not use it
            //return Observable.Timer(interval, interval).Subscribe(_ => tick());

            //System.Threading.Timer
            //int ms = (int)interval.TotalMilliseconds;
            ////regular timer is working perfect
            //return new System.Threading.Timer(_ => tick(), null, ms, ms);

            //System.Timers.Timer
            if (interval.TotalMilliseconds == 0)
            {
                //android ui thread
                PerspexLocator.Current.GetService<IAndroidActivity>().Activity.RunOnUiThread(tick);
                return Disposable.Empty;
            }

            if (OverrideAnimateFramesPerSecond > 0)
            {
                if (_animationTick >= interval)
                {
                    interval = TimeSpan.FromSeconds(1.0 / OverrideAnimateFramesPerSecond);
                }
            }

            //working not very bad with standard timer
            //var timer = new System.Timers.Timer(interval.TotalMilliseconds);
            //System.Timers.ElapsedEventHandler elapsed = (s, e) => tick();
            //timer.Elapsed += elapsed;
            //timer.Start();
            //return Disposable.Create(() =>
            //{
            //    timer.Stop();
            //    timer.Elapsed -= elapsed;
            //    timer.Dispose();
            //});

            //common ui timer is optimizing performance
            //a lot almost twice, also may be processor usage
            //most probably because it optimizes invalidation calls in animations and using less system resources
            CommonUITimer timer;
            if (!_commonTimers.TryGetValue(interval, out timer))
            {
                timer = new CommonUITimer(interval);
                _commonTimers.Add(interval, timer);
            }

            timer.AddAction(tick);

            return Disposable.Create(() =>
            {
                timer.RemoveAction(tick);

                if (timer.ActiveActionsCount == 0)
                {
                    timer.Dispose();
                    _commonTimers.Remove(interval);
                }
            });
        }

        private Dictionary<TimeSpan, CommonUITimer> _commonTimers = new Dictionary<TimeSpan, CommonUITimer>();

        public void Signal()
        {
            EnsureInvokeOnMainThread(() => Signaled?.Invoke());
        }

        public event Action Signaled;

        public static void Initialize()
        {
            PerspexLocator.CurrentMutable
                .Bind<IClipboard>().ToTransient<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactory>()
                .Bind<IKeyboardDevice>().ToSingleton<AndroidKeyboardDevice>()
                .Bind<IMouseDevice>().ToSingleton<AndroidMouseDevice>()
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(Instance)
                .Bind<ISystemDialogImpl>().ToTransient<SystemDialogImpl>()
                .Bind<IAssetLoader>().ToSingleton<AndroidAssetLoader>()
                .Bind<ITopLevelRenderer>().ToTransient<AndroidTopLevelRenderer>()
                ;
            AndroidPlatformRender.Initialize();

            Instance.RegisterViewDrawType();
            //set defaults for simple resources
            //Assembly.GetEntryAssembly(); //not working on mono for android
            var appType = Application.Current.GetType();
            var entryAssembly = appType.Assembly;
            AndroidPlatform.Instance.InitializeAssetsLoader(entryAssembly, appType.Namespace + ".");
            // SharedPlatform.Register();
            //we have custom Assetloader so no need to overwrite it
            PerspexLocator.CurrentMutable
              .Bind<IPclPlatformWrapper>().ToSingleton<PclPlatformWrapper>();
        }

        public void RegisterViewDrawType()
        {
            if (Instance.DefaultViewDrawType == ViewDrawType.SurfaceViewCanvasOnDraw)
            {
                PerspexLocator.CurrentMutable.Bind<IWindowImpl>().ToSingleton<Platform.CanvasPlatform.SurfaceMainWindowImpl>();
            }
            else
            {
                PerspexLocator.CurrentMutable.Bind<IWindowImpl>().ToSingleton<Platform.CanvasPlatform.MainWindowImpl>();
            }

            PerspexLocator.CurrentMutable.Bind<IPopupImpl>().ToTransient<Platform.CanvasPlatform.PopupImpl>();
        }

        public void RegisterViewPointUnits()
        {
            PerspexLocator.CurrentMutable.Bind<IPointUnitService>().ToSingleton<PointUnitService>();
        }

        public void InitializeAssetsLoader(Assembly assembly, string defaultResourcePrefix)
        {
            var assetLoder = PerspexLocator.Current.GetService<IAssetLoader>() as AndroidAssetLoader;
            if (assetLoder != null)
            {
                assetLoder.DefaultAssetAssembly = assembly;
                assetLoder.DefaultResourcePrefix = defaultResourcePrefix;
            }
        }

        private void EnsureInvokeOnMainThread(Action action)
        {
            var mainHandler = new Handler(global::Android.App.Application.Context.MainLooper);
            mainHandler.Post(action);
        }

        public bool DrawDebugInfo { get; set; } = false;

        /// <summary>
        /// Default View DrawType
        /// WindowImpl supports:
        ///     ViewDrawType.CanvasOnDraw
        ///     ViewDrawType.BitmapOnPreDraw
        ///     BitmapBackgroundRender
        /// by default it is ViewDrawType.CanvasOnDraw simples way of drawing view
        /// SurfaceWindowImpl supports:
        ///     SurfaceViewCanvasOnDraw
        /// </summary>
        public ViewDrawType DefaultViewDrawType { get; set; } = ViewDrawType.SurfaceViewCanvasOnDraw;

        public PointUnit DefaultPointUnit { get; set; } = PointUnit.DP;

        public int OverrideAnimateFramesPerSecond { get; set; } = -1;
    }
}