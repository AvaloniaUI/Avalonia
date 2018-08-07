using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Windowing.Bindings;

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
        private readonly Dictionary<WindowId, WindowImpl> _windows;
        private int _uiThreadId;

        public WindowingPlatform()
        {
            _eventsLoop = new EventsLoop();
            _eventsLoop.OnMouseEvent += _eventsLoop_MouseEvent;
            _eventsLoop.OnKeyboardEvent += _eventsLoop_OnKeyboardEvent;
            _eventsLoop.OnCharacterEvent += _eventsLoop_OnCharacterEvent;
            _eventsLoop.OnAwakened += _eventsLoop_Awakened;
            _eventsLoop.OnResized += _eventsLoop_Resized;
            _eventsLoop.OnShouldExitEventLoop += _eventsLoop_OnShouldExitEventLoop;
            _eventsLoop.OnCloseRequested += _eventsLoop_OnCloseRequested;
            _eventsLoop.OnFocused += _eventsLoop_OnFocused;
            _uiThreadId = Thread.CurrentThread.ManagedThreadId;
            _windows = new Dictionary<WindowId, WindowImpl>();
        }

        void _eventsLoop_OnCloseRequested(WindowId windowId)
        {
            if (_windows.ContainsKey(windowId)) 
            {
                if (!_windows[windowId].OnCloseRequested()) 
                {
                    _windows[windowId].Dispose();    
                }
            }
        }

        byte _eventsLoop_OnShouldExitEventLoop(WindowId windowId)
        {
            if (_windows.ContainsKey(windowId))
            {
                _windows[windowId].OnClosed();
                _windows.Remove(windowId);
            }

            return _windows.Any() ? (byte)0 : (byte)1;
        }

        void _eventsLoop_OnFocused(WindowId windowId, byte focused)
        {
            if (_windows.ContainsKey(windowId))
            {
                _windows[windowId].OnFocused(focused == 0 ? true : false);
            }
        }


        void _eventsLoop_Resized(WindowId windowId, ResizeEvent resizeEvent)
        {
            if (_windows.ContainsKey(windowId)) 
            {
                _windows[windowId].OnResizeEvent(resizeEvent);    
            }
        }


        private void _eventsLoop_MouseEvent(WindowId windowId, MouseEvent mouseEvent)
        {
            if (_windows.ContainsKey(windowId)) 
            {
                _windows[windowId].OnMouseEvent(mouseEvent);    
            }
        }

        void _eventsLoop_OnCharacterEvent(WindowId windowId, CharacterEvent characterEvent)
        {
            if (_windows.ContainsKey(windowId))
            {
                _windows[windowId].OnCharacterEvent(characterEvent);
            }
        }


        void _eventsLoop_OnKeyboardEvent(WindowId windowId, KeyboardEvent keyboardEvent)
        {
            if(_windows.ContainsKey(windowId))
            {
                _windows[windowId].OnKeyboardEvent(keyboardEvent);
            }
        }

        private bool _signaled;
        private void _eventsLoop_Awakened()
        {
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
                .Bind<IClipboard>().ToConstant(new ClipboardImpl())
                .Bind<ISystemDialogImpl>().ToConstant(new SystemDialogsImpl())
                .Bind<IPlatformThreadingInterface>().ToConstant(this);
        }

        public bool CurrentThreadIsLoopThread => _uiThreadId == Thread.CurrentThread.ManagedThreadId;
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
            var id = windowWrapper.Id;
            _windows.Add(id, window);

            return window;
        }

        public void RunLoop(CancellationToken cancellationToken)
        {
            // TODO: Support canceling the EventLoop here.
            _eventsLoop.Run();
        }

        public void Signal(DispatcherPriority priority)
        {
            _eventsLoop.Wakeup();
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            return new WinitTimer(new Timer(delegate
            {
                var tcs = new TaskCompletionSource<int>();
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        tick();
                    }
                    finally
                    {
                        tcs.SetResult(0);
                    }
                });

                tcs.Task.Wait();
            }, null, interval.Milliseconds, Timeout.Infinite)); 
        }
    }

	public class WinitTimer : IDisposable
	{
		private readonly Timer _timer;
        private readonly GCHandle _gcHandle;

        public WinitTimer(Timer timer) 
        {
            _timer = timer;
            _gcHandle = GCHandle.Alloc(_timer);
        }

        public void Dispose()
        {
            _timer.Dispose();
            _gcHandle.Free();
        }
	}
}
