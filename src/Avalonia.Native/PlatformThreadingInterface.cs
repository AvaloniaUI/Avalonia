// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Threading;
using SharpGen.Runtime;

namespace Avalonia.Native
{
    public class PlatformThreadingInterface : IPlatformThreadingInterface
    {
        class TimerCallback : CallbackBase, IAvnActionCallback
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

        class SignaledCallback : CallbackBase, IAvnSignaledCallback
        {
            readonly PlatformThreadingInterface _parent;

            public SignaledCallback(PlatformThreadingInterface parent)
            {
                _parent = parent;
            }

            public void Signaled(int priority, bool priorityContainsMeaningfulValue)
            {
                _parent.Signaled?.Invoke(priorityContainsMeaningfulValue ? (DispatcherPriority?)priority : null);
            }
        }

        readonly IAvnPlatformThreadingInterface _native;
        private ExceptionDispatchInfo _exceptionDispatchInfo;
        private IAvnLoopCancellation _loopCancellation;

        public PlatformThreadingInterface(IAvnPlatformThreadingInterface native)
        {
            _native = native;
            using (var cb = new SignaledCallback(this))
                _native.SignaledCallback = cb;
        }

        public bool CurrentThreadIsLoopThread => _native.CurrentThreadIsLoopThread;

        public event Action<DispatcherPriority?> Signaled;

        public void RunLoop(CancellationToken cancellationToken)
        {
            var l = new object();
            _loopCancellation = _native.CreateLoopCancellation();
            cancellationToken.Register(() =>
            {
                lock (l)
                {
                    _loopCancellation?.Cancel();
                }
            });
            try
            {
                _native.RunLoop(_loopCancellation);
            }
            finally
            {
                lock (l)
                {
                    _loopCancellation?.Dispose();
                    _loopCancellation = null;
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
            _loopCancellation?.Cancel();
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
