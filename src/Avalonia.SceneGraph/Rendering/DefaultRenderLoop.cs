// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines a default render loop that uses a standard timer.
    /// </summary>
    /// <remarks>
    /// This class may be overridden by platform implementations to use a specialized timer
    /// implementation.
    /// </remarks>
    public class DefaultRenderLoop : IRenderLoop
    {
        private IPlatformThreadingInterface _threading;
        private int _subscriberCount;
        private EventHandler<EventArgs> _tick;
        private IDisposable _subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRenderLoop"/> class.
        /// </summary>
        /// <param name="framesPerSecond">
        /// The number of frames per second at which the loop should run.
        /// </param>
        public DefaultRenderLoop(int framesPerSecond)
        {
            FramesPerSecond = framesPerSecond;
        }

        /// <summary>
        /// Gets the number of frames per second at which the loop runs.
        /// </summary>
        public int FramesPerSecond { get; }

        /// <inheritdoc/>
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

        /// <summary>
        /// Starts the timer.
        /// </summary>
        protected void Start()
        {
            _subscription = StartCore();
        }

        /// <summary>
        /// Provides the implementation of starting the timer.
        /// </summary>
        /// <remarks>
        /// This can be overridden by platform implementations to use a specialized timer
        /// implementation.
        /// </remarks>
        protected virtual IDisposable StartCore()
        {
            if (_threading == null)
            {
                _threading = AvaloniaLocator.Current.GetService<IPlatformThreadingInterface>();
            }

            return _threading.StartTimer(TimeSpan.FromSeconds(1.0 / FramesPerSecond), InternalTick);
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
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
