using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Windowing.Bindings;
using static Avalonia.Windowing.Bindings.EventsLoop;

namespace Avalonia.Windowing
{
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
        private readonly Dictionary<IntPtr, WindowImpl> _windows;

        public WindowingPlatform()
        {
            _eventsLoop = new EventsLoop();
            _eventsLoop.OnMouseEvent += _eventsLoop_MouseEvent;
            _eventsLoop.OnKeyboardEvent += _eventsLoop_OnKeyboardEvent;
            _eventsLoop.OnCharacterEvent += _eventsLoop_OnCharacterEvent;
            _eventsLoop.OnAwakened += _eventsLoop_Awakened;
            _eventsLoop.OnResized += _eventsLoop_Resized;
            _windows = new Dictionary<IntPtr, WindowImpl>();
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

        void _eventsLoop_OnCharacterEvent(IntPtr windowId, CharacterEvent characterEvent)
        {
            if (_windows.ContainsKey(windowId))
            {
                _windows[windowId].OnCharacterEvent(characterEvent);
            }
        }


        void _eventsLoop_OnKeyboardEvent(IntPtr windowId, KeyboardEvent keyboardEvent)
        {
            if(_windows.ContainsKey(windowId))
            {
                _windows[windowId].OnKeyboardEvent(keyboardEvent);
            }
        }

        private bool _signaled;
        private void _eventsLoop_Awakened()
        {
            lock (this)
            {
                if (!_signaled)
                    return;
                _signaled = false;
            }

            Signaled?.Invoke(null);
        }


        public static void Initialize() 
        {
            Instance = new WindowingPlatform();
            Instance.DoInitialize();
        }

        private readonly IKeyboardDevice keyboardDevice = new KeyboardDevice();
        private readonly IMouseDevice mouseDevice = new MouseDevice();

        public void DoInitialize() 
        {
            AvaloniaLocator.CurrentMutable
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IKeyboardDevice>().ToConstant(keyboardDevice)
                .Bind<IMouseDevice>().ToConstant(mouseDevice)
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
            var windowWrapper = new GlWindowWrapper(_eventsLoop);
            var window = new PopupImpl(windowWrapper);
            _windows.Add(windowWrapper.Id, window);

            return window;
        }

        public IWindowImpl CreateWindow() 
        {
            var windowWrapper = new GlWindowWrapper(_eventsLoop);
            var window = new WindowImpl(windowWrapper);
            _windows.Add(windowWrapper.Id, window);

            return window;
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            _eventsLoop.Run();
        }

        public void Signal(DispatcherPriority priority)
        {
            lock (this)
            {
                if (_signaled)
                    return;
                _signaled = true;
            }

            _eventsLoop.Wakeup();
        }

        private IList<TimerDel> timerTickers = new List<TimerDel>();

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            // TODO: Need a way to cancel a timer when Dispose is called. 

           // var x = new TimerDel(tick);
           // timerTickers.Add(x);

         //   _eventsLoop.RunTimer(x);
            return Disposable.Create(() => {
         //       timerTickers.Remove(x);
            });
        }
    }
}
