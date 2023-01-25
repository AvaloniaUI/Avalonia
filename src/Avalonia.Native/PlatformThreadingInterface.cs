using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Threading;

namespace Avalonia.Native
{
    internal class PlatformThreadingInterface : IPlatformThreadingInterface
    {
        class TimerCallback : NativeCallbackBase, IAvnActionCallback
        {
            readonly Action _tick;

            public TimerCallback(Action tick)
            {
                _tick = tick;
            }

            public void Run()
            {
                _tick();
            }
        }

        class SignaledCallback : NativeCallbackBase, IAvnSignaledCallback
        {
            readonly PlatformThreadingInterface _parent;

            public SignaledCallback(PlatformThreadingInterface parent)
            {
                _parent = parent;
            }

            public void Signaled(int priority, int priorityContainsMeaningfulValue)
            {
                _parent.Signaled?.Invoke(priorityContainsMeaningfulValue.FromComBool() ? (DispatcherPriority?)priority : null);
            }
        }

        readonly IAvnPlatformThreadingInterface _native;
        private ExceptionDispatchInfo _exceptionDispatchInfo;
        private CancellationTokenSource _exceptionCancellationSource;

        public PlatformThreadingInterface(IAvnPlatformThreadingInterface native)
        {
            _native = native;
            using (var cb = new SignaledCallback(this))
                _native.SetSignaledCallback(cb);
        }

        public bool CurrentThreadIsLoopThread => _native.CurrentThreadIsLoopThread.FromComBool();

        public event Action<DispatcherPriority?> Signaled;

        public void RunLoop(CancellationToken cancellationToken)
        {
            _exceptionDispatchInfo?.Throw();
            var l = new object();
            _exceptionCancellationSource = new CancellationTokenSource();

            var compositeCancellation = CancellationTokenSource
                .CreateLinkedTokenSource(cancellationToken, _exceptionCancellationSource.Token).Token;

            var cancellation = _native.CreateLoopCancellation();
            compositeCancellation.Register(() =>
            {
                lock (l)
                {
                    cancellation?.Cancel();
                }
            });

            try
            {
                _native.RunLoop(cancellation);
            }
            finally
            {
                lock (l)
                {
                    cancellation?.Dispose();
                    cancellation = null;
                }
            }

            if (_exceptionDispatchInfo != null)
            {
                _exceptionDispatchInfo.Throw();
            }
        }

        public void DispatchException (ExceptionDispatchInfo exceptionInfo)
        {
            _exceptionDispatchInfo = exceptionInfo;
        }

        public void TerminateNativeApp()
        {
            _exceptionCancellationSource?.Cancel();
        }

        public void Signal(DispatcherPriority priority)
        {
            _native.Signal((int)priority);
        }

        public IDisposable StartTimer(DispatcherPriority priority, TimeSpan interval, Action tick)
        {
            using (var cb = new TimerCallback(tick))
                return _native.StartTimer((int)priority, (int)interval.TotalMilliseconds, cb);
        }
    }
}
