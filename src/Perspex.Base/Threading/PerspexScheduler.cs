// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Concurrency;

namespace Perspex.Threading
{
    /// <summary>
    /// A reactive scheduler that uses Perspex's <see cref="Dispatcher"/>.
    /// </summary>
    public class PerspexScheduler : LocalScheduler
    {
        /// <summary>
        /// The instance of the <see cref="PerspexScheduler"/>.
        /// </summary>
        public static readonly PerspexScheduler Instance = new PerspexScheduler();

        /// <summary>
        /// Initializes a new instance of the <see cref="PerspexScheduler"/> class.
        /// </summary>
        private PerspexScheduler()
        {
        }

        /// <inheritdoc/>
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
