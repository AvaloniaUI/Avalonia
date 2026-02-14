using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    /// An items control that displays its items as pages that fill the control.
    /// </summary>
    public class Carousel : SelectingItemsControl
    {
        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
            AvaloniaProperty.Register<Carousel, IPageTransition?>(nameof(PageTransition));

        /// <summary>
        /// Defines the <see cref="IsSwipeEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSwipeEnabledProperty =
            AvaloniaProperty.Register<Carousel, bool>(nameof(IsSwipeEnabled), defaultValue: false);

        /// <summary>
        /// Defines the <see cref="IsSwiping"/> property.
        /// </summary>
        public static readonly DirectProperty<Carousel, bool> IsSwipingProperty =
            AvaloniaProperty.RegisterDirect<Carousel, bool>(nameof(IsSwiping),
                o => o.IsSwiping);

        /// <summary>
        /// The default value of <see cref="ItemsControl.ItemsPanelProperty"/> for 
        /// <see cref="Carousel"/>.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new VirtualizingCarouselPanel());

        private IScrollable? _scroller;
        private bool _isSwiping;

        /// <summary>
        /// Initializes static members of the <see cref="Carousel"/> class.
        /// </summary>
        static Carousel()
        {
            SelectionModeProperty.OverrideDefaultValue<Carousel>(SelectionMode.AlwaysSelected);
            ItemsPanelProperty.OverrideDefaultValue<Carousel>(DefaultPanel);
        }

        /// <summary>
        /// Gets or sets the transition to use when moving between pages.
        /// </summary>
        public IPageTransition? PageTransition
        {
            get => GetValue(PageTransitionProperty);
            set => SetValue(PageTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether swipe gestures are enabled for navigating between pages.
        /// </summary>
        public bool IsSwipeEnabled
        {
            get => GetValue(IsSwipeEnabledProperty);
            set => SetValue(IsSwipeEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets whether a swipe gesture is currently in progress.
        /// </summary>
        public bool IsSwiping
        {
            get => _isSwiping;
            internal set => SetAndRaise(IsSwipingProperty, ref _isSwiping, value);
        }

        /// <summary>
        /// Moves to the next item in the carousel.
        /// </summary>
        public void Next()
        {
            if (ItemCount == 0)
                return;

            if (SelectedIndex < ItemCount - 1)
            {
                ++SelectedIndex;
            }
            else if (WrapSelection)
            {
                SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Moves to the previous item in the carousel.
        /// </summary>
        public void Previous()
        {
            if (ItemCount == 0)
                return;

            if (SelectedIndex > 0)
            {
                --SelectedIndex;
            }
            else if (WrapSelection)
            {
                SelectedIndex = ItemCount - 1;
            }
        }
        
        internal PageSlide.SlideAxis? GetTransitionAxis()
        {
            return PageTransition switch
            {
                Rotate3DTransition r3d => r3d.Orientation,
                PageSlide ps => ps.Orientation,
                _ => null
            };
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || ItemCount == 0)
                return;

            var axis = GetTransitionAxis();
            var isVertical = axis == PageSlide.SlideAxis.Vertical;
            var isHorizontal = axis == PageSlide.SlideAxis.Horizontal;

            switch (e.Key)
            {
                case Key.Left when !isVertical:
                case Key.Up when !isHorizontal:
                    Previous();
                    e.Handled = true;
                    break;
                case Key.Right when !isVertical:
                case Key.Down when !isHorizontal:
                    Next();
                    e.Handled = true;
                    break;
                case Key.Home:
                    if (ItemCount > 0)
                    {
                        SelectedIndex = 0;
                        e.Handled = true;
                    }
                    break;
                case Key.End:
                    if (ItemCount > 0)
                    {
                        SelectedIndex = ItemCount - 1;
                        e.Handled = true;
                    }
                    break;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);

            if (_scroller is not null)
                _scroller.Offset = new(SelectedIndex, 0);

            return result;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _scroller = e.NameScope.Find<IScrollable>("PART_ScrollViewer");
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectedIndexProperty && _scroller is not null)
            {
                var value = change.GetNewValue<int>();
                _scroller.Offset = new(value, 0);
            }

            if (change.Property == IsSwipeEnabledProperty || change.Property == PageTransitionProperty)
            {
                if (ItemsPanelRoot is VirtualizingCarouselPanel panel)
                {
                    panel.RefreshGestureRecognizer();
                }
            }
        }
    }
}
