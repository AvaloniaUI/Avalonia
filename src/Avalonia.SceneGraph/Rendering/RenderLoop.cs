// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;

namespace Avalonia.Rendering
{
    public class RenderLoop : IRenderLoop
    {
        private readonly int _fps;
        private IPlatformThreadingInterface _threading;
        private int _subscriberCount;
        private EventHandler<EventArgs> _tick;
        private IDisposable _subscription;

        public RenderLoop(int fps)
        {
            _fps = fps;
        }

        public event EventHandler<EventArgs> Tick
        {
            add
            {
                if (_subscriberCount++ == 0)
                {
                    Start();
                }

                _tick += value;
            }

            remove
            {
                if (--_subscriberCount == 0)
                {
                    Stop();
                }

                _tick -= value;
            }
        }

        protected void Start()
        {
            if (_threading == null)
            {
                _threading = AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>();
            }

            _subscription = _threading.StartTimer(TimeSpan.FromSeconds(1.0 / _fps), InternalTick);
        }

        protected void Stop()
        {
            _subscription.Dispose();
            _subscription = null;
        }

        private void InternalTick()
        {
            _tick(this, EventArgs.Empty);
        }
    }
}
