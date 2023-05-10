using System;
using Avalonia.Animation.Easings;
using Avalonia.Media;
using Avalonia.Styling;

namespace Avalonia.Animation
{
    /// <summary>
    /// The axis on which a <see cref="PageSlide"/> should occur
    /// </summary>
    public enum PageSlideAxis
    {
        Horizontal,
        Vertical
    }

    /// <summary>
    /// Transitions between two pages by sliding them horizontally or vertically.
    /// </summary>
    public class PageSlide : PageTransition
    {
        private readonly Animation _slideOut;
        private readonly Animation _slideIn;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageSlide"/> class.
        /// </summary>
        public PageSlide()
            : this(TimeSpan.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PageSlide"/> class.
        /// </summary>
        /// <param name="duration">The duration of the animation.</param>
        /// <param name="orientation">The axis on which the animation should occur</param>
        public PageSlide(TimeSpan duration, PageSlideAxis orientation = PageSlideAxis.Horizontal)
            : base(duration)
        {
            var property = orientation == PageSlideAxis.Horizontal ? 
                TranslateTransform.XProperty : TranslateTransform.YProperty;
            _slideIn = CreateSlideAnimation(duration, property);
            _slideOut = CreateSlideAnimation(duration, property);
        }

        /// <summary>
        /// Gets the direction of the animation.
        /// </summary>
        public PageSlideAxis Orientation
        {
            get
            {
                return _slideIn.Children[0].Setters[0].Property == TranslateTransform.XProperty ? 
                    PageSlideAxis.Horizontal : PageSlideAxis.Vertical;
            }

            set
            {
                if (Orientation != value)
                {
                    var property = value == PageSlideAxis.Horizontal ?
                        TranslateTransform.XProperty : TranslateTransform.YProperty;
                    _slideIn.Children[0].Setters[0].Property = property;
                    _slideIn.Children[1].Setters[0].Property = property;
                    _slideOut.Children[0].Setters[0].Property = property;
                    _slideOut.Children[1].Setters[0].Property = property;
                }
            }
        }

        /// <summary>
        /// Gets or sets element entrance easing.
        /// </summary>
        public Easing SlideInEasing 
        {
            get => _slideIn.Easing;
            set => _slideIn.Easing = value;
        }
        
        /// <summary>
        /// Gets or sets element exit easing.
        /// </summary>
        public Easing SlideOutEasing
        {
            get => _slideOut.Easing;
            set => _slideOut.Easing = value;
        }

        protected override Animation GetHideAnimation(Visual? from, Visual? to, bool forward)
        {
            var parent = GetVisualParent(from, to);
            var distance = Orientation == PageSlideAxis.Horizontal ? parent.Bounds.Width : parent.Bounds.Height;
            _slideOut.Children[1].Setters[0].Value = forward ? -distance : distance;
            return _slideOut;
        }

        protected override Animation GetShowAnimation(Visual? from, Visual? to, bool forward)
        {
            var parent = GetVisualParent(from, to);
            var distance = Orientation == PageSlideAxis.Horizontal ? parent.Bounds.Width : parent.Bounds.Height;
            _slideIn.Children[0].Setters[0].Value = forward ? distance : -distance;
            return _slideIn;
        }

        protected override void InvalidateCachedAnimations()
        {
            base.InvalidateCachedAnimations();
            _slideIn.Duration = _slideOut.Duration = Duration;
        }

        /// <summary>
        /// Gets the common visual parent of the two control.
        /// </summary>
        /// <param name="from">The from control.</param>
        /// <param name="to">The to control.</param>
        /// <returns>The common parent.</returns>
        /// <exception cref="ArgumentException">
        /// The two controls do not share a common parent.
        /// </exception>
        /// <remarks>
        /// Any one of the parameters may be null, but not both.
        /// </remarks>
        internal static Visual GetVisualParent(Visual? from, Visual? to)
        {
            var p1 = (from ?? to)!.VisualParent;
            var p2 = (to ?? from)!.VisualParent;

            if (p1 != null && p2 != null && p1 != p2)
            {
                throw new ArgumentException("Controls for PageSlide must have same parent.");
            }

            return p1 ?? throw new InvalidOperationException("Cannot determine visual parent.");
        }

        private static Animation CreateSlideAnimation(TimeSpan duration, AvaloniaProperty property)
        {
            return new Animation
            {
                Duration = duration,
                Children =
                {
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = property,
                                Value = 0.0,
                            },
                        },
                        Cue = new Cue(0)
                    },
                    new KeyFrame
                    {
                        Setters =
                        {
                            new Setter
                            {
                                Property = property,
                                Value = 0.0
                            }
                        },
                        Cue = new Cue(1)
                    }
                },
            };
        }
    }
}
