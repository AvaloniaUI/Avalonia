using System;
using System.Linq;
using Avalonia.Automation.Peers;
using System.Reactive.Disposables;
using Avalonia.Controls.Generators;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Controls.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// A drop-down list control.
    /// </summary>
    [TemplatePart("PART_Popup", typeof(Popup))]
    [PseudoClasses(pcDropdownOpen, pcPressed)]
    public class ComboBox : SelectingItemsControl
    {
        public const string pcDropdownOpen = ":dropdownopen";
        public const string pcPressed = ":pressed";
        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel> DefaultPanel =
            new FuncTemplate<Panel>(() => new VirtualizingStackPanel());

        /// <summary>
        /// Defines the <see cref="IsDropDownOpen"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, bool> IsDropDownOpenProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, bool>(
                nameof(IsDropDownOpen),
                o => o.IsDropDownOpen,
                (o, v) => o.IsDropDownOpen = v);

        /// <summary>
        /// Defines the <see cref="MaxDropDownHeight"/> property.
        /// </summary>
        public static readonly StyledProperty<double> MaxDropDownHeightProperty =
            AvaloniaProperty.Register<ComboBox, double>(nameof(MaxDropDownHeight), 200);

        /// <summary>
        /// Defines the <see cref="SelectionBoxItem"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, object?> SelectionBoxItemProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, object?>(nameof(SelectionBoxItem), o => o.SelectionBoxItem);

        /// <summary>
        /// Defines the <see cref="VirtualizationMode"/> property.
        /// </summary>
        public static readonly StyledProperty<ItemVirtualizationMode> VirtualizationModeProperty =
            ItemsPresenter.VirtualizationModeProperty.AddOwner<ComboBox>();

        /// <summary>
        /// Defines the <see cref="PlaceholderText"/> property.
        /// </summary>
        public static readonly StyledProperty<string?> PlaceholderTextProperty =
            AvaloniaProperty.Register<ComboBox, string?>(nameof(PlaceholderText));

        /// <summary>
        /// Defines the <see cref="PlaceholderForeground"/> property.
        /// </summary>
        public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
            AvaloniaProperty.Register<ComboBox, IBrush?>(nameof(PlaceholderForeground));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<ComboBox>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<ComboBox>();

        private bool _isDropDownOpen;
        private Popup? _popup;
        private object? _selectionBoxItem;
        private readonly CompositeDisposable _subscriptionsOnOpen = new CompositeDisposable();

        /// <summary>
        /// Initializes static members of the <see cref="ComboBox"/> class.
        /// </summary>
        static ComboBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ComboBox>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<ComboBox>(true);
            IsTextSearchEnabledProperty.OverrideDefaultValue<ComboBox>(true);

            KeyDownEvent.AddClassHandler<ComboBox>((x, e) => x.OnKeyDown(e), Interactivity.RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown is currently open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get => _isDropDownOpen;
            set => SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value);
        }

        /// <summary>
        /// Gets or sets the maximum height for the dropdown list.
        /// </summary>
        public double MaxDropDownHeight
        {
            get => GetValue(MaxDropDownHeightProperty);
            set => SetValue(MaxDropDownHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the item to display as the control's content.
        /// </summary>
        protected object? SelectionBoxItem
        {
            get => _selectionBoxItem;
            set => SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value);
        }

        /// <summary>
        /// Gets or sets the PlaceHolder text.
        /// </summary>
        public string? PlaceholderText
        {
            get => GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the Brush that renders the placeholder text.
        /// </summary>
        public IBrush? PlaceholderForeground
        {
            get => GetValue(PlaceholderForegroundProperty);
            set => SetValue(PlaceholderForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the virtualization mode for the items.
        /// </summary>
        public ItemVirtualizationMode VirtualizationMode
        {
            get => GetValue(VirtualizationModeProperty);
            set => SetValue(VirtualizationModeProperty, value);
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

        /// <inheritdoc/>
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<ComboBoxItem>(
                this,
                ComboBoxItem.ContentProperty,
                ComboBoxItem.ContentTemplateProperty);
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            UpdateSelectionBoxItem(SelectedItem);
        }

        public override void InvalidateMirrorTransform()
        {
            base.InvalidateMirrorTransform();
            UpdateFlowDirection();
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
                return;

            if ((e.Key == Key.F4 && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) == false) ||
                ((e.Key == Key.Down || e.Key == Key.Up) && e.KeyModifiers.HasAllFlags(KeyModifiers.Alt)))
            {
                IsDropDownOpen = !IsDropDownOpen;
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Escape)
            {
                IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Enter)
            {
                SelectFocusedItem();
                IsDropDownOpen = false;
                e.Handled = true;
            }
            else if (!IsDropDownOpen)
            {
                if (e.Key == Key.Down)
                {
                    SelectNext();
                    e.Handled = true;
                }
                else if (e.Key == Key.Up)
                {
                    SelectPrev();
                    e.Handled = true;
                }
            }
            // This part of code is needed just to acquire initial focus, subsequent focus navigation will be done by ItemsControl.
            else if (IsDropDownOpen && SelectedIndex < 0 && ItemCount > 0 &&
                     (e.Key == Key.Up || e.Key == Key.Down) && IsFocused == true)
            {
                var firstChild = Presenter?.Panel?.Children.FirstOrDefault(c => CanFocus(c));
                if (firstChild != null)
                {
                    FocusManager.Instance?.Focus(firstChild, NavigationMethod.Directional);
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            if (!e.Handled)
            {
                if (!IsDropDownOpen)
                {
                    if (IsFocused)
                    {
                        if (e.Delta.Y < 0)
                            SelectNext();
                        else
                            SelectPrev();

                        e.Handled = true;
                    }
                }
                else
                {
                    e.Handled = true;
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if(!e.Handled && e.Source is Visual source)
            {
                if (_popup?.IsInsidePopup(source) == true)
                {
                    return;
                }
            }
            PseudoClasses.Set(pcPressed, true);
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (!e.Handled && e.Source is Visual source)
            {
                if (_popup?.IsInsidePopup(source) == true)
                {
                    if (UpdateSelectionFromEventSource(e.Source))
                    {
                        _popup?.Close();
                        e.Handled = true;
                    }
                }
                else
                {
                    IsDropDownOpen = !IsDropDownOpen;
                    e.Handled = true;
                }
            }

            PseudoClasses.Set(pcPressed, false);
            base.OnPointerReleased(e);
        }

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            if (_popup != null)
            {
                _popup.Opened -= PopupOpened;
                _popup.Closed -= PopupClosed;
            }

            _popup = e.NameScope.Get<Popup>("PART_Popup");
            _popup.Opened += PopupOpened;
            _popup.Closed += PopupClosed;
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == SelectedItemProperty)
            {
                UpdateSelectionBoxItem(change.NewValue);
                TryFocusSelectedItem();
            }
            else if (change.Property == IsDropDownOpenProperty)
            {
                PseudoClasses.Set(pcDropdownOpen, change.GetNewValue<bool>());
            }

            base.OnPropertyChanged(change);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ComboBoxAutomationPeer(this);
        }

        internal void ItemFocused(ComboBoxItem dropDownItem)
        {
            if (IsDropDownOpen && dropDownItem.IsFocused && dropDownItem.IsArrangeValid)
            {
                dropDownItem.BringIntoView();
            }
        }

        private void PopupClosed(object? sender, EventArgs e)
        {
            _subscriptionsOnOpen.Clear();

            if (CanFocus(this))
            {
                Focus();
            }
        }

        private void PopupOpened(object? sender, EventArgs e)
        {
            TryFocusSelectedItem();

            _subscriptionsOnOpen.Clear();

            var toplevel = this.GetVisualRoot() as TopLevel;
            if (toplevel != null)
            {
                toplevel.AddDisposableHandler(PointerWheelChangedEvent, (s, ev) =>
                {
                    //eat wheel scroll event outside dropdown popup while it's open
                    if (IsDropDownOpen && (ev.Source as Visual)?.GetVisualRoot() == toplevel)
                    {
                        ev.Handled = true;
                    }
                }, Interactivity.RoutingStrategies.Tunnel).DisposeWith(_subscriptionsOnOpen);
            }

            this.GetObservable(IsVisibleProperty).Subscribe(IsVisibleChanged).DisposeWith(_subscriptionsOnOpen);

            foreach (var parent in this.GetVisualAncestors().OfType<Control>())
            {
                parent.GetObservable(IsVisibleProperty).Subscribe(IsVisibleChanged).DisposeWith(_subscriptionsOnOpen);
            }

            UpdateFlowDirection();
        }

        private void IsVisibleChanged(bool isVisible)
        {
            if (!isVisible && IsDropDownOpen)
            {
                IsDropDownOpen = false;
            }
        }

        private void TryFocusSelectedItem()
        {
            var selectedIndex = SelectedIndex;
            if (IsDropDownOpen && selectedIndex != -1)
            {
                var container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);

                if (container == null && SelectedIndex != -1)
                {
                    ScrollIntoView(Selection.SelectedIndex);
                    container = ItemContainerGenerator.ContainerFromIndex(selectedIndex);
                }

                if (container != null && CanFocus(container))
                {
                    container.Focus();
                }
            }
        }

        private bool CanFocus(Control control) => control.Focusable && control.IsEffectivelyEnabled && control.IsVisible;

        private void UpdateSelectionBoxItem(object? item)
        {
            var contentControl = item as IContentControl;

            if (contentControl != null)
            {
                item = contentControl.Content;
            }

            var control = item as Control;

            if (control != null)
            {
                if (VisualRoot is object)
                {
                    control.Measure(Size.Infinity);

                    SelectionBoxItem = new Rectangle
                    {
                        Width = control.DesiredSize.Width,
                        Height = control.DesiredSize.Height,
                        Fill = new VisualBrush
                        {
                            Visual = control,
                            Stretch = Stretch.None,
                            AlignmentX = AlignmentX.Left,
                        }
                    };
                }

                UpdateFlowDirection();
            }
            else
            {
                SelectionBoxItem = item;
            }
        }

        private void UpdateFlowDirection()
        {
            if (SelectionBoxItem is Rectangle rectangle)
            {
                if ((rectangle.Fill as VisualBrush)?.Visual is Visual content)
                {
                    var flowDirection = content.VisualParent?.FlowDirection ?? FlowDirection.LeftToRight;
                    rectangle.FlowDirection = flowDirection;
                }
            }
        }

        private void SelectFocusedItem()
        {
            foreach (ItemContainerInfo dropdownItem in ItemContainerGenerator.Containers)
            {
                if (dropdownItem.ContainerControl.IsFocused)
                {
                    SelectedIndex = dropdownItem.Index;
                    break;
                }
            }
        }

        private void SelectNext()
        {
            if (ItemCount >= 1)
            {
                MoveSelection(NavigationDirection.Next, WrapSelection);
            }
        }

        private void SelectPrev()
        {
            if (ItemCount >= 1)
            {
                MoveSelection(NavigationDirection.Previous, WrapSelection);
            }
        }
    }
}
