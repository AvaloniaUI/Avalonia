





namespace Perspex.Threading
{
    using System;
    using System.Reactive.Concurrency;

    /// <summary>
    /// A reactive scheduler that uses Perspex's <see cref="Dispatcher.UIThread"/>.
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
