// -----------------------------------------------------------------------
// <copyright file="PerspexScheduler.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Threading
{
    using System;
    using System.Reactive.Concurrency;
    using System.Reactive.Disposables;

    public class PerspexScheduler : LocalScheduler
    {
        private static readonly PerspexScheduler instance = new PerspexScheduler();

        public static PerspexScheduler Instance
        {
            get { return instance; }
        }

        public override IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return DispatcherTimer.Run(() => 
            {
                action(this, state);
                return false;
            }, dueTime);
        }
    }
}
