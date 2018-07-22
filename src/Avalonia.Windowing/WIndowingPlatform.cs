using System;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Windowing.Bindings;

namespace Avalonia.Windowing
{
    // Exposing C# Enums to Rust will again prove to be a massive PITA.
    public enum WinitEventType 
    {
        MouseMove    
    }

    public struct MouseMoveData
    {
    }

    public class DummyPlatformHandle : IPlatformHandle
    {
        public IntPtr Handle => IntPtr.Zero;

        public string HandleDescriptor => "Dummy";
    }


    public class CursorFactory : IStandardCursorFactory
    {
        public IPlatformHandle GetCursor(StandardCursorType cursorType)
        {
            return new DummyPlatformHandle();
        }
    }

    public class WindowingPlatform : IPlatformThreadingInterface, IWindowingPlatform
    {
        internal static WindowingPlatform Instance { get; private set; }
        private readonly EventsLoop _eventsLoop;

        public WindowingPlatform()
        {
            _eventsLoop = new EventsLoop();
        }

        public static void Initialize() 
        {
            Instance = new WindowingPlatform();
            Instance.DoInitialize();
        }

        public void DoInitialize() 
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IPlatformIconLoader>().ToConstant(new IconLoader())
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactory())
                .Bind<IPlatformThreadingInterface>().ToConstant(this);
        }

        public bool CurrentThreadIsLoopThread => true;
        public event Action<DispatcherPriority?> Signaled;

        public IEmbeddableWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IPopupImpl CreatePopup()
        {
            throw new NotImplementedException();
        }

        public IWindowImpl CreateWindow() => new Window(new GlWindowWrapper(_eventsLoop));

        public void RunLoop(CancellationToken cancellationToken)
        {
            _eventsLoop.Run();
        }

        public void Signal(DispatcherPriority priority)
        {
            // We need to run some sort of callback on wakeup don't we?
            _eventsLoop.Wakeup();
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return Disposable.Create(() => { });
        }
    }
}
