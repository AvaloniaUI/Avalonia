// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Avalonia.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    /// <summary>
    /// Represents the filter used by the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control to
    /// determine whether an item is a possible match for the specified text.
    /// </summary>
    /// <returns>true to indicate <paramref name="item" /> is a possible match
    /// for <paramref name="search" />; otherwise false.</returns>
    /// <param name="search">The string used as the basis for filtering.</param>
    /// <param name="item">The item that is compared with the
    /// <paramref name="search" /> parameter.</param>
    /// <typeparam name="T">The type used for filtering the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />. This type can
    /// be either a string or an object.</typeparam>
    public delegate bool AutoCompleteFilterPredicate<T>(string? search, T item);

    /// <summary>
    /// Represents the selector used by the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control to
    /// determine how the specified text should be modified with an item.
    /// </summary>
    /// <returns>
    /// Modified text that will be used by the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />.
    /// </returns>
    /// <param name="search">The string used as the basis for filtering.</param>
    /// <param name="item">
    /// The selected item that should be combined with the
    /// <paramref name="search" /> parameter.
    /// </param>
    /// <typeparam name="T">
    /// The type used for filtering the
    /// <see cref="T:Avalonia.Controls.AutoCompleteBox" />.
    /// This type can be either a string or an object.
    /// </typeparam>
    public delegate string AutoCompleteSelector<T>(string? search, T item);

    /// <summary>
    /// Represents a control that provides a text box for user input and a
    /// drop-down that contains possible matches based on the input in the text
    /// box.
    /// </summary>
    [TemplatePart(ElementPopup,            typeof(Popup))]
    [TemplatePart(ElementSelector,         typeof(SelectingItemsControl))]
    [TemplatePart(ElementSelectionAdapter, typeof(ISelectionAdapter))]
    [TemplatePart(ElementTextBox,          typeof(TextBox))]
    [PseudoClasses(":dropdownopen")]
    public partial class AutoCompleteBox : TemplatedControl
    {
        /// <summary>
        /// Specifies the name of the selection adapter TemplatePart.
        /// </summary>
        private const string ElementSelectionAdapter = "PART_SelectionAdapter";

        /// <summary>
        /// Specifies the name of the Selector TemplatePart.
        /// </summary>
        private const string ElementSelector = "PART_SelectingItemsControl";

        /// <summary>
        /// Specifies the name of the Popup TemplatePart.
        /// </summary>
        private const string ElementPopup = "PART_Popup";

        /// <summary>
        /// The name for the text box part.
        /// </summary>
        private const string ElementTextBox = "PART_TextBox";

        /// <summary>
        /// Gets or sets a local cached copy of the items data.
        /// </summary>
        private List<object>? _items;

        /// <summary>
        /// Gets or sets the observable collection that contains references to
        /// all of the items in the generated view of data that is provided to
        /// the selection-style control adapter.
        /// </summary>
        private AvaloniaList<object>? _view;

        /// <summary>
        /// Gets or sets a value to ignore a number of pending change handlers.
        /// The value is decremented after each use. This is used to reset the
        /// value of properties without performing any of the actions in their
        /// change handlers.
        /// </summary>
        /// <remarks>The int is important as a value because the TextBox
        /// TextChanged event does not immediately fire, and this will allow for
        /// nested property changes to be ignored.</remarks>
        private int _ignoreTextPropertyChange;

        /// <summary>
        /// Gets or sets a value indicating whether to ignore calling a pending
        /// change handlers.
        /// </summary>
        private bool _ignorePropertyChange;

        /// <summary>
        /// Gets or sets a value indicating whether to ignore the selection
        /// changed event.
        /// </summary>
        private bool _ignoreTextSelectionChange;

        /// <summary>
        /// Gets or sets a value indicating whether to skip the text update
        /// processing when the selected item is updated.
        /// </summary>
        private bool _skipSelectedItemTextUpdate;

        /// <summary>
        /// Gets or sets the last observed text box selection start location.
        /// </summary>
        private int _textSelectionStart;

        /// <summary>
        /// Gets or sets a value indicating whether the user initiated the
        /// current populate call.
        /// </summary>
        private bool _userCalledPopulate;

        /// <summary>
        /// A value indicating whether the popup has been opened at least once.
        /// </summary>
        private bool _popupHasOpened;

        /// <summary>
        /// Gets or sets the DispatcherTimer used for the MinimumPopulateDelay
        /// condition for auto completion.
        /// </summary>
        private DispatcherTimer? _delayTimer;

        /// <summary>
        /// Gets or sets a value indicating whether a read-only dependency
        /// property change handler should allow the value to be set.  This is
        /// used to ensure that read-only properties cannot be changed via
        /// SetValue, etc.
        /// </summary>
        private bool _allowWrite;

        /// <summary>
        /// A boolean indicating if a cancellation was requested
        /// </summary>
        private bool _cancelRequested;

        /// <summary>
        /// A boolean indicating if filtering is in action
        /// </summary>
        private bool _filterInAction;

        /// <summary>
        /// The TextBox template part.
        /// </summary>
        private TextBox? _textBox;
        private IDisposable? _textBoxSubscriptions;

        /// <summary>
        /// The SelectionAdapter.
        /// </summary>
        private ISelectionAdapter? _adapter;

        /// <summary>
        /// A control that can provide updated string values from a binding.
        /// </summary>
        private BindingEvaluator<string>? _valueBindingEvaluator;

        /// <summary>
        /// A weak subscription for the collection changed event.
        /// </summary>
        private IDisposable? _collectionChangeSubscription;

        private CancellationTokenSource? _populationCancellationTokenSource;

        private bool _itemTemplateIsFromValueMemberBinding = true;
        private bool _settingItemTemplateFromValueMemberBinding;

        private bool _isFocused = false;

        private string? _searchText = string.Empty;

        private readonly EventHandler _populateDropDownHandler;

        /// <summary>
        /// 
        /// </summary>
        public static readonly RoutedEvent<SelectionChangedEventArgs> SelectionChangedEvent =
            RoutedEvent.Register<SelectionChangedEventArgs>(
                nameof(SelectionChanged),
                RoutingStrategies.Bubble,
                typeof(AutoCompleteBox));

        /// <summary>
        /// Defines the <see cref="TextChanged"/> event.
        /// </summary>
        public static readonly RoutedEvent<TextChangedEventArgs> TextChangedEvent =
            RoutedEvent.Register<AutoCompleteBox, TextChangedEventArgs>(
                nameof(TextChanged),
                RoutingStrategies.Bubble);

        private static bool IsValidMinimumPrefixLength(int value) => value >= -1;

        private static bool IsValidMinimumPopulateDelay(TimeSpan value) => value.TotalMilliseconds >= 0.0;

        private static bool IsValidMaxDropDownHeight(double value) => value >= 0.0;

        private static bool IsValidFilterMode(AutoCompleteFilterMode mode)
        {
            switch (mode)
            {
                case AutoCompleteFilterMode.None:
                case AutoCompleteFilterMode.StartsWith:
                case AutoCompleteFilterMode.StartsWithCaseSensitive:
                case AutoCompleteFilterMode.StartsWithOrdinal:
                case AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive:
                case AutoCompleteFilterMode.Contains:
                case AutoCompleteFilterMode.ContainsCaseSensitive:
                case AutoCompleteFilterMode.ContainsOrdinal:
                case AutoCompleteFilterMode.ContainsOrdinalCaseSensitive:
                case AutoCompleteFilterMode.Equals:
                case AutoCompleteFilterMode.EqualsCaseSensitive:
                case AutoCompleteFilterMode.EqualsOrdinal:
                case AutoCompleteFilterMode.EqualsOrdinalCaseSensitive:
                case AutoCompleteFilterMode.Custom:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Handle the change of the IsEnabled property.
        /// </summary>
        /// <param name="e">The event data.</param>
        private void OnControlIsEnabledChanged(AvaloniaPropertyChangedEventArgs e)
        {
            bool isEnabled = (bool)e.NewValue!;
            if (!isEnabled)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        /// <summary>
        /// MinimumPopulateDelayProperty property changed handler. Any current
        /// dispatcher timer will be stopped. The timer will not be restarted
        /// until the next TextUpdate call by the user.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnMinimumPopulateDelayChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var newValue = (TimeSpan)e.NewValue!;

            // Stop any existing timer
            if (_delayTimer != null)
            {
                _delayTimer.Stop();

                if (newValue == TimeSpan.Zero)
                {
                    _delayTimer.Tick -= _populateDropDownHandler;
                    _delayTimer = null;
                }
            }

            if (newValue > TimeSpan.Zero)
            {
                // Create or clear a dispatcher timer instance
                if (_delayTimer == null)
                {
                    _delayTimer = new DispatcherTimer();
                    _delayTimer.Tick += _populateDropDownHandler;
                }

                // Set the new tick interval
                _delayTimer.Interval = newValue;
            }
        }

        /// <summary>
        /// IsDropDownOpenProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnIsDropDownOpenChanged(AvaloniaPropertyChangedEventArgs e)
        {
            // Ignore the change if requested
            if (_ignorePropertyChange)
            {
                _ignorePropertyChange = false;
                return;
            }

            bool oldValue = (bool)e.OldValue!;
            bool newValue = (bool)e.NewValue!;

            if (newValue)
            {
                TextUpdated(Text, true);
            }
            else
            {
                ClosingDropDown(oldValue);
            }

            UpdatePseudoClasses();
        }

        private void OnSelectedItemPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_ignorePropertyChange)
            {
                _ignorePropertyChange = false;
                return;
            }

            // Update the text display
            if (_skipSelectedItemTextUpdate)
            {
                _skipSelectedItemTextUpdate = false;
            }
            else
            {
                OnSelectedItemChanged(e.NewValue);
            }

            // Fire the SelectionChanged event
            List<object> removed = new List<object>();
            if (e.OldValue != null)
            {
                removed.Add(e.OldValue);
            }

            List<object> added = new List<object>();
            if (e.NewValue != null)
            {
                added.Add(e.NewValue);
            }

            OnSelectionChanged(new SelectionChangedEventArgs(SelectionChangedEvent, removed, added));
        }

        /// <summary>
        /// TextProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnTextPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            TextUpdated((string?)e.NewValue, false);
        }

        private void OnSearchTextPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (_ignorePropertyChange)
            {
                _ignorePropertyChange = false;
                return;
            }

            // Ensure the property is only written when expected
            if (!_allowWrite)
            {
                // Reset the old value before it was incorrectly written
                _ignorePropertyChange = true;
                SetCurrentValue(e.Property, e.OldValue);

                throw new InvalidOperationException("Cannot set read-only property SearchText.");
            }
        }

        /// <summary>
        /// FilterModeProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnFilterModePropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            AutoCompleteFilterMode mode = (AutoCompleteFilterMode)e.NewValue!;

            // Sets the filter predicate for the new value
            SetCurrentValue(TextFilterProperty, AutoCompleteSearch.GetFilter(mode));
        }

        /// <summary>
        /// ItemFilterProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnItemFilterPropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var value = e.NewValue as AutoCompleteFilterPredicate<object>;

            // If null, revert to the "None" predicate
            if (value == null)
            {
                SetCurrentValue(FilterModeProperty, AutoCompleteFilterMode.None);
            }
            else
            {
                SetCurrentValue(FilterModeProperty, AutoCompleteFilterMode.Custom);
                SetCurrentValue(TextFilterProperty, null);
            }
        }

        /// <summary>
        /// ItemsSourceProperty property changed handler.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnItemsSourcePropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            OnItemsSourceChanged((IEnumerable?)e.NewValue);
        }

        private void OnItemTemplatePropertyChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (!_settingItemTemplateFromValueMemberBinding)
                _itemTemplateIsFromValueMemberBinding = false;
        }
        private void OnValueMemberBindingChanged(IBinding? value)
        {
            if (_itemTemplateIsFromValueMemberBinding)
            {
                var template =
                    new FuncDataTemplate(
                        typeof(object),
                        (o, _) =>
                        {
                            var control = new ContentControl();
                            if (value is not null)
                                control.Bind(ContentControl.ContentProperty, value);
                            return control;
                        });

                _settingItemTemplateFromValueMemberBinding = true;
                SetCurrentValue(ItemTemplateProperty, template);
                _settingItemTemplateFromValueMemberBinding = false;
            }
        }

        static AutoCompleteBox()
        {
            FocusableProperty.OverrideDefaultValue<AutoCompleteBox>(true);
            IsTabStopProperty.OverrideDefaultValue<AutoCompleteBox>(false);

            MinimumPopulateDelayProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnMinimumPopulateDelayChanged(e));
            IsDropDownOpenProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnIsDropDownOpenChanged(e));
            SelectedItemProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnSelectedItemPropertyChanged(e));
            TextProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnTextPropertyChanged(e));
            SearchTextProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnSearchTextPropertyChanged(e));
            FilterModeProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnFilterModePropertyChanged(e));
            ItemFilterProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnItemFilterPropertyChanged(e));
            ItemsSourceProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnItemsSourcePropertyChanged(e));
            ItemTemplateProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnItemTemplatePropertyChanged(e));
            IsEnabledProperty.Changed.AddClassHandler<AutoCompleteBox>((x,e) => x.OnControlIsEnabledChanged(e));
        }

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> class.
        /// </summary>
        public AutoCompleteBox()
        {
            _populateDropDownHandler = PopulateDropDown;
            ClearView();
        }

        /// <summary>
        /// Gets or sets the drop down popup control.
        /// </summary>
        private Popup? DropDownPopup { get; set; }

        /// <summary>
        /// Gets or sets the Text template part.
        /// </summary>
        private TextBox? TextBox
        {
            get => _textBox;
            set
            {
                _textBoxSubscriptions?.Dispose();
                _textBox = value;

                // Attach handlers
                if (_textBox != null)
                {
                    _textBoxSubscriptions =
                        _textBox.GetObservable(TextBox.TextProperty)
                                .Skip(1)
                                .Subscribe(_ => OnTextBoxTextChanged());

                    if (Text != null)
                    {
                        UpdateTextValue(Text);
                    }
                }
            }
        }

        private int TextBoxSelectionStart
        {
            get
            {
                if (TextBox != null)
                {
                    return Math.Min(TextBox.SelectionStart, TextBox.SelectionEnd);
                }
                else
                {
                    return 0;
                }
            }
        }
        private int TextBoxSelectionLength
        {
            get
            {
                if (TextBox != null)
                {
                    return Math.Abs(TextBox.SelectionEnd - TextBox.SelectionStart);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Gets or sets the selection adapter used to populate the drop-down
        /// with a list of selectable items.
        /// </summary>
        /// <value>The selection adapter used to populate the drop-down with a
        /// list of selectable items.</value>
        /// <remarks>
        /// You can use this property when you create an automation peer to
        /// use with AutoCompleteBox or deriving from AutoCompleteBox to
        /// create a custom control.
        /// </remarks>
        protected ISelectionAdapter? SelectionAdapter
        {
            get => _adapter;
            set
            {
                if (_adapter != null)
                {
                    _adapter.SelectionChanged -= OnAdapterSelectionChanged;
                    _adapter.Commit -= OnAdapterSelectionComplete;
                    _adapter.Cancel -= OnAdapterSelectionCanceled;
                    _adapter.Cancel -= OnAdapterSelectionComplete;
                    _adapter.ItemsSource = null;
                }

                _adapter = value;

                if (_adapter != null)
                {
                    _adapter.SelectionChanged += OnAdapterSelectionChanged;
                    _adapter.Commit += OnAdapterSelectionComplete;
                    _adapter.Cancel += OnAdapterSelectionCanceled;
                    _adapter.Cancel += OnAdapterSelectionComplete;
                    _adapter.ItemsSource = _view;
                }
            }
        }

        /// <summary>
        /// Returns the
        /// <see cref="T:Avalonia.Controls.ISelectionAdapter" /> part, if
        /// possible.
        /// </summary>
        /// <returns>
        /// A <see cref="T:Avalonia.Controls.ISelectionAdapter" /> object,
        /// if possible. Otherwise, null.
        /// </returns>
        protected virtual ISelectionAdapter? GetSelectionAdapterPart(INameScope nameScope)
        {
            ISelectionAdapter? adapter = null;
            SelectingItemsControl? selector = nameScope.Find<SelectingItemsControl>(ElementSelector);
            if (selector != null)
            {
                // Check if it is already an IItemsSelector
                adapter = selector as ISelectionAdapter;
                if (adapter == null)
                {
                    // Built in support for wrapping a Selector control
                    adapter = new SelectingItemsControlSelectionAdapter(selector);
                }
            }
            if (adapter == null)
            {
                adapter = nameScope.Find<ISelectionAdapter>(ElementSelectionAdapter);
            }
            return adapter;
        }

        /// <summary>
        /// Builds the visual tree for the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control
        /// when a new template is applied.
        /// </summary>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {

            if (DropDownPopup != null)
            {
                DropDownPopup.Closed -= DropDownPopup_Closed;
                DropDownPopup = null;
            }

            // Set the template parts. Individual part setters remove and add
            // any event handlers.
            Popup? popup = e.NameScope.Find<Popup>(ElementPopup);
            if (popup != null)
            {
                DropDownPopup = popup;
                DropDownPopup.Closed += DropDownPopup_Closed;
            }

            SelectionAdapter = GetSelectionAdapterPart(e.NameScope);
            TextBox = e.NameScope.Find<TextBox>(ElementTextBox);

            // If the drop down property indicates that the popup is open,
            // flip its value to invoke the changed handler.
            if (IsDropDownOpen && DropDownPopup != null && !DropDownPopup.IsOpen)
            {
                OpeningDropDown(false);
            }

            base.OnApplyTemplate(e);
        }

        /// <summary>
        /// Called to update the validation state for properties for which data validation is
        /// enabled.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="state">The current data binding state.</param>
        /// <param name="error">The current data binding error, if any.</param>
        protected override void UpdateDataValidation(
            AvaloniaProperty property,
            BindingValueType state,
            Exception? error)
        {
            if (property == TextProperty || property == SelectedItemProperty)
            {
                DataValidationErrors.SetError(this, error);
            }
        }

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Avalonia.InputElement.KeyDown" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.Input.KeyEventArgs" />
        /// that contains the event data.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            _ = e ?? throw new ArgumentNullException(nameof(e));

            base.OnKeyDown(e);

            if (e.Handled || !IsEnabled)
            {
                return;
            }

            // The drop down is open, pass along the key event arguments to the
            // selection adapter. If it isn't handled by the adapter's logic,
            // then we handle some simple navigation scenarios for controlling
            // the drop down.
            if (IsDropDownOpen)
            {
                if (SelectionAdapter != null)
                {
                    SelectionAdapter.HandleKeyDown(e);
                    if (e.Handled)
                    {
                        return;
                    }
                }

                if (e.Key == Key.Escape)
                {
                    OnAdapterSelectionCanceled(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            else
            {
                // The drop down is not open, the Down key will toggle it open.
                // Ignore key buttons, if they are used for XY focus.
                if (e.Key == Key.Down
                    && !XYFocusHelpers.IsAllowedXYNavigationMode(this, e.KeyDeviceType))
                {
                    SetCurrentValue(IsDropDownOpenProperty, true);
                    e.Handled = true;
                }
            }

            // Standard drop down navigation
            switch (e.Key)
            {
                case Key.F4:
                    SetCurrentValue(IsDropDownOpenProperty, !IsDropDownOpen);
                    e.Handled = true;
                    break;

                case Key.Enter:
                    if (IsDropDownOpen)
                    {
                        OnAdapterSelectionComplete(this, new RoutedEventArgs());
                        e.Handled = true;
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Avalonia.UIElement.GotFocus" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.RoutedEventArgs" />
        /// that contains the event data.</param>
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            FocusChanged(HasFocus());
        }

        /// <summary>
        /// Provides handling for the
        /// <see cref="E:Avalonia.UIElement.LostFocus" /> event.
        /// </summary>
        /// <param name="e">A <see cref="T:Avalonia.RoutedEventArgs" />
        /// that contains the event data.</param>
        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            FocusChanged(HasFocus());
        }

        /// <summary>
        /// Determines whether the text box or drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> control has
        /// focus.
        /// </summary>
        /// <returns>true to indicate the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> has focus;
        /// otherwise, false.</returns>
        protected bool HasFocus() => IsKeyboardFocusWithin;

        /// <summary>
        /// Handles the FocusChanged event.
        /// </summary>
        /// <param name="hasFocus">A value indicating whether the control
        /// currently has the focus.</param>
        private void FocusChanged(bool hasFocus)
        {
            // The OnGotFocus & OnLostFocus are asynchronously and cannot
            // reliably tell you that have the focus.  All they do is let you
            // know that the focus changed sometime in the past.  To determine
            // if you currently have the focus you need to do consult the
            // FocusManager (see HasFocus()).

            bool wasFocused = _isFocused;
            _isFocused = hasFocus;

            if (hasFocus)
            {

                if (!wasFocused && TextBox != null && TextBoxSelectionLength <= 0)
                {
                    TextBox.Focus();
                    TextBox.SelectAll();
                }
            }
            else
            {
                // Check if we still have focus in the parent's focus scope
                if (GetFocusScope() is { } scope &&
                    (FocusManager.GetFocusManager(this)?.GetFocusedElement(scope) is not { } focused ||
                    (focused != this &&
                    (focused is Visual v && !this.IsVisualAncestorOf(v)))))
                {
                    SetCurrentValue(IsDropDownOpenProperty, false);
                }

                _userCalledPopulate = false;
                ClearTextBoxSelection();
            }

            _isFocused = hasFocus;

            IFocusScope? GetFocusScope()
            {
                IInputElement? c = this;

                while (c != null)
                {
                    if (c is IFocusScope scope &&
                        c is Visual v &&
                        v.VisualRoot is Visual root &&
                        root.IsVisible)
                    {
                        return scope;
                    }

                    c = (c as Visual)?.GetVisualParent<IInputElement>() ??
                        ((c as IHostedVisualTreeRoot)?.Host as IInputElement);
                }

                return null;
            }
        }

        /// <summary>
        /// Occurs asynchronously when the text in the <see cref="TextBox"/> portion of the
        /// <see cref="AutoCompleteBox" /> changes.
        /// </summary>
        public event EventHandler<TextChangedEventArgs>? TextChanged
        {
            add => AddHandler(TextChangedEvent, value);
            remove => RemoveHandler(TextChangedEvent, value);
        }

        /// <summary>
        /// Occurs when the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> is
        /// populating the drop-down with possible matches based on the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// property.
        /// </summary>
        /// <remarks>
        /// If the event is canceled, by setting the PopulatingEventArgs.Cancel
        /// property to true, the AutoCompleteBox will not automatically
        /// populate the selection adapter contained in the drop-down.
        /// In this case, if you want possible matches to appear, you must
        /// provide the logic for populating the selection adapter.
        /// </remarks>
        public event EventHandler<PopulatingEventArgs>? Populating;

        /// <summary>
        /// Occurs when the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> has
        /// populated the drop-down with possible matches based on the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Text" />
        /// property.
        /// </summary>
        public event EventHandler<PopulatedEventArgs>? Populated;

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property is changing from false to true.
        /// </summary>
        public event EventHandler<CancelEventArgs>? DropDownOpening;

        /// <summary>
        /// Occurs when the value of the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property has changed from false to true and the drop-down is open.
        /// </summary>
        public event EventHandler? DropDownOpened;

        /// <summary>
        /// Occurs when the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property is changing from true to false.
        /// </summary>
        public event EventHandler<CancelEventArgs>? DropDownClosing;

        /// <summary>
        /// Occurs when the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.IsDropDownOpen" />
        /// property was changed from true to false and the drop-down is open.
        /// </summary>
        public event EventHandler? DropDownClosed;

        /// <summary>
        /// Occurs when the selected item in the drop-down portion of the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> has
        /// changed.
        /// </summary>
        public event EventHandler<SelectionChangedEventArgs> SelectionChanged
        {
            add => AddHandler(SelectionChangedEvent, value);
            remove => RemoveHandler(SelectionChangedEvent, value);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.Populating" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.PopulatingEventArgs" /> that
        /// contains the event data.</param>
        protected virtual void OnPopulating(PopulatingEventArgs e)
        {
            Populating?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.Populated" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.PopulatedEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnPopulated(PopulatedEventArgs e)
        {
            Populated?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.SelectionChanged" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.SelectionChangedEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownOpening" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.CancelEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnDropDownOpening(CancelEventArgs e)
        {
            DropDownOpening?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownOpened" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:System.EventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnDropDownOpened(EventArgs e)
        {
            DropDownOpened?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownClosing" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:Avalonia.Controls.CancelEventArgs" />
        /// that contains the event data.</param>
        protected virtual void OnDropDownClosing(CancelEventArgs e)
        {
            DropDownClosing?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the
        /// <see cref="E:Avalonia.Controls.AutoCompleteBox.DropDownClosed" />
        /// event.
        /// </summary>
        /// <param name="e">A
        /// <see cref="T:System.EventArgs" />
        /// which contains the event data.</param>
        protected virtual void OnDropDownClosed(EventArgs e)
        {
            DropDownClosed?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the <see cref="TextChanged" /> event.
        /// </summary>
        /// <param name="e">A <see cref="TextChangedEventArgs" /> that contains the event data.</param>
        protected virtual void OnTextChanged(TextChangedEventArgs e)
        {
            RaiseEvent(e);
        }

        /// <summary>
        /// Begin closing the drop-down.
        /// </summary>
        /// <param name="oldValue">The original value.</param>
        private void ClosingDropDown(bool oldValue)
        {
            var args = new CancelEventArgs();
            OnDropDownClosing(args);

            if (args.Cancel)
            {
                _ignorePropertyChange = true;
                SetCurrentValue(IsDropDownOpenProperty, oldValue);
            }
            else
            {
                CloseDropDown();
            }

            UpdatePseudoClasses();
        }

        /// <summary>
        /// Begin opening the drop down by firing cancelable events, opening the
        /// drop-down or reverting, depending on the event argument values.
        /// </summary>
        /// <param name="oldValue">The original value, if needed for a revert.</param>
        private void OpeningDropDown(bool oldValue)
        {
            var args = new CancelEventArgs();

            // Opening
            OnDropDownOpening(args);

            if (args.Cancel)
            {
                _ignorePropertyChange = true;
                SetCurrentValue(IsDropDownOpenProperty, oldValue);
            }
            else
            {
                OpenDropDown();
            }

            UpdatePseudoClasses();
        }

        /// <summary>
        /// Connects to the DropDownPopup Closed event.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event data.</param>
        private void DropDownPopup_Closed(object? sender, EventArgs e)
        {
            // Force the drop down dependency property to be false.
            if (IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }

            // Fire the DropDownClosed event
            if (_popupHasOpened)
            {
                OnDropDownClosed(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles the timer tick when using a populate delay.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event arguments.</param>
        private void PopulateDropDown(object? sender, EventArgs e)
        {
            _delayTimer?.Stop();

            // Update the prefix/search text.
            SearchText = Text;

            if (TryPopulateAsync(SearchText))
            {
                return;
            }

            // The Populated event enables advanced, custom filtering. The
            // client needs to directly update the ItemsSource collection or
            // call the Populate method on the control to continue the
            // display process if Cancel is set to true.
            PopulatingEventArgs populating = new PopulatingEventArgs(SearchText);
            OnPopulating(populating);
            if (!populating.Cancel)
            {
                PopulateComplete();
            }
        }
        private bool TryPopulateAsync(string? searchText)
        {
            _populationCancellationTokenSource?.Cancel(false);
            _populationCancellationTokenSource?.Dispose();
            _populationCancellationTokenSource = null;

            if (AsyncPopulator == null)
            {
                return false;
            }

            _populationCancellationTokenSource = new CancellationTokenSource();
            var task = PopulateAsync(searchText, _populationCancellationTokenSource.Token);
            if (task.Status == TaskStatus.Created)
                task.Start();

            return true;
        }
        private async Task PopulateAsync(string? searchText, CancellationToken cancellationToken)
        {

            try
            {
                IEnumerable<object> result = await AsyncPopulator!.Invoke(searchText, cancellationToken);
                var resultList = result.ToList();

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        SetCurrentValue(ItemsSourceProperty, resultList);
                        PopulateComplete();
                    }
                });
            }
            catch (TaskCanceledException)
            { }
            finally
            {
                _populationCancellationTokenSource?.Dispose();
                _populationCancellationTokenSource = null;
            }

        }

        /// <summary>
        /// Private method that directly opens the popup, checks the expander
        /// button, and then fires the Opened event.
        /// </summary>
        private void OpenDropDown()
        {
            if (DropDownPopup != null)
            {
                DropDownPopup.IsOpen = true;
            }
            _popupHasOpened = true;
            OnDropDownOpened(EventArgs.Empty);
        }

        /// <summary>
        /// Private method that directly closes the popup, flips the Checked
        /// value, and then fires the Closed event.
        /// </summary>
        private void CloseDropDown()
        {
            if (_popupHasOpened)
            {
                if (SelectionAdapter != null)
                {
                    SelectionAdapter.SelectedItem = null;
                }
                if (DropDownPopup != null)
                {
                    DropDownPopup.IsOpen = false;
                }
                OnDropDownClosed(EventArgs.Empty);
            }
        }

        /// <summary>
        /// Formats an Item for text comparisons based on Converter
        /// and ConverterCulture properties.
        /// </summary>
        /// <param name="value">The object to format.</param>
        /// <param name="clearDataContext">A value indicating whether to clear
        /// the data context after the lookup is performed.</param>
        /// <returns>Formatted Value.</returns>
        private string? FormatValue(object? value, bool clearDataContext)
        {
            string? result = FormatValue(value);
            if (clearDataContext && _valueBindingEvaluator != null)
            {
                _valueBindingEvaluator.ClearDataContext();
            }

            return result;
        }

        /// <summary>
        /// Converts the specified object to a string by using the
        /// <see cref="P:Avalonia.Data.Binding.Converter" /> and
        /// <see cref="P:Avalonia.Data.Binding.ConverterCulture" /> values
        /// of the binding object specified by the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.ValueMemberBinding" />
        /// property.
        /// </summary>
        /// <param name="value">The object to format as a string.</param>
        /// <returns>The string representation of the specified object.</returns>
        /// <remarks>
        /// Override this method to provide a custom string conversion.
        /// </remarks>
        protected virtual string? FormatValue(object? value)
        {
            if (_valueBindingEvaluator != null)
            {
                return _valueBindingEvaluator.GetDynamicValue(value) ?? String.Empty;
            }

            return value == null ? String.Empty : value.ToString();
        }

        /// <summary>
        /// Handle the TextChanged event that is directly attached to the
        /// TextBox part. This ensures that only user initiated actions will
        /// result in an AutoCompleteBox suggestion and operation.
        /// </summary>
        private void OnTextBoxTextChanged()
        {
            //Uses Dispatcher.Post to allow the TextBox selection to update before processing
            Dispatcher.UIThread.Post(() =>
            {
                // Call the central updated text method as a user-initiated action
                TextUpdated(_textBox!.Text, true);
            });
        }

        /// <summary>
        /// Updates both the text box value and underlying text dependency
        /// property value if and when they change. Automatically fires the
        /// text changed events when there is a change.
        /// </summary>
        /// <param name="value">The new string value.</param>
        private void UpdateTextValue(string? value)
        {
            UpdateTextValue(value, null);
        }

        /// <summary>
        /// Updates both the text box value and underlying text dependency
        /// property value if and when they change. Automatically fires the
        /// text changed events when there is a change.
        /// </summary>
        /// <param name="value">The new string value.</param>
        /// <param name="userInitiated">A nullable bool value indicating whether
        /// the action was user initiated. In a user initiated mode, the
        /// underlying text dependency property is updated. In a non-user
        /// interaction, the text box value is updated. When user initiated is
        /// null, all values are updated.</param>
        private void UpdateTextValue(string? value, bool? userInitiated)
        {
            bool callTextChanged = false;
            // Update the Text dependency property
            if ((userInitiated ?? true) && Text != value)
            {
                _ignoreTextPropertyChange++;
                SetCurrentValue(TextProperty, value);
                callTextChanged = true;
            }

            // Update the TextBox's Text dependency property
            if ((userInitiated == null || userInitiated == false) && TextBox != null && TextBox.Text != value)
            {
                _ignoreTextPropertyChange++;
                TextBox.Text = value ?? string.Empty;

                // Text dependency property value was set, fire event
                if (!callTextChanged && (Text == value || Text == null))
                {
                    callTextChanged = true;
                }
            }

            if (callTextChanged)
            {
                OnTextChanged(new TextChangedEventArgs(TextChangedEvent));
            }
        }

        /// <summary>
        /// Handle the update of the text for the control from any source,
        /// including the TextBox part and the Text dependency property.
        /// </summary>
        /// <param name="newText">The new text.</param>
        /// <param name="userInitiated">A value indicating whether the update
        /// is a user-initiated action. This should be a True value when the
        /// TextUpdated method is called from a TextBox event handler.</param>
        private void TextUpdated(string? newText, bool userInitiated)
        {
            // Only process this event if it is coming from someone outside
            // setting the Text dependency property directly.
            if (_ignoreTextPropertyChange > 0)
            {
                _ignoreTextPropertyChange--;
                return;
            }

            if (newText == null)
            {
                newText = string.Empty;
            }

            // The TextBox.TextChanged event was not firing immediately and
            // was causing an immediate update, even with wrapping. If there is
            // a selection currently, no update should happen.
            if (IsTextCompletionEnabled && TextBox != null && TextBoxSelectionLength > 0 && TextBoxSelectionStart != (TextBox.Text?.Length ?? 0))
            {
                return;
            }

            // Evaluate the conditions needed for completion.
            // 1. Minimum prefix length
            // 2. If a delay timer is in use, use it
            bool minimumLengthReached = newText.Length >= MinimumPrefixLength && MinimumPrefixLength >= 0;

            _userCalledPopulate = minimumLengthReached && userInitiated;

            // Update the interface and values only as necessary
            UpdateTextValue(newText, userInitiated);

            if (minimumLengthReached)
            {
                _ignoreTextSelectionChange = true;

                if (_delayTimer != null)
                {
                    _delayTimer.Start();
                }
                else
                {
                    PopulateDropDown(this, EventArgs.Empty);
                }
            }
            else
            {
                SearchText = string.Empty;
                if (SelectedItem != null)
                {
                    _skipSelectedItemTextUpdate = true;
                }

                SetCurrentValue(SelectedItemProperty, null);

                if (IsDropDownOpen)
                {
                    SetCurrentValue(IsDropDownOpenProperty, false);
                }
            }
        }

        /// <summary>
        /// A simple helper method to clear the view and ensure that a view
        /// object is always present and not null.
        /// </summary>
        private void ClearView()
        {
            if (_view == null)
            {
                _view = new AvaloniaList<object>();
            }
            else
            {
                _view.Clear();
            }
        }

        /// <summary>
        /// Walks through the items enumeration. Performance is not going to be
        /// perfect with the current implementation.
        /// </summary>
        private void RefreshView()
        {
            // If we have a running filter, trigger a request first
            if (_filterInAction)
            {
                _cancelRequested = true;
            }

            // Indicate that filtering is ongoing
            _filterInAction = true;

            if (_items == null)
            {
                ClearView();
                return;
            }

            // Cache the current text value
            string text = Text ?? string.Empty;

            // Determine if any filtering mode is on
            bool stringFiltering = TextFilter != null;
            bool objectFiltering = FilterMode == AutoCompleteFilterMode.Custom && TextFilter == null;
            
            List<object> items = _items;

            // cache properties
            var textFilter = TextFilter;
            var itemFilter = ItemFilter;
            var _newViewItems = new Collection<object>();
            
            // if the mode is objectFiltering and itemFilter is null, we throw an exception
            if (objectFiltering && itemFilter is null)
            {
                // indicate that filtering is not ongoing anymore
                _filterInAction = false;
                _cancelRequested = false;
                
                throw new Exception(
                    "ItemFilter property can not be null when FilterMode has value AutoCompleteFilterMode.Custom");
            }

            foreach (object item in items)
            {
                // Exit the fitter when requested if cancellation is requested
                if (_cancelRequested)
                {
                    return;
                }

                bool inResults = !(stringFiltering || objectFiltering);

                if (!inResults)
                {
                    if (stringFiltering)
                    {
                        inResults = textFilter!(text, FormatValue(item));
                    }
                    else if (objectFiltering)
                    {
                        inResults = itemFilter!(text, item);
                    }
                }

                if (inResults)
                {
                    _newViewItems.Add(item);
                }
            }

            _view?.Clear();
            _view?.AddRange(_newViewItems);

            // Clear the evaluator to discard a reference to the last item
            _valueBindingEvaluator?.ClearDataContext();

            // indicate that filtering is not ongoing anymore
            _filterInAction = false;
            _cancelRequested = false;
        }

        /// <summary>
        /// Handle any change to the ItemsSource dependency property, update
        /// the underlying ObservableCollection view, and set the selection
        /// adapter's ItemsSource to the view if appropriate.
        /// </summary>
        /// <param name="newValue">The new enumerable reference.</param>
        private void OnItemsSourceChanged(IEnumerable? newValue)
        {
            // Remove handler for oldValue.CollectionChanged (if present)
            _collectionChangeSubscription?.Dispose();
            _collectionChangeSubscription = null;

            // Add handler for newValue.CollectionChanged (if possible)
            if (newValue is INotifyCollectionChanged newValueINotifyCollectionChanged)
            {
                _collectionChangeSubscription = newValueINotifyCollectionChanged.WeakSubscribe(ItemsCollectionChanged);
            }

            // Store a local cached copy of the data
            _items = newValue == null ? null : new List<object>(newValue.Cast<object>());

            // Clear and set the view on the selection adapter
            ClearView();
            if (SelectionAdapter != null && SelectionAdapter.ItemsSource != _view)
            {
                SelectionAdapter.ItemsSource = _view;
            }
            if (IsDropDownOpen)
            {
                RefreshView();
            }
        }

        /// <summary>
        /// Method that handles the ObservableCollection.CollectionChanged event for the ItemsSource property.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void ItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Update the cache
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                for (int index = 0; index < e.OldItems.Count; index++)
                {
                    _items!.RemoveAt(e.OldStartingIndex);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && _items!.Count >= e.NewStartingIndex)
            {
                for (int index = 0; index < e.NewItems.Count; index++)
                {
                    _items.Insert(e.NewStartingIndex + index, e.NewItems[index]!);
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems != null && e.OldItems != null)
            {
                for (int index = 0; index < e.NewItems.Count; index++)
                {
                    _items![e.NewStartingIndex] = e.NewItems[index]!;
                }
            }

            // Update the view
            if ((e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace) && e.OldItems != null)
            {
                for (int index = 0; index < e.OldItems.Count; index++)
                {
                    _view!.Remove(e.OldItems[index]!);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Significant changes to the underlying data.
                ClearView();
                if (ItemsSource != null)
                {
                    _items = new List<object>(ItemsSource.Cast<object>());
                }
            }

            // Refresh the observable collection used in the selection adapter.
            RefreshView();
        }

        /// <summary>
        /// Notifies the
        /// <see cref="T:Avalonia.Controls.AutoCompleteBox" /> that the
        /// <see cref="P:Avalonia.Controls.AutoCompleteBox.Items" />
        /// property has been set and the data can be filtered to provide
        /// possible matches in the drop-down.
        /// </summary>
        /// <remarks>
        /// Call this method when you are providing custom population of
        /// the drop-down portion of the AutoCompleteBox, to signal the control
        /// that you are done with the population process.
        /// Typically, you use PopulateComplete when the population process
        /// is a long-running process and you want to cancel built-in filtering
        ///  of the ItemsSource items. In this case, you can handle the
        /// Populated event and set PopulatingEventArgs.Cancel to true.
        /// When the long-running process has completed you call
        /// PopulateComplete to indicate the drop-down is populated.
        /// </remarks>
        public void PopulateComplete()
        {
            // Apply the search filter
            RefreshView();

            // Fire the Populated event containing the read-only view data.
            PopulatedEventArgs populated = new PopulatedEventArgs(new ReadOnlyCollection<object>(_view!));
            OnPopulated(populated);

            if (SelectionAdapter != null && SelectionAdapter.ItemsSource != _view)
            {
                SelectionAdapter.ItemsSource = _view;
            }

            bool isDropDownOpen = _userCalledPopulate && (_view!.Count > 0);
            if (isDropDownOpen != IsDropDownOpen)
            {
                _ignorePropertyChange = true;
                SetCurrentValue(IsDropDownOpenProperty, isDropDownOpen);
            }
            if (IsDropDownOpen)
            {
                OpeningDropDown(false);
            }
            else
            {
                ClosingDropDown(true);
            }

            UpdateTextCompletion(_userCalledPopulate);
        }

        /// <summary>
        /// Performs text completion, if enabled, and a lookup on the underlying
        /// item values for an exact match. Will update the SelectedItem value.
        /// </summary>
        /// <param name="userInitiated">A value indicating whether the operation
        /// was user initiated. Text completion will not be performed when not
        /// directly initiated by the user.</param>
        private void UpdateTextCompletion(bool userInitiated)
        {
            // By default this method will clear the selected value
            object? newSelectedItem = null;
            string? text = Text;

            // Text search is StartsWith explicit and only when enabled, in
            // line with WPF's ComboBox lookup. When in use it will associate
            // a Value with the Text if it is found in ItemsSource. This is
            // only valid when there is data and the user initiated the action.
            if (_view!.Count > 0)
            {
                if (IsTextCompletionEnabled && TextBox != null && userInitiated)
                {
                    int currentLength = TextBox.Text?.Length ?? 0;
                    int selectionStart = TextBoxSelectionStart;
                    if (selectionStart == text?.Length && selectionStart > _textSelectionStart)
                    {
                        // When the FilterMode dependency property is set to
                        // either StartsWith or StartsWithCaseSensitive, the
                        // first item in the view is used. This will improve
                        // performance on the lookup. It assumes that the
                        // FilterMode the user has selected is an acceptable
                        // case sensitive matching function for their scenario.
                        object? top = FilterMode == AutoCompleteFilterMode.StartsWith || FilterMode == AutoCompleteFilterMode.StartsWithCaseSensitive
                            ? _view[0]
                            : TryGetMatch(text, _view, AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.StartsWith));

                        // If the search was successful, update SelectedItem
                        if (top != null)
                        {
                            newSelectedItem = top;
                            string? topString = FormatValue(top, true);

                            // Only replace partially when the two words being the same
                            int minLength = Math.Min(topString?.Length ?? 0, Text?.Length ?? 0);
                            if (AutoCompleteSearch.Equals(Text?.Substring(0, minLength), topString?.Substring(0, minLength)))
                            {
                                // Update the text
                                UpdateTextValue(topString);

                                // Select the text past the user's caret
                                TextBox.SelectionStart = currentLength;
                                TextBox.SelectionEnd = topString?.Length ?? 0;
                            }
                        }
                    }
                }
                else
                {
                    // Perform an exact string lookup for the text. This is a
                    // design change from the original Toolkit release when the
                    // IsTextCompletionEnabled property behaved just like the
                    // WPF ComboBox's IsTextSearchEnabled property.
                    //
                    // This change provides the behavior that most people expect
                    // to find: a lookup for the value is always performed.
                    newSelectedItem = TryGetMatch(text, _view, AutoCompleteSearch.GetFilter(AutoCompleteFilterMode.EqualsCaseSensitive));
                }
            }

            // Update the selected item property

            if (SelectedItem != newSelectedItem)
            {
                _skipSelectedItemTextUpdate = true;
            }
            SetCurrentValue(SelectedItemProperty, newSelectedItem);

            // Restore updates for TextSelection
            if (_ignoreTextSelectionChange)
            {
                _ignoreTextSelectionChange = false;
                if (TextBox != null)
                {
                    _textSelectionStart = TextBoxSelectionStart;
                }
            }
        }

        /// <summary>
        /// Attempts to look through the view and locate the specific exact
        /// text match.
        /// </summary>
        /// <param name="searchText">The search text.</param>
        /// <param name="view">The view reference.</param>
        /// <param name="predicate">The predicate to use for the partial or
        /// exact match.</param>
        /// <returns>Returns the object or null.</returns>
        private object? TryGetMatch(string? searchText, AvaloniaList<object>? view, AutoCompleteFilterPredicate<string?>? predicate)
        {
            if (predicate is null)
                return null;

            if (view != null && view.Count > 0)
            {
                foreach (object o in view)
                {
                    if (predicate(searchText, FormatValue(o)))
                    {
                        return o;
                    }
                }
            }

            return null;
        }

        private void UpdatePseudoClasses()
        {
            PseudoClasses.Set(":dropdownopen", IsDropDownOpen);
        }

        private void ClearTextBoxSelection()
        {
            if (TextBox != null)
            {
                int length = TextBox.Text?.Length ?? 0;
                TextBox.SelectionStart = length;
                TextBox.SelectionEnd = length;
            }
        }

        /// <summary>
        /// Called when the selected item is changed, updates the text value
        /// that is displayed in the text box part.
        /// </summary>
        /// <param name="newItem">The new item.</param>
        private void OnSelectedItemChanged(object? newItem)
        {
            string? text;

            if (newItem == null)
            {
                text = SearchText;
            }
            else if (TextSelector != null)
            {
                text = TextSelector(SearchText, FormatValue(newItem, true));
            }
            else if (ItemSelector != null)
            {
                text = ItemSelector(SearchText, newItem);
            }
            else
            {
                text = FormatValue(newItem, true);
            }

            // Update the Text property and the TextBox values
            UpdateTextValue(text);

            // Move the caret to the end of the text box
            ClearTextBoxSelection();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the selection adapter.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The selection changed event data.</param>
        private void OnAdapterSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            SetCurrentValue(SelectedItemProperty, _adapter!.SelectedItem);
        }

        //TODO Check UpdateTextCompletion
        /// <summary>
        /// Handles the Commit event on the selection adapter.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event data.</param>
        private void OnAdapterSelectionComplete(object? sender, RoutedEventArgs e)
        {
            SetCurrentValue(IsDropDownOpenProperty, false);

            // Completion will update the selected value
            //UpdateTextCompletion(false);

            // Text should not be selected
            ClearTextBoxSelection();

            TextBox!.Focus();
        }

        /// <summary>
        /// Handles the Cancel event on the selection adapter.
        /// </summary>
        /// <param name="sender">The source object.</param>
        /// <param name="e">The event data.</param>
        private void OnAdapterSelectionCanceled(object? sender, RoutedEventArgs e)
        {
            UpdateTextValue(SearchText);

            // Completion will update the selected value
            UpdateTextCompletion(false);
        }

        /// <summary>
        /// A predefined set of filter functions for the known, built-in
        /// AutoCompleteFilterMode enumeration values.
        /// </summary>
        private static class AutoCompleteSearch
        {
            /// <summary>
            /// Index function that retrieves the filter for the provided
            /// AutoCompleteFilterMode.
            /// </summary>
            /// <param name="FilterMode">The built-in search mode.</param>
            /// <returns>Returns the string-based comparison function.</returns>
            public static AutoCompleteFilterPredicate<string?>? GetFilter(AutoCompleteFilterMode FilterMode)
            {
                switch (FilterMode)
                {
                    case AutoCompleteFilterMode.Contains:
                        return Contains;

                    case AutoCompleteFilterMode.ContainsCaseSensitive:
                        return ContainsCaseSensitive;

                    case AutoCompleteFilterMode.ContainsOrdinal:
                        return ContainsOrdinal;

                    case AutoCompleteFilterMode.ContainsOrdinalCaseSensitive:
                        return ContainsOrdinalCaseSensitive;

                    case AutoCompleteFilterMode.Equals:
                        return Equals;

                    case AutoCompleteFilterMode.EqualsCaseSensitive:
                        return EqualsCaseSensitive;

                    case AutoCompleteFilterMode.EqualsOrdinal:
                        return EqualsOrdinal;

                    case AutoCompleteFilterMode.EqualsOrdinalCaseSensitive:
                        return EqualsOrdinalCaseSensitive;

                    case AutoCompleteFilterMode.StartsWith:
                        return StartsWith;

                    case AutoCompleteFilterMode.StartsWithCaseSensitive:
                        return StartsWithCaseSensitive;

                    case AutoCompleteFilterMode.StartsWithOrdinal:
                        return StartsWithOrdinal;

                    case AutoCompleteFilterMode.StartsWithOrdinalCaseSensitive:
                        return StartsWithOrdinalCaseSensitive;

                    case AutoCompleteFilterMode.None:
                    case AutoCompleteFilterMode.Custom:
                    default:
                        return null;
                }
            }

            /// <summary>
            /// An implementation of the Contains member of string that takes in a
            /// string comparison. The traditional .NET string Contains member uses
            /// StringComparison.Ordinal.
            /// </summary>
            /// <param name="s">The string.</param>
            /// <param name="value">The string value to search for.</param>
            /// <param name="comparison">The string comparison type.</param>
            /// <returns>Returns true when the substring is found.</returns>
            private static bool Contains(string? s, string? value, StringComparison comparison)
            {
                if (s is not null && value is not null)
                    return s.IndexOf(value, comparison) >= 0;
                return false;
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWith(string? text, string? value)
            {
                if (value is not null && text is not null)
                    return value.StartsWith(text, StringComparison.CurrentCultureIgnoreCase);
                return false;
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWithCaseSensitive(string? text, string? value)
            {
                if (value is not null && text is not null)
                    return value.StartsWith(text, StringComparison.CurrentCulture);
                return false;
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWithOrdinal(string? text, string? value)
            {
                if (value is not null && text is not null)
                    return value.StartsWith(text, StringComparison.OrdinalIgnoreCase);
                return false;
            }

            /// <summary>
            /// Check if the string value begins with the text.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool StartsWithOrdinalCaseSensitive(string? text, string? value)
            {
                if (value is not null && text is not null)
                    return value.StartsWith(text, StringComparison.Ordinal);
                return false;
            }

            /// <summary>
            /// Check if the prefix is contained in the string value. The current
            /// culture's case insensitive string comparison operator is used.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool Contains(string? text, string? value)
            {
                return Contains(value, text, StringComparison.CurrentCultureIgnoreCase);
            }

            /// <summary>
            /// Check if the prefix is contained in the string value.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool ContainsCaseSensitive(string? text, string? value)
            {
                return Contains(value, text, StringComparison.CurrentCulture);
            }

            /// <summary>
            /// Check if the prefix is contained in the string value.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool ContainsOrdinal(string? text, string? value)
            {
                return Contains(value, text, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Check if the prefix is contained in the string value.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool ContainsOrdinalCaseSensitive(string? text, string? value)
            {
                return Contains(value, text, StringComparison.Ordinal);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool Equals(string? text, string? value)
            {
                return string.Equals(value, text, StringComparison.CurrentCultureIgnoreCase);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool EqualsCaseSensitive(string? text, string? value)
            {
                return string.Equals(value, text, StringComparison.CurrentCulture);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool EqualsOrdinal(string? text, string? value)
            {
                return string.Equals(value, text, StringComparison.OrdinalIgnoreCase);
            }

            /// <summary>
            /// Check if the string values are equal.
            /// </summary>
            /// <param name="text">The AutoCompleteBox prefix text.</param>
            /// <param name="value">The item's string value.</param>
            /// <returns>Returns true if the condition is met.</returns>
            public static bool EqualsOrdinalCaseSensitive(string? text, string? value)
            {
                return string.Equals(value, text, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// A framework element that permits a binding to be evaluated in a new data
        /// context leaf node.
        /// </summary>
        /// <typeparam name="T">The type of dynamic binding to return.</typeparam>
        public class BindingEvaluator<T> : Control
        {
            /// <summary>
            /// Gets or sets the string value binding used by the control.
            /// </summary>
            private IBinding? _binding;

            /// <summary>
            /// Identifies the Value dependency property.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("AvaloniaProperty", "AVP1002:AvaloniaProperty objects should not be owned by a generic type",
                Justification = "This property is not supposed to be used from XAML.")]
            public static readonly StyledProperty<T> ValueProperty =
                AvaloniaProperty.Register<BindingEvaluator<T>, T>(nameof(Value));

            /// <summary>
            /// Gets or sets the data item value.
            /// </summary>
            public T Value
            {
                get => GetValue(ValueProperty);
                set => SetValue(ValueProperty, value);
            }

            /// <summary>
            /// Gets or sets the value binding.
            /// </summary>
            public IBinding? ValueBinding
            {
                get => _binding;
                set
                {
                    _binding = value;
                    if (value is not null)
                        Bind(ValueProperty, value);
                }
            }

            /// <summary>
            /// Initializes a new instance of the BindingEvaluator class.
            /// </summary>
            public BindingEvaluator()
            { }

            /// <summary>
            /// Initializes a new instance of the BindingEvaluator class,
            /// setting the initial binding to the provided parameter.
            /// </summary>
            /// <param name="binding">The initial string value binding.</param>
            public BindingEvaluator(IBinding? binding)
                : this()
            {
                ValueBinding = binding;
            }

            /// <summary>
            /// Clears the data context so that the control does not keep a
            /// reference to the last-looked up item.
            /// </summary>
            public void ClearDataContext()
            {
                DataContext = null;
            }

            /// <summary>
            /// Updates the data context of the framework element and returns the
            /// updated binding value.
            /// </summary>
            /// <param name="o">The object to use as the data context.</param>
            /// <param name="clearDataContext">If set to true, this parameter will
            /// clear the data context immediately after retrieving the value.</param>
            /// <returns>Returns the evaluated T value of the bound dependency
            /// property.</returns>
            public T GetDynamicValue(object o, bool clearDataContext)
            {
                DataContext = o;
                T value = Value;
                if (clearDataContext)
                {
                    DataContext = null;
                }
                return value;
            }

            /// <summary>
            /// Updates the data context of the framework element and returns the
            /// updated binding value.
            /// </summary>
            /// <param name="o">The object to use as the data context.</param>
            /// <returns>Returns the evaluated T value of the bound dependency
            /// property.</returns>
            public T GetDynamicValue(object? o)
            {
                DataContext = o;
                return Value;
            }
        }
    }
}
