using System;
using System.Collections.Generic;
using Avalonia.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a cross-fade animation between two <see cref="Visual"/>s.
    /// </summary>
    public class CrossFade : IPageTransition, IProgressPageTransition
    {
        private const double SidePeekOpacity = 0.72;
        private const double FarPeekOpacity = 0.42;
        private const double OutgoingDip = 0.22;
        private const double IncomingBoost = 0.12;
        private const double PassiveDip = 0.05;
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
                FillMode = FillMode.Forward,
                Children =
                {
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
                        Cue = new Cue(0d)
                    },
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
                        Cue = new Cue(1d)
                    }

                }
            };
            _fadeInAnimation = new Animation
            {
                FillMode = FillMode.Forward,
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
                        Cue = new Cue(0d)
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
                        Cue = new Cue(1d)
                    }

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

        /// <summary>
        /// Gets or sets the fill mode applied to both fade animations.
        /// Defaults to <see cref="FillMode.Forward"/>.
        /// </summary>
        public FillMode FillMode
        {
            get => _fadeOutAnimation.FillMode;
            set => _fadeOutAnimation.FillMode = _fadeInAnimation.FillMode = value;
        }

        /// <inheritdoc cref="Start(Visual, Visual, CancellationToken)" />
        public async Task Start(Visual? from, Visual? to, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            var tasks = new List<Task>();

            if (from != null)
            {
                tasks.Add(_fadeOutAnimation.RunAsync(from, null, cancellationToken));
            }

            if (to != null)
            {
                to.IsVisible = true;
                tasks.Add(_fadeInAnimation.RunAsync(to, null, cancellationToken));
            }

            await Task.WhenAll(tasks);

            if (from != null && !cancellationToken.IsCancellationRequested)
                from.IsVisible = false;
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

        /// <inheritdoc/>
        public void Update(
            double progress,
            Visual? from,
            Visual? to,
            bool forward,
            double pageLength,
            IReadOnlyList<PageTransitionItem> visibleItems)
        {
            if (visibleItems.Count > 0)
            {
                UpdateVisibleItems(progress, from, to, visibleItems);
                return;
            }

            if (from != null)
                from.Opacity = 1 - progress;
            if (to != null)
            {
                to.IsVisible = true;
                to.Opacity = progress;
            }
        }

        /// <inheritdoc/>
        public void Reset(Visual visual)
        {
            visual.Opacity = 1;
        }

        private static void UpdateVisibleItems(
            double progress,
            Visual? from,
            Visual? to,
            IReadOnlyList<PageTransitionItem> visibleItems)
        {
            var emphasis = Math.Sin(Math.Clamp(progress, 0.0, 1.0) * Math.PI);
            foreach (var item in visibleItems)
            {
                item.Visual.IsVisible = true;
                var opacity = GetOpacityForOffset(item.ViewportCenterOffset);

                if (ReferenceEquals(item.Visual, from))
                {
                    opacity = Math.Max(FarPeekOpacity, opacity - (OutgoingDip * emphasis));
                }
                else if (ReferenceEquals(item.Visual, to))
                {
                    opacity = Math.Min(1.0, opacity + (IncomingBoost * emphasis));
                }
                else
                {
                    opacity = Math.Max(FarPeekOpacity, opacity - (PassiveDip * emphasis));
                }

                item.Visual.Opacity = opacity;
            }
        }

        private static double GetOpacityForOffset(double offsetFromCenter)
        {
            var distance = Math.Abs(offsetFromCenter);

            if (distance <= 1.0)
                return Lerp(1.0, SidePeekOpacity, distance);

            if (distance <= 2.0)
                return Lerp(SidePeekOpacity, FarPeekOpacity, distance - 1.0);

            return FarPeekOpacity;
        }

        private static double Lerp(double from, double to, double t)
        {
            return from + ((to - from) * Math.Clamp(t, 0.0, 1.0));
        }
    }
}
