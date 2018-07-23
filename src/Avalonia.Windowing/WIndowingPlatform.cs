using System;
using System.Collections.Generic;
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
        private readonly Dictionary<IntPtr, Window> _windows;

        public WindowingPlatform()
        {
            _eventsLoop = new EventsLoop();
            _eventsLoop.MouseEvent += _eventsLoop_MouseEvent;
            _eventsLoop.Awakened += _eventsLoop_Awakened;
            _eventsLoop.Resized += _eventsLoop_Resized;
            _windows = new Dictionary<IntPtr, Window>();
        }

        void _eventsLoop_Resized(IntPtr windowId, ResizeEvent resizeEvent)
        {
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Layout);
            if (_windows.ContainsKey(windowId)) 
            {
                _windows[windowId].OnResizeEvent(resizeEvent);    
            }
        }


        private void _eventsLoop_MouseEvent(IntPtr windowId, MouseEvent mouseEvent)
        {
            if (_windows.ContainsKey(windowId)) 
            {
                _windows[windowId].OnMouseEvent(mouseEvent);    
            }
        }

        private void _eventsLoop_Awakened()
        {
            Signaled?.Invoke(DispatcherPriority.Normal);
          //  Dispatcher.UIThread.RunJobs();
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
                .Bind<IMouseDevice>().ToConstant(new MouseDevice())
                .Bind<IPlatformIconLoader>().ToConstant(new IconLoader())
                .Bind<IStandardCursorFactory>().ToConstant(new CursorFactory())
                .Bind<IPlatformSettings>().ToConstant(new PlatformSettings())
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

        public IWindowImpl CreateWindow() 
        {
            var windowWrapper = new GlWindowWrapper(_eventsLoop);
            var window = new Window(windowWrapper);
            _windows.Add(windowWrapper.Id, window);

            return window;
        }

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
