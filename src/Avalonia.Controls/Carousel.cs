using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    /// An items control that displays its items as pages and can reveal adjacent pages
    /// using <see cref="ViewportFraction"/>.
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
        /// Defines the <see cref="ViewportFraction"/> property.
        /// </summary>
        public static readonly StyledProperty<double> ViewportFractionProperty =
            AvaloniaProperty.Register<Carousel, double>(
                nameof(ViewportFraction),
                defaultValue: 1d,
                coerce: (_, value) => double.IsFinite(value) && value > 0 ? value : 1d);

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
        /// When enabled, mouse pointer events are also accepted in addition to touch and pen.
        /// </summary>
        public bool IsSwipeEnabled
        {
            get => GetValue(IsSwipeEnabledProperty);
            set => SetValue(IsSwipeEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the fraction of the viewport occupied by each page.
        /// A value of 1 shows a single full page; values below 1 reveal adjacent pages.
        /// </summary>
        public double ViewportFraction
        {
            get => GetValue(ViewportFractionProperty);
            set => SetValue(ViewportFractionProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether a swipe gesture is currently in progress.
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
            var transition = PageTransition;

            if (transition is CompositePageTransition composite)
            {
                foreach (var t in composite.PageTransitions)
                {
                    if (t is PageSlide slide)
                        return slide.Orientation;
                }

                return null;
            }

            return transition is PageSlide ps ? ps.Orientation : null;
        }

        internal PageSlide.SlideAxis GetLayoutAxis() => GetTransitionAxis() ?? PageSlide.SlideAxis.Horizontal;

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled || ItemCount == 0)
                return;

            var axis = ViewportFraction != 1d ? GetLayoutAxis() : GetTransitionAxis();
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
                    SelectedIndex = 0;
                    e.Handled = true;
                    break;
                case Key.End:
                    SelectedIndex = ItemCount - 1;
                    e.Handled = true;
                    break;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);

            SyncScrollOffset();

            return result;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            _scroller = e.NameScope.Find<IScrollable>("PART_ScrollViewer");

            if (ItemsPanelRoot is VirtualizingCarouselPanel panel)
                panel.RefreshGestureRecognizer();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectedIndexProperty)
            {
                SyncScrollOffset();
            }

            if (change.Property == IsSwipeEnabledProperty ||
                change.Property == PageTransitionProperty ||
                change.Property == ViewportFractionProperty ||
                change.Property == WrapSelectionProperty)
            {
                if (ItemsPanelRoot is VirtualizingCarouselPanel panel)
                {
                    if (change.Property == ViewportFractionProperty && !panel.IsManagingInteractionOffset)
                        panel.SyncSelectionOffset(SelectedIndex);

                    panel.RefreshGestureRecognizer();
                    panel.InvalidateMeasure();
                }

                SyncScrollOffset();
            }
        }

        private void SyncScrollOffset()
        {
            if (ItemsPanelRoot is VirtualizingCarouselPanel panel)
            {
                if (panel.IsManagingInteractionOffset)
                    return;

                panel.SyncSelectionOffset(SelectedIndex);

                if (ViewportFraction != 1d)
                    return;
            }

            if (_scroller is null)
                return;

            _scroller.Offset = CreateScrollOffset(SelectedIndex);
        }

        private Vector CreateScrollOffset(int index)
        {
            if (ViewportFraction != 1d && GetLayoutAxis() == PageSlide.SlideAxis.Vertical)
                return new(0, index);

            return new(index, 0);
        }
    }
}
