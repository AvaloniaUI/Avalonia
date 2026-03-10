using System.Collections;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.Controls
{
    /// <summary>
    /// A page that displays its child pages in a horizontally scrollable carousel,
    /// with optional animated page transitions.
    /// </summary>
    [TemplatePart("PART_Carousel", typeof(Carousel))]
    public class CarouselPage : SelectingMultiPage
    {
        private Carousel? _carousel;

        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new FuncTemplate<Panel?>(() => new VirtualizingCarouselPanel());

        /// <summary>
        /// Defines the <see cref="ItemsPanel"/> property.
        /// </summary>
        public static readonly StyledProperty<ITemplate<Panel?>> ItemsPanelProperty =
            ItemsControl.ItemsPanelProperty.AddOwner<CarouselPage>();

        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
            AvaloniaProperty.Register<CarouselPage, IPageTransition?>(nameof(PageTransition));

        /// <summary>
        /// Defines the <see cref="IsGestureEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsGestureEnabledProperty =
            AvaloniaProperty.Register<CarouselPage, bool>(nameof(IsGestureEnabled), true);

        /// <summary>
        /// Defines the <see cref="IsKeyboardNavigationEnabled"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsKeyboardNavigationEnabledProperty =
            AvaloniaProperty.Register<CarouselPage, bool>(nameof(IsKeyboardNavigationEnabled), true);

        static CarouselPage()
        {
            ItemsPanelProperty.OverrideDefaultValue<CarouselPage>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<CarouselPage>(true);
        }

        public CarouselPage()
        {
            SetCurrentValue(PagesProperty, new AvaloniaList<Page>());
            AddHandler(PointerWheelChangedEvent, OnPointerWheelTunnel, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets or sets the items panel template used to arrange page items.
        /// </summary>
        public ITemplate<Panel?> ItemsPanel
        {
            get => GetValue(ItemsPanelProperty);
            set => SetValue(ItemsPanelProperty, value);
        }

        /// <summary>
        /// Gets or sets the animated page transition used when the selected page changes.
        /// </summary>
        public IPageTransition? PageTransition
        {
            get => GetValue(PageTransitionProperty);
            set => SetValue(PageTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether swipe and scroll gestures can be used to navigate between pages.
        /// </summary>
        public bool IsGestureEnabled
        {
            get => GetValue(IsGestureEnabledProperty);
            set => SetValue(IsGestureEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets whether keyboard shortcuts (arrow keys, Home/End) can be used to navigate between pages.
        /// </summary>
        public bool IsKeyboardNavigationEnabled
        {
            get => GetValue(IsKeyboardNavigationEnabledProperty);
            set => SetValue(IsKeyboardNavigationEnabledProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_carousel != null)
                _carousel.SelectionChanged -= OnCarouselSelectionChanged;

            _carousel = e.NameScope.Find<Carousel>("PART_Carousel");

            if (_carousel != null)
            {
                _carousel.PageTransition = PageTransition;
                _carousel.ItemsPanel = ItemsPanel;
                _carousel.ItemTemplate = PageTemplate;
                _carousel.IsSwipeEnabled = IsGestureEnabled;

                if (SelectedIndex >= 0)
                {
                    _carousel.SelectedIndex = SelectedIndex;
                }

                _carousel.SelectionChanged += OnCarouselSelectionChanged;

                UpdateActivePage();
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PageTransitionProperty && _carousel != null)
                _carousel.PageTransition = change.GetNewValue<IPageTransition?>();
            else if (change.Property == ItemsPanelProperty && _carousel != null)
                _carousel.ItemsPanel = change.GetNewValue<ITemplate<Panel?>>();
            else if (change.Property == PageTemplateProperty && _carousel != null)
                _carousel.ItemTemplate = change.GetNewValue<IDataTemplate?>();
            else if (change.Property == IsGestureEnabledProperty && _carousel != null)
                _carousel.IsSwipeEnabled = change.GetNewValue<bool>();
        }

        protected override void UpdateActivePage(NavigationType navigationType)
        {
            if (_carousel != null)
            {
                var index = _carousel.SelectedIndex;
                if (index >= 0)
                {
                    CommitSelection(index, _carousel.SelectedItem as Page ?? ResolvePageAtIndex(index), navigationType);
                    UpdateAccessibilityName(index, GetPageCount(), _carousel.SelectedItem as Page);
                }
                else if (Pages is IList { Count: > 0 })
                {
                    ApplySelectedIndex(0);
                }
            }
            else if (SelectedIndex < 0 && Pages is IList { Count: > 0 })
            {
                ApplySelectedIndex(0);
            }
        }

        protected override void ApplySelectedIndex(int index)
        {
            if (_carousel != null)
            {
                // Delegate to the internal Carousel; lifecycle events fire via OnCarouselSelectionChanged.
                _carousel.SelectedIndex = index;
            }
            else
            {
                // No template applied yet — commit directly so lifecycle events still fire.
                var newPage = ResolvePageAtIndex(index);
                CommitSelection(index, newPage);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled || !IsKeyboardNavigationEnabled)
                return;

            bool isRtl = FlowDirection == Media.FlowDirection.RightToLeft;
            bool next = isRtl ? e.Key == Key.Left : e.Key == Key.Right;
            bool prev = isRtl ? e.Key == Key.Right : e.Key == Key.Left;

            var pageCount = GetPageCount();
            if (next || e.Key == Key.Down)
            {
                if (SelectedIndex < pageCount - 1)
                {
                    ApplySelectedIndex(SelectedIndex + 1);
                    e.Handled = true;
                }
            }
            else if (prev || e.Key == Key.Up)
            {
                if (SelectedIndex > 0)
                {
                    ApplySelectedIndex(SelectedIndex - 1);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Home)
            {
                if (pageCount > 0 && SelectedIndex != 0)
                {
                    ApplySelectedIndex(0);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.End)
            {
                if (pageCount > 0 && SelectedIndex != pageCount - 1)
                {
                    ApplySelectedIndex(pageCount - 1);
                    e.Handled = true;
                }
            }
        }

        private void OnCarouselSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_carousel == null)
                return;

            var newIndex = _carousel.SelectedIndex;
            if (newIndex == SelectedIndex && ReferenceEquals(_carousel.SelectedItem as Page, SelectedPage))
                return;

            var newPage = _carousel.SelectedItem as Page ?? ResolvePageAtIndex(newIndex);
            CommitSelection(newIndex, newPage, NavigationType.Replace);
            UpdateAccessibilityName(newIndex, GetPageCount(), newPage);
        }

        private void OnPointerWheelTunnel(object? sender, PointerWheelEventArgs e)
        {
            if (!IsGestureEnabled)
            {
                e.Handled = true;
                return;
            }

            bool isRtl = FlowDirection == Media.FlowDirection.RightToLeft;
            var pageCount = GetPageCount();
            bool goNext = e.Delta.Y < 0 || (isRtl ? e.Delta.X < 0 : e.Delta.X > 0);
            bool goPrev = e.Delta.Y > 0 || (isRtl ? e.Delta.X > 0 : e.Delta.X < 0);

            if (goNext && SelectedIndex < pageCount - 1)
                ApplySelectedIndex(SelectedIndex + 1);
            else if (goPrev && SelectedIndex > 0)
                ApplySelectedIndex(SelectedIndex - 1);

            e.Handled = true;
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new CarouselPageAutomationPeer(this);

        private void UpdateAccessibilityName(int index, int pageCount, Page? page)
        {
            var header = page?.Header?.ToString();
            var position = pageCount > 0 ? $"Page {index + 1} of {pageCount}" : $"Page {index + 1}";
            var name = string.IsNullOrEmpty(header) ? position : $"{position}: {header}";
            AutomationProperties.SetName(this, name);
        }

        private int GetPageCount()
        {
            return Pages is ICollection<Page> col ? col.Count :
                   Pages is IList list ? list.Count : 0;
        }

    }
}
