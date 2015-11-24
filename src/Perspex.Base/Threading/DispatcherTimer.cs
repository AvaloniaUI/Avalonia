// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Disposables;
using Perspex.Platform;

namespace Perspex.Threading
{
    /// <summary>
    /// A timer that uses a <see cref="Dispatcher"/> to fire at a specified interval.
    /// </summary>
    public class DispatcherTimer
    {
        private IDisposable _timer;

        private readonly DispatcherPriority _priority;

        private TimeSpan _interval;

        private readonly Action _raiseTickAction;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        public DispatcherTimer() : this(DispatcherPriority.Normal)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        /// <param name="priority">The priority to use.</param>
        /// <param name="dispatcher">The dispatcher to use.</param>
        public DispatcherTimer(DispatcherPriority priority)
        {
            _priority = priority;
            _raiseTickAction = RaiseTick;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        /// <param name="interval">The interval at which to tick.</param>
        /// <param name="priority">The priority to use.</param>
        /// <param name="dispatcher">The dispatcher to use.</param>
        /// <param name="callback">The event to call when the timer ticks.</param>
        public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback) : this(priority)
        {
            _priority = priority;
            Interval = interval;
            Tick += callback;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        ~DispatcherTimer()
        {
            if (_timer != null)
            {
                Stop();
            }
        }

        /// <summary>
        /// Raised when the timer ticks.
        /// </summary>
        public event EventHandler Tick;

        /// <summary>
        /// Gets or sets the interval at which the timer ticks.
        /// </summary>
        public TimeSpan Interval
        {
            get
            {
                return _interval;
            }

            set
            {
                bool enabled = IsEnabled;
                Stop();
                _interval = value;
                IsEnabled = enabled;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the timer is running.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return _timer != null;
            }

            set
            {
                if (IsEnabled != value)
                {
                    if (value)
                    {
                        Start();
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets user-defined data associated with the timer.
        /// </summary>
        public object Tag
        {
            get;
            set;
        }

        /// <summary>
        /// Starts a new timer.
        /// </summary>
        /// <param name="action">
        /// The method to call on timer tick. If the method returns false, the timer will stop.
        /// </param>
        /// <param name="interval">The interval at which to tick.</param>
        /// <param name="priority">The priority to use.</param>
        /// <returns>An <see cref="IDisposable"/> used to cancel the timer.</returns>
        public static IDisposable Run(Func<bool> action, TimeSpan interval, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var timer = new DispatcherTimer(priority);

            timer.Interval = interval;
            timer.Tick += (s, e) =>
            {
                if (!action())
                {
                    timer.Stop();
                }
            };

            timer.Start();

            return Disposable.Create(() => timer.Stop());
        }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        public void Start()
        {
            if (!IsEnabled)
            {
                IPlatformThreadingInterface threading = PerspexLocator.Current.GetService<IPlatformThreadingInterface>();
                _timer = threading.StartTimer(Interval, InternalTick);
            }
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            if (IsEnabled)
            {
                IPlatformThreadingInterface threading = PerspexLocator.Current.GetService<IPlatformThreadingInterface>();
                _timer.Dispose();
                _timer = null;
            }
        }

        

        /// <summary>
        /// Raises the <see cref="Tick"/> event on the dispatcher thread.
        /// </summary>
        private void InternalTick()
        {
            Dispatcher.UIThread.InvokeAsync(_raiseTickAction, _priority);
        }

        /// <summary>
        /// Raises the <see cref="Tick"/> event.
        /// </summary>
        private void RaiseTick()
        {
            if (Tick != null)
            {
                Tick(this, EventArgs.Empty);
            }
        }
    }
}