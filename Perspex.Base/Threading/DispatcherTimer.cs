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

    public class DispatcherTimer
    {
        private IDisposable timer;

        private DispatcherPriority priority;

        private TimeSpan interval;

        public DispatcherTimer()
        {
            this.priority = DispatcherPriority.Normal;
            this.Dispatcher = Dispatcher.UIThread;
        }

        public DispatcherTimer(DispatcherPriority priority)
        {
            this.priority = priority;
            this.Dispatcher = Dispatcher.UIThread;
        }

        public DispatcherTimer(DispatcherPriority priority, Dispatcher dispatcher)
        {
            this.priority = priority;
            this.Dispatcher = dispatcher;
        }

        public DispatcherTimer(TimeSpan interval, DispatcherPriority priority, EventHandler callback, Dispatcher dispatcher)
        {
            this.priority = priority;
            this.Dispatcher = dispatcher;
            this.Interval = interval;
            this.Tick += callback;
        }

        ~DispatcherTimer()
        {
            if (this.timer != null)
            {
                this.Stop();
            }
        }

        public event EventHandler Tick;

        public Dispatcher Dispatcher 
        { 
            get; 
            private set; 
        }
        
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
        
        public object Tag 
        { 
            get; 
            set; 
        }

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

        public void Start()
        {
            if (!this.IsEnabled)
            {
                IPlatformThreadingInterface threading = Locator.Current.GetService<IPlatformThreadingInterface>();
                this.timer = threading.StartTimer(this.Interval, this.InternalTick);
            }
        }
        
        public void Stop()
        {
            if (this.IsEnabled)
            {
                IPlatformThreadingInterface threading = Locator.Current.GetService<IPlatformThreadingInterface>();
                this.timer.Dispose();
                this.timer = null;
            }
        }

        private void InternalTick()
        {
            this.Dispatcher.InvokeAsync(this.RaiseTick, this.priority);
        }

        private void RaiseTick()
        {
            if (this.Tick != null)
            {
                this.Tick(this, EventArgs.Empty);
            }
        }
    }
}