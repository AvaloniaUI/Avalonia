// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;

namespace Avalonia.Threading
{
    /// <summary>
    /// A reactive scheduler that uses Avalonia's <see cref="Dispatcher"/>.
    /// </summary>
    public class AvaloniaScheduler : LocalScheduler
    {
        /// <summary>
        /// The instance of the <see cref="AvaloniaScheduler"/>.
        /// </summary>
        public static readonly AvaloniaScheduler Instance = new AvaloniaScheduler();

        /// <summary>
        /// Initializes a new instance of the <see cref="AvaloniaScheduler"/> class.
        /// </summary>
        private AvaloniaScheduler()
        {
        }

        /// <inheritdoc/>
        public override IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var composite = new CompositeDisposable(2);
            if (dueTime == TimeSpan.Zero)
            {
                if (!Dispatcher.UIThread.CheckAccess())
                {
                    var cancellation = new CancellationDisposable();
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (!cancellation.Token.IsCancellationRequested)
                        {
                            composite.Add(action(this, state));
                        }
                    }, DispatcherPriority.DataBind);
                    composite.Add(cancellation); 
                }
                else
                {
                    return action(this, state);
                }
            }
            else
            {
                composite.Add(DispatcherTimer.RunOnce(() => composite.Add(action(this, state)), dueTime));
            }
            return composite;
        }
    }
}
