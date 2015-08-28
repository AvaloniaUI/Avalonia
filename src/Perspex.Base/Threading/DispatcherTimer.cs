// -----------------------------------------------------------------------
// <copyright file="DispatcherTimer.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Threading
{
    using System;
    using System.Reactive.Disposables;
    using Perspex.Platform;
    using Splat;

    /// <summary>
    /// A timer that uses a <see cref="Dispatcher"/> to fire at a specified interval.
    /// </summary>
    public class DispatcherTimer
    {
        private IDisposable timer;

        private DispatcherPriority priority;

        private TimeSpan interval;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        public DispatcherTimer()
        {
            this.priority = DispatcherPriority.Normal;
            this.Dispatcher = Dispatcher.UIThread;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        /// <param name="priority">The priority to use.</param>
        public DispatcherTimer(DispatcherPriority priority)
        {
            this.priority = priority;
            this.Dispatcher = Dispatcher.UIThread;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        /// <param name="priority">The priority to use.</param>
        /// <param name="dispatcher">The dispatcher to use.</param>
        public DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher)
        {
            this.priority = priority;
            this.Dispatcher = dispatcher;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        /// <param name="interval">The interval at which to tick.</param>
        /// <param name="priority">The priority to use.</param>
        /// <param name="dispatcher">The dispatcher to use.</param>
        /// <param name="callback">The event to call when the timer ticks.</param>
        public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback, Dispatcher dispatcher)
        {
            this.priority = priority;
            this.Dispatcher = dispatcher;
            this.Interval = interval;
            this.Tick += callback;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DispatcherTimer"/> class.
        /// </summary>
        ~DispatcherTimer()
        {
            if (this.timer != null)
            {
                this.Stop();
            }
        }

        /// <summary>
        /// Raised when the timer ticks.
        /// </summary>
        public event EventHandler Tick;

        /// <summary>
        /// Gets the dispatcher that the timer uses.
        /// </summary>
        public Dispatcher Dispatcher
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets or sets the interval at which the timer ticks.
        /// </summary>
        public TimeSpan Interval
        {
            get
            {
                return this.interval;
            }

            set
            {
                bool enabled = this.IsEnabled;
                this.Stop();
                this.interval = value;
                this.IsEnabled = enabled;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the timer is running.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return this.timer != null;
            }

            set
            {
                if (this.IsEnabled != value)
                {
                    if (value)
                    {
                        this.Start();
                    }
                    else
                    {
                        this.Stop();
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
            if (!this.IsEnabled)
            {
                IPlatformThreadingInterface threading = Locator.Current.GetService<IPlatformThreadingInterface>();
                this.timer = threading.StartTimer(this.Interval, this.InternalTick);
            }
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            if (this.IsEnabled)
            {
                IPlatformThreadingInterface threading = Locator.Current.GetService<IPlatformThreadingInterface>();
                this.timer.Dispose();
                this.timer = null;
            }
        }

        /// <summary>
        /// Raises the <see cref="Tick"/> event on the dispatcher thread.
        /// </summary>
        private void InternalTick()
        {
            this.Dispatcher.InvokeAsync(this.RaiseTick, this.priority);
        }

        /// <summary>
        /// Raises the <see cref="Tick"/> event.
        /// </summary>
        private void RaiseTick()
        {
            if (this.Tick != null)
            {
                this.Tick(this, EventArgs.Empty);
            }
        }
    }
}