using System.Linq;
using Avalonia.Collections;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Automation;
using Avalonia.Controls.Metadata;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// A tab control that displays a tab strip along with the content of the selected tab.
    /// </summary>
    [TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
    public class TabControl : SelectingItemsControl, IContentPresenterHost
    {
        private object? _selectedContent;
        private IDataTemplate? _selectedContentTemplate;
        private CompositeDisposable? _selectedItemSubscriptions;

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
        /// The selected content property
        /// </summary>
        public static readonly DirectProperty<TabControl, object?> SelectedContentProperty =
            AvaloniaProperty.RegisterDirect<TabControl, object?>(nameof(SelectedContent), o => o.SelectedContent);

        /// <summary>
        /// The selected content template property
        /// </summary>
        public static readonly DirectProperty<TabControl, IDataTemplate?> SelectedContentTemplateProperty =
            AvaloniaProperty.RegisterDirect<TabControl, IDataTemplate?>(nameof(SelectedContentTemplate), o => o.SelectedContentTemplate);
        
        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
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
            SelectedItemProperty.Changed.AddClassHandler<TabControl>((x, e) => x.UpdateSelectedContent());
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
        /// <value>
        /// The content of the selected tab.
        /// </value>
        public object? SelectedContent
        {
            get => _selectedContent;
            internal set => SetAndRaise(SelectedContentProperty, ref _selectedContent, value);
        }

        /// <summary>
        /// Gets or sets the content template for the selected tab.
        /// </summary>
        /// <value>
        /// The content template of the selected tab.
        /// </value>
        public IDataTemplate? SelectedContentTemplate
        {
            get => _selectedContentTemplate;
            internal set => SetAndRaise(SelectedContentTemplateProperty, ref _selectedContentTemplate, value);
        }

        internal ItemsPresenter? ItemsPresenterPart { get; private set; }

        internal ContentPresenter? ContentPart { get; private set; }

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

        private void UpdateSelectedContent(Control? container = null)
        {
            _selectedItemSubscriptions?.Dispose();
            _selectedItemSubscriptions = null;

            if (SelectedIndex == -1)
            {
                SelectedContent = SelectedContentTemplate = null;
            }
            else
            {
                container ??= ContainerFromIndex(SelectedIndex);
                if (container != null)
                {
                    _selectedItemSubscriptions = new CompositeDisposable(
                        container.GetObservable(ContentControl.ContentProperty).Subscribe(v => SelectedContent = v),
                        // Note how we fall back to our own ContentTemplate if the container doesn't specify one
                        container.GetObservable(ContentControl.ContentTemplateProperty).Subscribe(v => SelectedContentTemplate = v ?? ContentTemplate));
                }
            }
        }

        /// <summary>
        /// Called when an <see cref="ContentPresenter"/> is registered with the control.
        /// </summary>
        /// <param name="presenter">The presenter.</param>
        protected virtual bool RegisterContentPresenter(ContentPresenter presenter)
        {
            if (presenter.Name == "PART_SelectedContentHost")
            {
                ContentPart = presenter;
                return true;
            }

            return false;
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            ItemsPresenterPart = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
            ItemsPresenterPart?.ApplyTemplate();

            UpdateTabStripPlacement();

            // Set TabNavigation to Once on the panel if not already set and
            // forward the TabOnceActiveElement to the panel.
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

        /// <inheritdoc/>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);

            if (e.NavigationMethod == NavigationMethod.Directional)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Pointer.Type == PointerType.Mouse)
            {
                e.Handled = UpdateSelectionFromEventSource(e.Source);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left && e.Pointer.Type != PointerType.Mouse)
            {
                var container = GetContainerFromEventSource(e.Source);
                if (container != null
                    && container.GetVisualsAt(e.GetPosition(container))
                        .Any(c => container == c || container.IsVisualAncestorOf(c)))
                {
                    e.Handled = UpdateSelectionFromEventSource(e.Source);
                }
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == TabStripPlacementProperty)
            {
                RefreshContainers();
            }
            else if (change.Property == ContentTemplateProperty)
            {
                var newTemplate = change.GetNewValue<IDataTemplate?>();
                if (SelectedContentTemplate != newTemplate &&
                    ContainerFromIndex(SelectedIndex) is { } container && 
                    container.GetValue(ContentControl.ContentTemplateProperty) == null)
                {
                    SelectedContentTemplate = newTemplate; // See also UpdateSelectedContent
                }
            }
            else if (change.Property == KeyboardNavigation.TabOnceActiveElementProperty &&
                ItemsPresenterPart?.Panel is { } panel)
            {
                // Forward TabOnceActiveElement to the panel.
                KeyboardNavigation.SetTabOnceActiveElement(
                    panel,
                    change.GetNewValue<IInputElement?>());
            }
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
    }
}
