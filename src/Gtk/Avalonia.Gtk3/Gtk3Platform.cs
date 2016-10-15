using System;
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
            App = Native.GtkApplicationNew("avalonia.app." + Guid.NewGuid(), 0);
            //Mark current thread as UI thread
            s_tlsMarker = true;

            AvaloniaLocator.CurrentMutable.Bind<IWindowingPlatform>().ToConstant(Instance)
                .Bind<IClipboard>().ToSingleton<ClipboardStub>()
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactoryStub())
                .Bind<IKeyboardDevice>().ToConstant(Keyboard)
                .Bind<IMouseDevice>().ToConstant(Mouse)
                .Bind<IPlatformSettings>().ToConstant(Instance)
                .Bind<IPlatformThreadingInterface>().ToConstant(Instance)
                .Bind<ISystemDialogImpl>().ToSingleton<SystemDialogStub>()
                .Bind<IPlatformIconLoader>().ToConstant(new PlatformIconLoaderStub());

        }

        public IWindowImpl CreateWindow() => new WindowImpl();

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
        }

        

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
            return null;
        }

        public void Signal()
        {
        }
        public event Action Signaled;


        [ThreadStatic]
        private static bool s_tlsMarker;

        public bool CurrentThreadIsLoopThread => s_tlsMarker;

    }

    public static class Gtk3AppBuilderExtensions
    {
        public static T UseGtk3<T>(this AppBuilderBase<T> builder) where T : AppBuilderBase<T>, new()
        => builder.UseWindowingSubsystem(Gtk3Platform.Initialize, "GTK3");
    }
}