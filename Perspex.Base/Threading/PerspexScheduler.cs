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
        public static readonly PerspexScheduler Instance = new PerspexScheduler();

        public override IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            return DispatcherTimer.Run(
                () => 
                {
                    action(this, state);
                    return false;
                }, 
                dueTime);
        }
    }
}
