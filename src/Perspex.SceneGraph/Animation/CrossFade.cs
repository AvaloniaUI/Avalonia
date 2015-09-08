





namespace Perspex.Animation
{
    using System;
    using System.Collections.Generic;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a cross-fade animation between two <see cref="IVisual"/>s.
    /// </summary>
    public class CrossFade : IPageTransition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CrossFade"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        public CrossFade(TimeSpan duration)
        {
            this.Duration = duration;
        }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public TimeSpan Duration { get; }

        /// <summary>
        /// Starts the animation.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        public async Task Start(IVisual from, IVisual to)
        {
            var tasks = new List<Task>();

            if (to != null)
            {
                to.Opacity = 0;
            }

            if (from != null)
            {
                tasks.Add(Animate.Property(
                    (IObservablePropertyBag)from,
                    Visual.OpacityProperty,
                    from.Opacity,
                    0,
                    LinearEasing.For<double>(),
                    this.Duration).ToTask());
            }

            if (to != null)
            {
                to.Opacity = 0;
                to.IsVisible = true;

                tasks.Add(Animate.Property(
                    (IObservablePropertyBag)to,
                    Visual.OpacityProperty,
                    0,
                    1,
                    LinearEasing.For<double>(),
                    this.Duration).ToTask());
            }

            await Task.WhenAll(tasks.ToArray());

            if (from != null)
            {
                from.IsVisible = false;
                from.Opacity = 1;
            }

            to.Opacity = 1;
        }

        /// <summary>
        /// Starts the animation.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <param name="forward">
        /// Unused for cross-fades.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        Task IPageTransition.Start(IVisual from, IVisual to, bool forward)
        {
            return this.Start(from, to);
        }
    }
}
