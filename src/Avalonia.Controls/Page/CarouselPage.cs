using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

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
            PageNavigationSystemBackButtonPressedEvent.AddClassHandler<CarouselPage>((sender, eventArgs) =>
            {
                if (eventArgs.Handled)
                    return;

                var pageEvent = new RoutedEventArgs(PageNavigationSystemBackButtonPressedEvent);
                sender.CurrentPage?.RaiseEvent(pageEvent);

                if (pageEvent.Handled)
                {
                    eventArgs.Handled = true;
                }
            });
        }

        public CarouselPage()
        {
            SetCurrentValue(PagesProperty, new AvaloniaList<Page>());
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Bubble);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            RemoveHandler(PointerWheelChangedEvent, OnPointerWheelChanged);
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

        protected override Type StyleKeyOverride => typeof(CarouselPage);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            var requestedIndex = SelectedIndex;

            if (_carousel != null)
            {
                _carousel.SelectionChanged -= OnCarouselSelectionChanged;
                _carousel.ContainerPrepared -= OnCarouselContainerPrepared;
            }

            _carousel = e.NameScope.Find<Carousel>("PART_Carousel");

            if (_carousel != null)
            {
                _carousel.ContainerPrepared += OnCarouselContainerPrepared;
                _carousel.PageTransition = PageTransition;
                _carousel.ItemsPanel = ItemsPanel;
                _carousel.ItemTemplate = PageTemplate;
                _carousel.IsSwipeEnabled = IsGestureEnabled;
                _carousel.ItemsSource = (IEnumerable?)ItemsSource ?? Pages;

                if (requestedIndex >= 0)
                {
                    _carousel.SelectedIndex = requestedIndex;
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
            {
                _carousel.ItemTemplate = change.GetNewValue<IDataTemplate?>();
                if (ItemsSource != null)
                    UpdateActivePage();
            }
            else if (change.Property == IsGestureEnabledProperty && _carousel != null)
                _carousel.IsSwipeEnabled = change.GetNewValue<bool>();
            else if (change.Property == ItemsSourceProperty && _carousel != null)
            {
                _carousel.ItemsSource = change.GetNewValue<IEnumerable?>() ?? Pages;
                UpdateActivePage();
            }
            else if (change.Property == PagesProperty && ItemsSource == null && _carousel != null)
                _carousel.ItemsSource = change.GetNewValue<IEnumerable<Page>?>();
        }

        protected override void UpdateActivePage(NavigationType navigationType)
        {
            if (_carousel != null)
            {
                var index = _carousel.SelectedIndex;
                if (index >= 0)
                {
                    UpdateSelection(index, navigationType);
                }
                else if (GetPageCount() > 0)
                {
                    ApplySelectedIndex(0);
                }
            }
            else if (GetPageCount() > 0)
            {
                var index = CoercePreTemplateSelectedIndex(SelectedIndex);
                if (ItemsSource != null)
                {
                    StoreSelectedIndex(index);
                }
                else
                {
                    var page = ResolvePageAtIndex(index);

                    if (index != SelectedIndex || !ReferenceEquals(SelectedPage, page))
                        CommitSelection(index, page, navigationType);
                }
            }
        }

        protected override void ApplySelectedIndex(int index)
        {
            if (_carousel != null)
            {
                _carousel.SelectedIndex = index;
            }
            else
            {
                var pageCount = GetPageCount();

                if (pageCount > 0)
                {
                    var coercedIndex = CoercePreTemplateSelectedIndex(index);
                    if (ItemsSource != null)
                    {
                        StoreSelectedIndex(coercedIndex);
                    }
                    else
                    {
                        var newPage = ResolvePageAtIndex(coercedIndex);
                        CommitSelection(coercedIndex, newPage);
                    }
                }
                else
                {
                    // Preserve preselection until pages exist.
                    StoreSelectedIndex(index);
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled || !IsKeyboardNavigationEnabled)
                return;

            bool isRtl = IsRightToLeft;
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
            var newPage = ResolveDisplayedPageAtIndex(newIndex);
            if (newIndex == SelectedIndex && ReferenceEquals(newPage, SelectedPage))
                return;

            UpdateSelection(newIndex, NavigationType.Replace);
        }

        private void OnCarouselContainerPrepared(object? sender, ContainerPreparedEventArgs e)
        {
            if (_carousel == null || e.Index != _carousel.SelectedIndex)
                return;

            Dispatcher.UIThread.Post(
                () =>
                {
                    if (_carousel != null && _carousel.SelectedIndex == e.Index)
                        UpdateSelection(e.Index, NavigationType.Replace);
                },
                DispatcherPriority.Loaded);
        }

        private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            if (!IsGestureEnabled)
                return;

            bool isRtl = IsRightToLeft;
            var pageCount = GetPageCount();
            bool goNext = e.Delta.Y < 0 || (isRtl ? e.Delta.X < 0 : e.Delta.X > 0);
            bool goPrev = e.Delta.Y > 0 || (isRtl ? e.Delta.X > 0 : e.Delta.X < 0);

            if (goNext && SelectedIndex < pageCount - 1)
            {
                ApplySelectedIndex(SelectedIndex + 1);
                e.Handled = true;
            }
            else if (goPrev && SelectedIndex > 0)
            {
                ApplySelectedIndex(SelectedIndex - 1);
                e.Handled = true;
            }
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new CarouselPageAutomationPeer(this);

        private void UpdateAccessibilityName(int index, int pageCount, Page? page)
        {
            var header = page?.Header?.ToString();
            var position = pageCount > 0 ? $"Page {index + 1} of {pageCount}" : string.Empty;
            var name = string.IsNullOrEmpty(header) ? position : string.IsNullOrEmpty(position) ? header : $"{position}: {header}";
            // CarouselPageAutomationPeer.GetNameCore reads this via base.GetNameCore(), which returns
            // AutomationProperties.Name when set. Position and header are encoded here rather than in the
            // peer so that the name stays current without requiring the peer to re-query the carousel state.
            AutomationProperties.SetName(this, name);
        }

        private bool IsRightToLeft => FlowDirection == Media.FlowDirection.RightToLeft;

        private int GetPageCount()
        {
            if (_carousel != null)
                return _carousel.ItemCount;

            var source = (IEnumerable?)ItemsSource ?? Pages;

            if (source is ICollection nonGenericCol)
                return nonGenericCol.Count;
            if (source is ICollection<Page> col)
                return col.Count;
            if (source != null)
            {
                int count = 0;
                foreach (var _ in source)
                    count++;
                return count;
            }
            return 0;
        }

        private int CoercePreTemplateSelectedIndex(int index)
        {
            var pageCount = GetPageCount();
            if (pageCount <= 0)
                return index;

            return (uint)index < (uint)pageCount ? index : 0;
        }

        private void UpdateSelection(int index, NavigationType navigationType)
        {
            var page = ResolveDisplayedPageAtIndex(index);
            var pageCount = GetPageCount();

            if (page == null && ItemsSource != null)
            {
                StoreSelectedIndex(index);
                UpdateAccessibilityName(index, pageCount, null);
                return;
            }

            CommitSelection(index, page, navigationType);
            UpdateAccessibilityName(index, pageCount, page);
        }

        private Page? ResolveDisplayedPageAtIndex(int index)
        {
            if (index < 0)
                return null;

            if (_carousel?.ContainerFromIndex(index) is { } container)
                return TryGetPageFromContainer(container);

            return ItemsSource == null ? ResolvePageAtIndex(index) : null;
        }

        private static Page? TryGetPageFromContainer(Control container)
        {
            if (container is Page page)
                return page;

            if (container is ContentPresenter presenter)
            {
                if (presenter.Child == null)
                    presenter.UpdateChild();

                return presenter.Child as Page;
            }

            if (container is ContentControl contentControl)
                return contentControl.Content as Page;

            return null;
        }

    }
}
