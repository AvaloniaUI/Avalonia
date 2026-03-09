using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Collections;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Automation;
using Avalonia.Controls.Metadata;
using Avalonia.Logging;
using Avalonia.Reactive;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    [TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
    [TemplatePart("PART_SelectedContentHost", typeof(ContentPresenter))]
    [TemplatePart("PART_SelectedContentHost2", typeof(ContentPresenter))]
    public class TabControl : SelectingItemsControl, IContentPresenterHost
    {
        private object? _selectedContent;
        private IDataTemplate? _selectedContentTemplate;
        private CompositeDisposable? _selectedItemSubscriptions;
        private ContentPresenter? _contentPart;
        private ContentPresenter? _contentPresenter2;
        private Control? _dataContextHost;
        private int _previousSelectedIndex = -1;
        private CancellationTokenSource? _currentTransition;
        private bool _shouldAnimate;
        private bool _pendingForward;

        /// <summary>
        /// Defines the <see cref="TabStripPlacement"/> property.
        /// </summary>
        public static readonly StyledProperty<Dock> TabStripPlacementProperty =
            AvaloniaProperty.Register<TabControl, Dock>(nameof(TabStripPlacement), defaultValue: Dock.Top);

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<TabControl>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<TabControl>();

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<TabControl>();

        /// <summary>
        /// Defines the <see cref="SelectedContent"/> property.
        /// </summary>
        public static readonly DirectProperty<TabControl, object?> SelectedContentProperty =
            AvaloniaProperty.RegisterDirect<TabControl, object?>(nameof(SelectedContent), o => o.SelectedContent);

        /// <summary>
        /// Defines the <see cref="SelectedContentTemplate"/> property.
        /// </summary>
        public static readonly DirectProperty<TabControl, IDataTemplate?> SelectedContentTemplateProperty =
            AvaloniaProperty.RegisterDirect<TabControl, IDataTemplate?>(nameof(SelectedContentTemplate), o => o.SelectedContentTemplate);

        /// <summary>
        /// Defines the <see cref="PageTransition"/> property.
        /// </summary>
        public static readonly StyledProperty<IPageTransition?> PageTransitionProperty =
            AvaloniaProperty.Register<TabControl, IPageTransition?>(nameof(PageTransition));

        /// <summary>
        /// Defines the <see cref="IndicatorTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> IndicatorTemplateProperty =
            AvaloniaProperty.Register<TabControl, IDataTemplate?>(nameof(IndicatorTemplate));

        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new WrapPanel());

        /// <summary>
        /// Initializes static members of the <see cref="TabControl"/> class.
        /// </summary>
        static TabControl()
        {
            SelectionModeProperty.OverrideDefaultValue<TabControl>(SelectionMode.AlwaysSelected);
            ItemsPanelProperty.OverrideDefaultValue<TabControl>(DefaultPanel);
            AffectsMeasure<TabControl>(TabStripPlacementProperty);
            SelectedItemProperty.Changed.AddClassHandler<TabControl>((x, _) => x.UpdateSelectedContent());
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<TabControl>(AutomationControlType.Tab);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the control.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the control.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the tabstrip placement of the TabControl.
        /// </summary>
        public Dock TabStripPlacement
        {
            get => GetValue(TabStripPlacementProperty);
            set => SetValue(TabStripPlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the default data template used to display the content of the selected tab.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the content of the selected tab.
        /// </summary>
        public object? SelectedContent
        {
            get => _selectedContent;
            internal set => SetAndRaise(SelectedContentProperty, ref _selectedContent, value);
        }

        /// <summary>
        /// Gets or sets the content template for the selected tab.
        /// </summary>
        public IDataTemplate? SelectedContentTemplate
        {
            get => _selectedContentTemplate;
            internal set => SetAndRaise(SelectedContentTemplateProperty, ref _selectedContentTemplate, value);
        }

        /// <summary>
        /// Gets or sets the page transition to use when switching tabs.
        /// </summary>
        public IPageTransition? PageTransition
        {
            get => GetValue(PageTransitionProperty);
            set => SetValue(PageTransitionProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to render the selection indicator on each tab item.
        /// </summary>
        public IDataTemplate? IndicatorTemplate
        {
            get => GetValue(IndicatorTemplateProperty);
            set => SetValue(IndicatorTemplateProperty, value);
        }

        internal ItemsPresenter? ItemsPresenterPart { get; private set; }

        internal ContentPresenter? ContentPart
        {
            get => _contentPart;
            private set => _contentPart = value;
        }

        /// <inheritdoc/>
        IAvaloniaList<ILogical> IContentPresenterHost.LogicalChildren => LogicalChildren;

        /// <inheritdoc/>
        bool IContentPresenterHost.RegisterContentPresenter(ContentPresenter presenter)
        {
            return RegisterContentPresenter(presenter);
        }

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new TabItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<TabItem>(item, out recycleKey);
        }

        protected internal override void PrepareContainerForItemOverride(Control element, object? item, int index)
        {
            base.PrepareContainerForItemOverride(element, item, index);

            if (element is TabItem tabItem)
            {
                tabItem.TabStripPlacement = TabStripPlacement;

                if (IndicatorTemplate is { } tmpl && !tabItem.IsSet(TabItem.IndicatorTemplateProperty))
                    tabItem.SetCurrentValue(TabItem.IndicatorTemplateProperty, tmpl);
            }

            if (index == SelectedIndex)
            {
                UpdateSelectedContent(element);
            }
        }

        protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
        {
            base.ContainerIndexChangedOverride(container, oldIndex, newIndex);

            var selectedIndex = SelectedIndex;

            if (selectedIndex == oldIndex || selectedIndex == newIndex)
                UpdateSelectedContent();
        }

        protected internal override void ClearContainerForItemOverride(Control element)
        {
            base.ClearContainerForItemOverride(element);
            UpdateSelectedContent();
        }

        protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs eventArgs) =>
            eventArgs.Properties.PointerUpdateKind is PointerUpdateKind.LeftButtonPressed or PointerUpdateKind.LeftButtonReleased && base.ShouldTriggerSelection(selectable, eventArgs);

        public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
        {
            if (eventArgs is GotFocusEventArgs { NavigationMethod: not NavigationMethod.Directional })
            {
                return false;
            }

            return base.UpdateSelectionFromEvent(container, eventArgs);
        }

        private void UpdateSelectedContent(Control? container = null)
        {
            _selectedItemSubscriptions?.Dispose();
            _selectedItemSubscriptions = null;

            _currentTransition?.Cancel();
            _currentTransition = null;
            _shouldAnimate = false;

            if (_contentPresenter2 is { IsVisible: true })
            {
                _contentPresenter2.IsVisible = false;
                _contentPresenter2.Content = null;
                _contentPresenter2.ContentTemplate = null;
                _contentPresenter2.DataContext = null;
            }

            int oldIndex = _previousSelectedIndex;
            _previousSelectedIndex = SelectedIndex;
            bool forward = SelectedIndex >= oldIndex || oldIndex < 0;

            if (SelectedIndex == -1)
            {
                SelectedContent = SelectedContentTemplate = null;
                if (ContentPart != null)
                {
                    ContentPart.Content = null;
                    ContentPart.ContentTemplate = null;
                    ContentPart.DataContext = null;
                }
                return;
            }

            container ??= ContainerFromIndex(SelectedIndex);

            if (container != null)
            {
                if (SelectedContentTemplate != SelectContentTemplate(container.GetValue(ContentControl.ContentTemplateProperty)))
                {
                    SelectedContentTemplate = null;
                    if (ContentPart != null)
                        ContentPart.ContentTemplate = null;
                }

                bool shouldTransition = PageTransition != null && _contentPresenter2 != null
                                        && VisualRoot != null && oldIndex >= 0
                                        && oldIndex != SelectedIndex;
                bool isInitialFire = true;

                _selectedItemSubscriptions = new CompositeDisposable(
                    container.GetObservable(StyledElement.DataContextProperty).Subscribe(dc =>
                    {
                        // The selected content presenter needs to inherit the DataContext of the TabItem, but
                        // the data context cannot be set directly on the ContentPresenter due to it calling
                        // ClearValue(DataContextProperty) in ContentPresenter.UpdateChild. For this reason we
                        // have a proxy element in the control template (PART_SelectedContentDataContextHost)
                        // which is used to set the DataContext inherited by the content presenters.
                        _dataContextHost?.DataContext = dc;
                    }),
                    container.GetObservable(ContentControl.ContentProperty).Subscribe(content =>
                    {
                        var contentElement = content as StyledElement;
                        var contentDataContext = contentElement?.DataContext;
                        SelectedContent = content;

                        if (isInitialFire && shouldTransition)
                        {
                            var template = SelectContentTemplate(container.GetValue(ContentControl.ContentTemplateProperty));
                            SelectedContentTemplate = template;

                            _contentPresenter2!.Content = content;
                            _contentPresenter2.ContentTemplate = template;
                            _contentPresenter2.IsVisible = true;
                            if (contentElement is not null && contentElement.DataContext != contentDataContext)
                                _contentPresenter2.DataContext = contentDataContext;

                            _pendingForward = forward;
                            _shouldAnimate = true;
                            InvalidateArrange();
                        }
                        else
                        {
                            ContentPart?.Content = content;
                        }

                        isInitialFire = false;
                    }),
                    container.GetObservable(ContentControl.ContentTemplateProperty).Subscribe(v =>
                    {
                        SelectedContentTemplate = SelectContentTemplate(v);
                        if (ContentPart != null && !_shouldAnimate)
                            ContentPart.ContentTemplate = _selectedContentTemplate;
                    }));

                IDataTemplate? SelectContentTemplate(IDataTemplate? containerTemplate) => containerTemplate ?? ContentTemplate;
            }
        }

        /// <summary>
        /// Called when a <see cref="ContentPresenter"/> is registered with the control.
        /// </summary>
        protected virtual bool RegisterContentPresenter(ContentPresenter presenter)
        {
            if (presenter.Name == "PART_SelectedContentHost")
            {
                ContentPart = presenter;
                ContentPart.Content = _selectedContent;
                ContentPart.ContentTemplate = _selectedContentTemplate;
                return true;
            }

            if (presenter.Name == "PART_SelectedContentHost2")
            {
                _contentPresenter2 = presenter;
                _contentPresenter2.IsVisible = false;
                return true;
            }

            return false;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            ItemsPresenterPart = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
            ItemsPresenterPart?.ApplyTemplate();

            _dataContextHost = e.NameScope.Find<Control>("PART_SelectedContentDataContextHost");

            // Initialize the data context host with the data context of the selected tab, if any.
            if (_dataContextHost is not null && ContainerFromIndex(SelectedIndex) is { } selectedTab)
                _dataContextHost.DataContext = selectedTab.DataContext;

            UpdateTabStripPlacement();

            if (ItemsPresenterPart?.Panel is { } panel)
            {
                if (!panel.IsSet(KeyboardNavigation.TabNavigationProperty))
                    panel.SetCurrentValue(
                        KeyboardNavigation.TabNavigationProperty,
                        KeyboardNavigationMode.Once);
                KeyboardNavigation.SetTabOnceActiveElement(
                    panel,
                    KeyboardNavigation.GetTabOnceActiveElement(this));
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            _currentTransition?.Cancel();
            _currentTransition?.Dispose();
            _currentTransition = null;
            _shouldAnimate = false;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var result = base.ArrangeOverride(finalSize);

            if (_shouldAnimate)
            {
                _shouldAnimate = false;
                _currentTransition?.Cancel();
                var cancel = new CancellationTokenSource();
                _currentTransition = cancel;
                var from = ContentPart;
                var to = _contentPresenter2;
                if (from != null && to != null && PageTransition != null)
                    _ = RunTransitionAsync(PageTransition, from, to, _pendingForward, cancel);
            }

            return result;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TabStripPlacementProperty)
            {
                RefreshContainers();
            }
            else if (change.Property == IndicatorTemplateProperty)
            {
                UpdateIndicatorTemplate();
            }
            else if (change.Property == ContentTemplateProperty)
            {
                var newTemplate = change.GetNewValue<IDataTemplate?>();
                if (SelectedContentTemplate != newTemplate &&
                    ContainerFromIndex(SelectedIndex) is { } container &&
                    container.GetValue(ContentControl.ContentTemplateProperty) == null)
                {
                    SelectedContentTemplate = newTemplate;
                    if (ContentPart != null && !_shouldAnimate)
                        ContentPart.ContentTemplate = newTemplate;
                }
            }
            else if (change.Property == KeyboardNavigation.TabOnceActiveElementProperty &&
                ItemsPresenterPart?.Panel is { } panel)
            {
                KeyboardNavigation.SetTabOnceActiveElement(
                    panel,
                    change.GetNewValue<IInputElement?>());
            }
        }

        private async Task RunTransitionAsync(
            IPageTransition transition,
            ContentPresenter from,
            ContentPresenter to,
            bool forward,
            CancellationTokenSource cts)
        {
            try
            {
                await transition.Start(from, to, forward, cts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, LogArea.Control)
                    ?.Log(this, "Tab transition threw an unhandled exception: {Exception}", e);
            }

            if (cts.IsCancellationRequested)
                return;

            from.IsVisible = false;
            from.Content = null;
            from.ContentTemplate = null;
            from.DataContext = null;
            from.RenderTransform = null;
            from.Opacity = 1;

            (_contentPart, _contentPresenter2) = (_contentPresenter2, _contentPart);
        }

        private void UpdateTabStripPlacement()
        {
            var controls = ItemsPresenterPart?.Panel?.Children;
            if (controls is null)
            {
                return;
            }

            foreach (var control in controls)
            {
                if (control is TabItem tabItem)
                {
                    tabItem.TabStripPlacement = TabStripPlacement;
                }
            }
        }

        private void UpdateIndicatorTemplate()
        {
            var controls = ItemsPresenterPart?.Panel?.Children;
            if (controls is null)
                return;

            var template = IndicatorTemplate;
            foreach (var control in controls)
            {
                if (control is TabItem tabItem && !tabItem.IsSet(TabItem.IndicatorTemplateProperty))
                {
                    if (template is not null)
                        tabItem.SetCurrentValue(TabItem.IndicatorTemplateProperty, template);
                    else
                        tabItem.ClearValue(TabItem.IndicatorTemplateProperty);
                }
            }
        }
    }
}
