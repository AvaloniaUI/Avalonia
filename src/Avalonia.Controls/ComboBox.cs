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
    [TemplatePart("PART_InputText", typeof(TextBox))]
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
        /// Defines the <see cref="IsEditable"/> property.
        /// </summary>
        public static readonly DirectProperty<ComboBox, bool> IsEditableProperty =
            AvaloniaProperty.RegisterDirect<ComboBox, bool>(
                nameof(IsEditable),
                o => o.IsEditable,
                (o, v) => o.IsEditable = v);

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

        /// <summary>
        /// Defines the <see cref="Text"/> property
        /// </summary>
        public static readonly DirectProperty<ComboBox, string?> TextProperty =
            TextBlock.TextProperty.AddOwner<ComboBox>(
                x => x.Text,
                (x, v) => x.Text = v,
                unsetValue: string.Empty,
                defaultBindingMode: Data.BindingMode.TwoWay);

        /// <summary>
        /// Defines the <see cref="SelectedItemTemplate"/> property.
        /// </summary>
        public static StyledProperty<IDataTemplate?> SelectedItemTemplateProperty =
            AvaloniaProperty.Register<ComboBox, IDataTemplate?>(nameof(SelectedItemTemplate));

        private bool _isDropDownOpen;
        private bool _isEditable;
        private Popup? _popup;
        private TextBox? _inputText;
        private object? _selectionBoxItem;
        private string? _text;
        private bool _ignoreNextInputTextUpdate;
        private readonly CompositeDisposable _subscriptionsOnOpen = new CompositeDisposable();

        /// <summary>
        /// Initializes static members of the <see cref="ComboBox"/> class.
        /// </summary>
        static ComboBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ComboBox>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<ComboBox>(true);
            IsTextSearchEnabledProperty.OverrideDefaultValue<ComboBox>(true);
            TextProperty.Changed.AddClassHandler<ComboBox>((x, e) => x.TextChanged(e));
            //when the items change we need to simulate a text change to validate the text being an item or not and selecting it
            ItemsProperty.Changed.AddClassHandler<ComboBox>((x, e) => x.TextChanged(
                new AvaloniaPropertyChangedEventArgs<string?>(e.Sender, TextProperty, x.Text, x.Text, e.Priority)));
        }

        /// <summary>
        /// Occurs after the drop-down (popup) list of the <see cref="ComboBox"/> closes.
        /// </summary>
        public event EventHandler? DropDownClosed;

        /// <summary>
        /// Occurs after the drop-down (popup) list of the <see cref="ComboBox"/> opens.
        /// </summary>
        public event EventHandler? DropDownOpened;

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown is currently open.
        /// </summary>
        public bool IsDropDownOpen
        {
            get => _isDropDownOpen;
            set => SetAndRaise(IsDropDownOpenProperty, ref _isDropDownOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is editable
        /// </summary>
        public bool IsEditable
        {
            get => _isEditable;
            set => SetAndRaise(IsEditableProperty, ref _isEditable, value);
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

        /// <summary>
        /// Gets or sets the text used when <see cref="IsEditable"/> is true.
        /// </summary>
        public string? Text
        {
            get => _text;
            set => SetAndRaise(TextProperty, ref _text, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display the item in the combo box when closed
        /// </summary>
        public IDataTemplate? SelectedItemTemplate
        {
            get => GetValue(SelectedItemTemplateProperty);
            set => SetValue(SelectedItemTemplateProperty, value);
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
            else if (!IsDropDownOpen && !IsEditable && (e.Key == Key.Enter || e.Key == Key.Space))
            {
                IsDropDownOpen = true;
                e.Handled = true;
            }
            else if (IsDropDownOpen && (e.Key == Key.Enter || e.Key == Key.Space))
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
            //if the user clicked in the input text we don't want to open the dropdown
            if (!e.Handled && e.Source is StyledElement styledSource && styledSource.TemplatedParent == _inputText)
                return;

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

            _inputText = e.NameScope.Get<TextBox>("PART_InputText");
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            if (change.Property == SelectedItemProperty)
            {
                UpdateSelectionBoxItem(change.NewValue);
                TryFocusSelectedItem();
                UpdateInputTextFromSelection(change.NewValue);
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

            DropDownClosed?.Invoke(this, EventArgs.Empty);
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

            DropDownOpened?.Invoke(this, EventArgs.Empty);
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

        private void UpdateInputTextFromSelection(object? item)
        {
            if (_ignoreNextInputTextUpdate)
                return;

            _ignoreNextInputTextUpdate = true;
            if (item is IContentControl cbItem)
                Text = cbItem.Content?.ToString() ?? string.Empty;
            else
                Text = item?.ToString() ?? string.Empty;
            _ignoreNextInputTextUpdate = false;
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

        private void TextChanged(AvaloniaPropertyChangedEventArgs e)
        {
            //don't check for an item if there are no items or if we are already processing a change
            if (Items == null || _ignoreNextInputTextUpdate)
                return;

            string newVal = e.GetNewValue<string>();
            int selectedIdx = -1;
            object? selectedItem = null;
            string? selectedItemText = null;
            int i = -1;
            foreach (object o in Items)
            {
                i++;
                string? text = o is IContentControl contentControl 
                    ? contentControl.Content?.ToString() 
                    : o.ToString();

                if (string.Equals(newVal, text, StringComparison.CurrentCultureIgnoreCase))
                {
                    selectedIdx = i;
                    selectedItem = o;
                    selectedItemText = text;
                    break;
                }
            }
            bool settingSelectedItem = selectedIdx > -1 && SelectedIndex != selectedIdx;

            _ignoreNextInputTextUpdate = true;
            SelectedIndex = selectedIdx;
            SelectedItem = selectedItem;
            if (settingSelectedItem)
                Text = selectedItemText ?? newVal;
            _ignoreNextInputTextUpdate = false;
        }
    }
}
