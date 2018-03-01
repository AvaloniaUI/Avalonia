using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using MonoMac.AppKit;
using MonoMac.Foundation;
using MonoMac.ObjCRuntime;

namespace Avalonia.MonoMac
{
    public class MonoMacPlatform : IWindowingPlatform, IPlatformSettings
    {
        internal static MonoMacPlatform Instance { get; private set; }
        internal readonly MouseDevice MouseDevice = new MouseDevice();
        readonly KeyboardDevice _keyboardDevice = new KeyboardDevice();
        internal static NSApplication App;
        private static bool s_monoMacInitialized;
        private static bool s_showInDock = true;
        private static IRenderLoop s_renderLoop;

        void DoInitialize()
        {
            InitializeMonoMac();
            AvaloniaLocator.CurrentMutable
                .Bind<IStandardCursorFactory>().ToTransient<CursorFactoryStub>()
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IKeyboardDevice>().ToConstant(_keyboardDevice)
                .Bind<IMouseDevice>().ToConstant(MouseDevice)
                .Bind<IPlatformSettings>().ToConstant(this)
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogsImpl>()
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IRenderLoop>().ToConstant(s_renderLoop)
                .Bind<IPlatformThreadingInterface>().ToConstant(PlatformThreadingInterface.Instance);
        }

        public static void Initialize()
        {
            Instance = new MonoMacPlatform();
            Instance.DoInitialize();

        }


        /// <summary>
        /// See "Using POSIX Threads in a Cocoa Application" section here:
        /// https://developer.apple.com/library/content/documentation/Cocoa/Conceptual/Multithreading/CreatingThreads/CreatingThreads.html#//apple_ref/doc/uid/20000738-125024
        /// </summary>
        class ThreadHelper : NSObject
        {
            private readonly AutoResetEvent _event = new AutoResetEvent(false);
            private const string InitThreadingName = "initThreading";
            [Export(InitThreadingName)]
            public void DoNothing()
            {
                _event.Set();
            }

            public static void InitializeCocoaThreadingLocks()
            {
                var helper = new ThreadHelper();
                var thread = new NSThread(helper, Selector.FromHandle(Selector.GetHandle(InitThreadingName)), new NSObject());
                thread.Start();
                helper._event.WaitOne();
                helper._event.Dispose();
                if (!NSThread.IsMultiThreaded)
                {
                    throw new Exception("Unable to initialize Cocoa threading");
                }
            }
        }

        void InitializeMonoMac()
        {
            if(s_monoMacInitialized)
                return;
            NSApplication.Init();
            ThreadHelper.InitializeCocoaThreadingLocks();
            App = NSApplication.SharedApplication;
            UpdateActivationPolicy();
            s_renderLoop = new RenderLoop(); //TODO: use CVDisplayLink
            s_monoMacInitialized = true;
        }

        static void UpdateActivationPolicy() => App.ActivationPolicy = ShowInDock
            ? NSApplicationActivationPolicy.Regular
            : NSApplicationActivationPolicy.Accessory;

        public static bool ShowInDock
        {
            get => s_showInDock;
            set
            {
                s_showInDock = value;
                if (s_monoMacInitialized)
                    UpdateActivationPolicy();
            }
        }

        public static bool UseDeferredRendering { get; set; } = true;

        public Size DoubleClickSize => new Size(4, 4);
        public TimeSpan DoubleClickTime => TimeSpan.FromSeconds(NSEvent.DoubleClickInterval);

        public IWindowImpl CreateWindow() => new WindowImpl();

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new PlatformNotSupportedException();
        }

        public IPopupImpl CreatePopup()
        {
            return new PopupImpl();
        }
    }
}


namespace Avalonia
{
    public static class MonoMacPlatformExtensions
    {
        public static T UseMonoMac<T>(this T builder, bool? useDeferredRendering = null) where T : AppBuilderBase<T>, new()
        {
            if (useDeferredRendering.HasValue)
                MonoMac.MonoMacPlatform.UseDeferredRendering = useDeferredRendering.Value;
            return builder.UseWindowingSubsystem(MonoMac.MonoMacPlatform.Initialize, "MonoMac");
        }
    }
}