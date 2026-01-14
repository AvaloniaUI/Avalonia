using System;
using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// A drop-down list control.
    /// </summary>
    [TemplatePart("PART_Popup", typeof(Popup), IsRequired = true)]
    [TemplatePart("PART_EditableTextBox", typeof(TextBox), IsRequired = false)]
    [PseudoClasses(pcDropdownOpen, pcPressed)]
    public class ComboBox : SelectingItemsControl
    {
        internal const string pcDropdownOpen = ":dropdownopen";
        internal const string pcPressed = ":pressed";

        /// <summary>
        /// The default value for the <see cref="ItemsControl.ItemsPanel"/> property.
        /// </summary>
        private static readonly FuncTemplate<Panel?> DefaultPanel =
            new(() => new VirtualizingStackPanel());

        /// <summary>
        /// Defines the <see cref="IsDropDownOpen"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsDropDownOpenProperty =
            AvaloniaProperty.Register<ComboBox, bool>(nameof(IsDropDownOpen));

        /// <summary>
        /// Defines the <see cref="IsEditable"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> IsEditableProperty =
            AvaloniaProperty.Register<ComboBox, bool>(nameof(IsEditable));

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
        public static readonly StyledProperty<string?> TextProperty =
            TextBlock.TextProperty.AddOwner<ComboBox>(new(string.Empty, BindingMode.TwoWay));

        /// <summary>
        /// Defines the <see cref="SelectionBoxItemTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> SelectionBoxItemTemplateProperty =
            AvaloniaProperty.Register<ComboBox, IDataTemplate?>(
                nameof(SelectionBoxItemTemplate), defaultBindingMode: BindingMode.TwoWay, coerce: CoerceSelectionBoxItemTemplate);
        
        private static IDataTemplate? CoerceSelectionBoxItemTemplate(AvaloniaObject obj, IDataTemplate? template)
        {
            if (template is not null) return template;
            if(obj is ComboBox comboBox && template is null)
            {
                return comboBox.ItemTemplate;
            }
            return template;
        }

        private Popup? _popup;
        private object? _selectionBoxItem;
        private readonly CompositeDisposable _subscriptionsOnOpen = new CompositeDisposable();

        private TextBox? _inputTextBox;
        private BindingEvaluator<string?>? _textValueBindingEvaluator = null;
        private bool _skipNextTextChanged = false;

        /// <summary>
        /// Initializes static members of the <see cref="ComboBox"/> class.
        /// </summary>
        static ComboBox()
        {
            ItemsPanelProperty.OverrideDefaultValue<ComboBox>(DefaultPanel);
            FocusableProperty.OverrideDefaultValue<ComboBox>(true);
            IsTextSearchEnabledProperty.OverrideDefaultValue<ComboBox>(true);
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
            get => GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the control is editable
        /// </summary>
        public bool IsEditable
        {
            get => GetValue(IsEditableProperty);
            set => SetValue(IsEditableProperty, value);
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
        public object? SelectionBoxItem
        {
            get => _selectionBoxItem;
            protected set => SetAndRaise(SelectionBoxItemProperty, ref _selectionBoxItem, value);
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
        /// Gets or sets the DataTemplate used to display the selected item. This has a higher priority than <see cref="ItemsControl.ItemTemplate"/> if set.
        /// </summary>
        [InheritDataTypeFromItems(nameof(ItemsSource))]
        public IDataTemplate? SelectionBoxItemTemplate
        {
            get => GetValue(SelectionBoxItemTemplateProperty);
            set => SetValue(SelectionBoxItemTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the text used when <see cref="IsEditable"/> is true.
        /// Does nothing if not <see cref="IsEditable"/>.
        /// </summary>
        public string? Text
        {
            get => GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            UpdateSelectionBoxItem(SelectedItem);
        }

        protected internal override void InvalidateMirrorTransform()
        {
            base.InvalidateMirrorTransform();
            UpdateFlowDirection();
        }

        protected internal override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey)
        {
            return new ComboBoxItem();
        }

        protected internal override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        {
            return NeedsContainer<ComboBoxItem>(item, out recycleKey);
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
                SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Escape)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
            }
            else if (!IsDropDownOpen && !IsEditable && (e.Key == Key.Enter || e.Key == Key.Space))
            {
                SetCurrentValue(IsDropDownOpenProperty, true);
                e.Handled = true;
            }
            else if (IsDropDownOpen && e.Key == Key.Tab)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
            // Ignore key buttons, if they are used for XY focus.
            else if (!IsDropDownOpen
                     && !XYFocusHelpers.IsAllowedXYNavigationMode(this, e.KeyDeviceType))
            {
                if (e.Key == Key.Down)
                {
                    e.Handled = SelectNext();
                }
                else if (e.Key == Key.Up)
                {
                    e.Handled = SelectPrevious();
                }
            }
            // This part of code is needed just to acquire initial focus, subsequent focus navigation will be done by ItemsControl.
            else if (IsDropDownOpen && SelectedIndex < 0 && ItemCount > 0 &&
                     (e.Key == Key.Up || e.Key == Key.Down) && IsFocused == true)
            {
                var firstChild = Presenter?.Panel?.Children.FirstOrDefault(c => CanFocus(c));
                if (firstChild != null)
                {
                    e.Handled = firstChild.Focus(NavigationMethod.Directional);
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
                        e.Handled = e.Delta.Y < 0 ? SelectNext() : SelectPrevious();
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
                    e.Handled = true;
                    return;
                }
            }

            if (IsDropDownOpen)
            {
                // When a drop-down is open with OverlayDismissEventPassThrough enabled and the control
                // is pressed, close the drop-down
                SetCurrentValue(IsDropDownOpenProperty, false);
                e.Handled = true;
            }
            else
            {
                PseudoClasses.Set(pcPressed, true);
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            //if the user clicked in the input text we don't want to open the dropdown
            if (_inputTextBox != null
                && !e.Handled
                && e.Source is StyledElement styledSource
                && styledSource.TemplatedParent == _inputTextBox)
            {
                return;
            }

            if (!e.Handled && e.Source is Visual source)
            {
                if (_popup?.IsInsidePopup(source) != true && PseudoClasses.Contains(pcPressed))
                {
                    SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
                    e.Handled = true;
                }
            }

            PseudoClasses.Set(pcPressed, false);
            base.OnPointerReleased(e);
        }

        public override bool UpdateSelectionFromEvent(Control container, RoutedEventArgs eventArgs)
        {
            if (base.UpdateSelectionFromEvent(container, eventArgs))
            {
                _popup?.Close();
                return true;
            }

            return false;
        }

        protected override bool ShouldTriggerSelection(Visual selectable, PointerEventArgs eventArgs) =>
            ItemSelectionEventTriggers.IsPointerEventWithinBounds(selectable, eventArgs) &&
            eventArgs is { Properties.PointerUpdateKind: PointerUpdateKind.LeftButtonReleased or PointerUpdateKind.RightButtonReleased } &&
            eventArgs.RoutedEvent == PointerReleasedEvent;

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

            _inputTextBox = e.NameScope.Find<TextBox>("PART_EditableTextBox");
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
            else if (change.Property == ItemTemplateProperty)
            {
                CoerceValue(SelectionBoxItemTemplateProperty);
            }
            else if (change.Property == IsEditableProperty && change.GetNewValue<bool>())
            {
                UpdateInputTextFromSelection(SelectedItem);
            }
            else if (change.Property == TextProperty)
            {
                TextChanged(change.GetNewValue<string>());
            }
            else if (change.Property == ItemsSourceProperty)
            {
                //the base handler deselects the current item (and resets Text) so we want to run the base first, then try match by text
                string? text = Text;
                base.OnPropertyChanged(change);
                SetCurrentValue(TextProperty, text);
                return;
            }
            else if (change.Property == DisplayMemberBindingProperty)
            {
                HandleTextValueBindingValueChanged(null, change);
            }
            else if (change.Property == TextSearch.TextBindingProperty)
            {
                HandleTextValueBindingValueChanged(change, null);
            }
            base.OnPropertyChanged(change);
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new ComboBoxAutomationPeer(this);
        }

        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            if (IsEditable && _inputTextBox != null)
            {
                _inputTextBox.Focus();
                _inputTextBox.SelectAll();
            }

            base.OnGotFocus(e);
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

            if(IsEditable && CanFocus(this))
            {
                Focus();
            }

            DropDownClosed?.Invoke(this, EventArgs.Empty);
        }

        private void PopupOpened(object? sender, EventArgs e)
        {
            TryFocusSelectedItem();

            _subscriptionsOnOpen.Clear();

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
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        private void TryFocusSelectedItem()
        {
            var selectedIndex = SelectedIndex;
            if (IsDropDownOpen && selectedIndex != -1)
            {
                var container = ContainerFromIndex(selectedIndex);

                if (container == null && SelectedIndex != -1)
                {
                    ScrollIntoView(Selection.SelectedIndex);
                    container = ContainerFromIndex(selectedIndex);
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
                if (item is not null && ItemTemplate is null && SelectionBoxItemTemplate is null && DisplayMemberBinding is { } binding)
                {
                    var template = new FuncDataTemplate<object?>((_, _) =>
                    new TextBlock
                    {
                        [TextBlock.DataContextProperty] = item,
                        [!TextBlock.TextProperty] = binding,
                    });
                    var text = template.Build(item);
                    SelectionBoxItem = text;
                }
                else
                {
                    SelectionBoxItem = item;
                }
                
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

        private void UpdateInputTextFromSelection(object? item)
        {
            //if we are modifying the text box which has deselected a value we don't want to update the textbox value
            if (_skipNextTextChanged)
                return;
            SetCurrentValue(TextProperty, GetItemTextValue(item));
        }

        private bool SelectNext() => MoveSelection(SelectedIndex, 1, WrapSelection);
        private bool SelectPrevious() => MoveSelection(SelectedIndex, -1, WrapSelection);

        private bool MoveSelection(int startIndex, int step, bool wrap)
        {
            static bool IsSelectable(object? o) => (o as AvaloniaObject)?.GetValue(IsEnabledProperty) ?? true;

            var count = ItemCount;

            for (int i = startIndex + step; i != startIndex; i += step)
            {
                if (i < 0 || i >= count)
                {
                    if (wrap)
                    {
                        if (i < 0)
                            i += count;
                        else if (i >= count)
                            i %= count;
                    }
                    else
                    {
                        return false;
                    }
                }

                var item = ItemsView[i];
                var container = ContainerFromIndex(i);
                
                if (IsSelectable(item) && IsSelectable(container))
                {
                    SelectedIndex = i;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the selection
        /// </summary>
        public void Clear()
        {
            SelectedItem = null;
            SelectedIndex = -1;
        }

        private void HandleTextValueBindingValueChanged(AvaloniaPropertyChangedEventArgs? textSearchPropChange,
            AvaloniaPropertyChangedEventArgs? displayMemberPropChange)
        {
            BindingBase? textValueBinding;
            //prioritise using the TextSearch.TextBindingProperty if possible
            if (textSearchPropChange == null && TextSearch.GetTextBinding(this) is BindingBase textSearchBinding)
                textValueBinding = textSearchBinding;

            else if (textSearchPropChange != null && textSearchPropChange.NewValue is BindingBase eventTextSearchBinding)
                textValueBinding = eventTextSearchBinding;

            else if (displayMemberPropChange != null && displayMemberPropChange.NewValue is BindingBase eventDisplayMemberBinding)
                textValueBinding = eventDisplayMemberBinding;

            else
                textValueBinding = null;

            if (_textValueBindingEvaluator == null)
                _textValueBindingEvaluator = BindingEvaluator<string?>.TryCreate(textValueBinding);
            else if (textValueBinding == null)
                _textValueBindingEvaluator = null;
            else
                _textValueBindingEvaluator.UpdateBinding(textValueBinding);

            //if the binding is set we want to set the initial value for the selected item so the text box has the correct value
            if (_textValueBindingEvaluator != null)
                _textValueBindingEvaluator.Value = GetItemTextValue(SelectedValue);
        }

        private void TextChanged(string? newValue)
        {
            if (!IsEditable || _skipNextTextChanged)
                return;

            int selectedIdx = -1;
            object? selectedItem = null;
            int i = -1;
            foreach (object? item in Items)
            {
                i++;
                string itemText = GetItemTextValue(item);
                if (string.Equals(newValue, itemText, StringComparison.CurrentCultureIgnoreCase))
                {
                    selectedIdx = i;
                    selectedItem = item;
                    break;
                }
            }

            _skipNextTextChanged = true;
            try
            {
                SelectedIndex = selectedIdx;
                SelectedItem = selectedItem;
            }
            finally
            {
                _skipNextTextChanged = false;
            }

            //when changing the SelectedIndex it will call: KeyboardNavigation.SetTabOnceActiveElement(this, [combo box item]);
            //this will then break tab navigation back into this combobox as it will try to focus the combo box item
            //rather than the combobox or editable text box, so we need to SetTabOnceActiveElement to null
            KeyboardNavigation.SetTabOnceActiveElement(this, null);
        }

        private string GetItemTextValue(object? item) 
            => TextSearch.GetEffectiveText(item, _textValueBindingEvaluator);
    }
}
