﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Gtk3.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Gtk3;

namespace Avalonia.Gtk3
{
    public class Gtk3Platform : IWindowingPlatform, IPlatformSettings, IPlatformThreadingInterface
    {
        internal static readonly Gtk3Platform Instance = new Gtk3Platform();
        internal static readonly MouseDevice Mouse = new MouseDevice();
        internal static readonly KeyboardDevice Keyboard = new KeyboardDevice();
        internal static IntPtr App { get; set; }
        public static void Initialize()
        {
            Resolver.Resolve();
            Native.GtkInit(0, IntPtr.Zero);
            using (var utf = new Utf8Buffer("avalonia.app." + Guid.NewGuid()))
                App = Native.GtkApplicationNew(utf, 0);
            //Mark current thread as UI thread
            s_tlsMarker = true;

            AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>().ToConstant(Instance)
                .Bind<IClipboard>().ToSingleton<ClipboardImpl>()
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactory())
                .Bind<IKeyboardDevice>().ToConstant(Keyboard)
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(Instance)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialog>()
                .Bind<IRenderLoop>().ToConstant(new DefaultRenderLoop(60))
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoader());

        }

        public IWindowImpl CreateWindow() => new WindowImpl();

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup() => new PopupImpl();

        public Size DoubleClickSize => new Size(4, 4);

        public TimeSpan DoubleClickTime => TimeSpan.FromMilliseconds(100); //STUB
        public double RenderScalingFactor { get; } = 1;
        public double LayoutScalingFactor { get; } = 1;

        public void RunLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
                Native.GtkMainIteration();
        }

        public IDisposable StartTimer(TimeSpan interval, Action tick)
        {
            return GlibTimeout.StarTimer((uint) interval.TotalMilliseconds, tick);
        }

        private bool _signaled = false;
        object _lock = new object();

        public void Signal()
        {
            lock(_lock)
                if (!_signaled)
                {
                    _signaled = true;
                    GlibTimeout.Add(0, () =>
                    {
                        lock (_lock)
                        {
                            _signaled = false;
                        }
                        Signaled?.Invoke();
                        return false;
                    });
                }
        }
        public event Action Signaled;


        [ThreadStatic]
        private static bool s_tlsMarker;

        public bool CurrentThreadIsLoopThread => s_tlsMarker;

    }
}

namespace Avalonia
{
    public static class Gtk3AppBuilderExtensions
    {
        public static T UseGtk3<T>(this AppBuilderBase<T> builder, ICustomGtk3NativeLibraryResolver resolver = null) 
            where T : AppBuilderBase<T>, new()
        {
            Resolver.Custom = resolver;
            return builder.UseWindowingSubsystem(Gtk3Platform.Initialize, "GTK3");
        }
    }
}