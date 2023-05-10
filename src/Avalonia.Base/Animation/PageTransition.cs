using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Styling;

namespace Avalonia.Animation
{
    /// <summary>
    /// Base class for animations that transition between two controls.
    /// </summary>
    public abstract class PageTransition : IPageTransition
    {
        private TimeSpan _duration;
        private Animation? _hideVisibilityAnimation;
        private Animation? _showVisibilityAnimation;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageTransition"/> class.
        /// </summary>
        public PageTransition()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageTransition"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        public PageTransition(TimeSpan duration)
        {
            _duration = duration;
        }

        /// <summary>
        /// Gets or sets the duration of the animation.
        /// </summary>
        public TimeSpan Duration
        {
            get => _duration;
            set
            {
                if (_duration != value)
                {
                    _duration = value;
                    InvalidateCachedAnimations();
                }
            }
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
        /// <param name="cancellationToken">
        /// Animation cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        /// <remarks>
        /// The <paramref name="from"/> and <paramref name="to"/> controls will be made visible
        /// and <paramref name="from"/> transitioned to <paramref name="to"/>. At the end of the
        /// animation (when the returned task completes), <paramref name="from"/> will be made
        /// invisible but all other properties involved in the transition will have been left
        /// unchanged.
        /// </remarks>
        public Task Start(Visual? from, Visual? to, CancellationToken cancellationToken)
        {
            return Start(from, to, true, cancellationToken);
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
        /// If the animation is bidirectional, controls the direction of the animation.
        /// </param>
        /// <param name="cancellationToken">
        /// Animation cancellation.
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        /// <remarks>
        /// The <paramref name="from"/> and <paramref name="to"/> controls will be made visible
        /// and <paramref name="from"/> transitioned to <paramref name="to"/>. At the end of the
        /// animation (when the returned task completes), <paramref name="from"/> will be made
        /// invisible but all other properties involved in the transition will have been left
        /// unchanged.
        /// </remarks>
        public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            List<Task>? tasks = null;

            if (from is not null)
            {
                tasks ??= new();
                tasks.Add(GetHideVisibilityAnimation().RunAsync(from, null, cancellationToken));
                tasks.Add(GetHideAnimation(from, to, forward).RunAsync(from, null, cancellationToken));
            }

            if (to is not null)
            {
                tasks ??= new();
                tasks.Add(GetShowAnimation(from, to, forward).RunAsync(to, null, cancellationToken));
                tasks.Add(GetShowVisibilityAnimation().RunAsync(to, null, cancellationToken));
            }

            if (tasks is not null)
                await Task.WhenAll(tasks);
        }

        /// <summary>
        /// When implemented in a derived class, returns the animation used to transition away from
        /// the <paramref name="from"/> control.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <param name="forward">
        /// If the animation is bidirectional, controls the direction of the animation.
        /// </param>
        protected abstract Animation GetHideAnimation(Visual? from, Visual? to, bool forward);

        /// <summary>
        /// When implemented in a derived class, returns the animation used to transition to the
        /// <paramref name="to"/> control.
        /// </summary>
        /// <param name="from">
        /// The control that is being transitioned away from. May be null.
        /// </param>
        /// <param name="to">
        /// The control that is being transitioned to. May be null.
        /// </param>
        /// <param name="forward">
        /// If the animation is bidirectional, controls the direction of the animation.
        /// </param>
        protected abstract Animation GetShowAnimation(Visual? from, Visual? to, bool forward);
        
        /// <summary>
        /// Called when a property that affects the animation is changed.
        /// </summary>
        protected virtual void InvalidateCachedAnimations()
        {
            if (_hideVisibilityAnimation is not null)
                _hideVisibilityAnimation.Duration = _duration;
            if (_showVisibilityAnimation is not null)
                _showVisibilityAnimation.Duration = _duration;
        }

        private Animation GetHideVisibilityAnimation()
        {
            return _hideVisibilityAnimation ??= CreateIsVisibleAnimation(Duration, false);
        }

        private Animation GetShowVisibilityAnimation()
        {
            return _showVisibilityAnimation ??= CreateIsVisibleAnimation(Duration, true);
        }

        private static Animation CreateIsVisibleAnimation(TimeSpan duration, bool endState)
        {
            return new()
            {
                Duration = duration,
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.IsVisibleProperty,
                                Value = true,
                            },
                        },
                        Cue = new Cue(0)
                    },
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.IsVisibleProperty,
                                Value = endState,
                            },
                        },
                        Cue = new Cue(1)
                    }
                }
            };
        }
    }
}
