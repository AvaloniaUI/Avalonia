﻿// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Platform;

namespace Avalonia.Rendering
{
    /// <summary>
    /// Defines a default render timer that uses a standard timer.
    /// </summary>
    /// <remarks>
    /// This class may be overridden by platform implementations to use a specialized timer
    /// implementation.
    /// </remarks>
    public class DefaultRenderTimer : IRenderTimer
    {
        private IRuntimePlatform _runtime;
        private int _subscriberCount;
        private long _tickStartTimeStamp;
        private Action<long> _tick;
        private IDisposable _subscription;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRenderTimer"/> class.
        /// </summary>
        /// <param name="framesPerSecond">
        /// The number of frames per second at which the loop should run.
        /// </param>
        public DefaultRenderTimer(int framesPerSecond)
        {
            FramesPerSecond = framesPerSecond;
        }

        /// <summary>
        /// Gets the number of frames per second at which the loop runs.
        /// </summary>
        public int FramesPerSecond { get; }

        /// <inheritdoc/>
        public event Action<long> Tick
        {
            add
            {
                _tick += value;

                if (_subscriberCount++ == 0)
                {
                    Start();
                }
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
            _tickStartTimeStamp = Stopwatch.GetTimestamp();
            _subscription = StartCore(InternalTick);
        }

        /// <summary>
        /// Provides the implementation of starting the timer.
        /// </summary>
        /// <param name="tick">The method to call on each tick.</param>
        /// <remarks>
        /// This can be overridden by platform implementations to use a specialized timer
        /// implementation.
        /// </remarks>
        protected virtual IDisposable StartCore(Action<long> tick)
        {
            if (_runtime == null)
            {
                _runtime = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            }

            return _runtime.StartSystemTimer(TimeSpan.FromSeconds(1.0 / FramesPerSecond),
                                             () => tick(TimeStampToFrames()));
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        protected void Stop()
        {
            _subscription.Dispose();
            _subscription = null;
        }

        protected long TimeStampToFrames()
                     => (Stopwatch.GetTimestamp() - _tickStartTimeStamp)
                      / (Stopwatch.Frequency / FramesPerSecond);

        private void InternalTick(long tickCount)
        {
            _tick(tickCount);
        }
    }
}
