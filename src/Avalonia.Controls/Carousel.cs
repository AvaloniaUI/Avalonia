using Avalonia.Animation;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;

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
        /// The default value of <see cref="ItemsControl.ItemsPanelProperty"/> for 
        /// <see cref="Carousel"/>.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new VirtualizingCarouselPanel());

        private IScrollable? _scroller;

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
        /// Moves to the next item in the carousel.
        /// </summary>
        public void Next()
        {
            if (SelectedIndex < ItemCount - 1)
            {
                ++SelectedIndex;
            }
        }

        /// <summary>
        /// Moves to the previous item in the carousel.
        /// </summary>
        public void Previous()
        {
            if (SelectedIndex > 0)
            {
                --SelectedIndex;
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
        }
    }
}
