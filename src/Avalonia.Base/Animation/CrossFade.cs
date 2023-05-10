using System;
using Avalonia.Animation.Easings;
using Avalonia.Styling;

namespace Avalonia.Animation
{
    /// <summary>
    /// Defines a cross-fade animation between two <see cref="Visual"/>s.
    /// </summary>
    public class CrossFade : PageTransition
    {
        private readonly Animation _fadeOut;
        private readonly Animation _fadeIn;

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
            : base(duration)
        {
            _fadeOut = CreateFadeAnimation(duration, 1, 0);
            _fadeIn = CreateFadeAnimation(duration, 0, 1);
        }

        /// <summary>
        /// Gets or sets element entrance easing.
        /// </summary>
        public Easing FadeInEasing
        {
            get => _fadeIn.Easing;
            set => _fadeIn.Easing = value;
        }

        /// <summary>
        /// Gets or sets element exit easing.
        /// </summary>
        public Easing FadeOutEasing
        {
            get => _fadeOut.Easing;
            set => _fadeOut.Easing = value;
        }

        protected override Animation GetShowAnimation(Visual? from, Visual? to, bool forward) => _fadeIn;
        protected override Animation GetHideAnimation(Visual? from, Visual? to, bool forward) => _fadeOut;

        protected override void InvalidateCachedAnimations()
        {
            base.InvalidateCachedAnimations();
            _fadeIn.Duration = _fadeOut.Duration = Duration;
        }

        private static Animation CreateFadeAnimation(TimeSpan duration, double fromOpacity, double toOpacity)
        {
            return new Animation
            {
                Duration = duration,
                Children =
                {
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.OpacityProperty,
                                Value = fromOpacity,
                            },
                        },
                        Cue = new Cue(0),
                    },
                    new KeyFrame()
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = Visual.OpacityProperty,
                                Value = toOpacity,
                            },
                        },
                        Cue = new Cue(1),
                    },
                }
            };
        }
    }
}
