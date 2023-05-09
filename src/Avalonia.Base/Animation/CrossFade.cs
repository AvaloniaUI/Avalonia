using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Styling;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a cross-fade animation between two <see cref="Visual"/>s.
    /// </summary>
    public class CrossFade : IPageTransition
    {
        private readonly Animation _fadeOutAnimation;
        private readonly Animation _fadeInAnimation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossFade"/> class.
        /// </summary>
        public CrossFade()
            : this(TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CrossFade"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        public CrossFade(TimeSpan duration)
        {
            _fadeOutAnimation = new Animation
            {
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
                            new Setter
                            {
                                Property = Visual.OpacityProperty,
                                Value = 1d
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
                                Property = Visual.OpacityProperty,
                                Value = 0d
                            },
                        },
                        Cue = new Cue(1)
                    },
                }
            };
            _fadeInAnimation = new Animation
            {
                Children =
                {
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.OpacityProperty,
                                Value = 0d
                            }
                        },
                        Cue = new Cue(0)
                    },
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.OpacityProperty,
                                Value = 1d
                            }
                        },
                        Cue = new Cue(1)
                    },
                }
            };
            _fadeOutAnimation.Duration = _fadeInAnimation.Duration = duration;
        }

        /// <summary>
        /// Gets the duration of the animation.
        /// </summary>
        public TimeSpan Duration
        {
            get => _fadeOutAnimation.Duration;
            set => _fadeOutAnimation.Duration = _fadeInAnimation.Duration = value;
        }

        /// <summary>
        /// Gets or sets element entrance easing.
        /// </summary>
        public Easing FadeInEasing
        {
            get => _fadeInAnimation.Easing;
            set => _fadeInAnimation.Easing = value;
        }

        /// <summary>
        /// Gets or sets element exit easing.
        /// </summary>
        public Easing FadeOutEasing
        {
            get => _fadeOutAnimation.Easing;
            set => _fadeOutAnimation.Easing = value;
        }

        /// <inheritdoc cref="Start(Visual, Visual, CancellationToken)" />
        public async Task Start(Visual? from, Visual? to, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var tasks = new List<Task>();
            var initialFromVisible = true;
            var initialToVisible = true;

            if (from != null)
            {
                tasks.Add(_fadeOutAnimation.RunAsync(from, null, cancellationToken));

                // Make "from" control invisible: this is overridden in the fade out animation, so
                // will only take effect when the animation is completed.
                initialFromVisible = from.IsVisible;
                from.IsVisible = false;
            }

            if (to != null)
            {
                initialToVisible = to.IsVisible;
                to.IsVisible = true;
                tasks.Add(_fadeInAnimation.RunAsync(to, null, cancellationToken));
            }

            await Task.WhenAll(tasks);

            if (cancellationToken.IsCancellationRequested)
            {
                if (from != null)
                    from.IsVisible = initialFromVisible;
                if (to != null)
                    to.IsVisible = initialToVisible;
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
        /// <param name="forward">
        /// Unused for cross-fades.
        /// </param>
        /// <param name="cancellationToken">allowed cancel transition</param>
        /// <returns>
        /// A <see cref="Task"/> that tracks the progress of the animation.
        /// </returns>
        Task IPageTransition.Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
        {
            return Start(from, to, cancellationToken);
        }
    }
}
