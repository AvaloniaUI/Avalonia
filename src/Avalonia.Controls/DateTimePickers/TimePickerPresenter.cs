using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Avalonia.Controls
{
    //Combines TimePickerFlyout & TimePickerFlyoutPicker from WinUI

    /// <summary>
    /// Defines the presenter used for selecting a time. Intended for use with
    /// <see cref="TimePicker"/> but can be used independently
    /// </summary>
    public class TimePickerPresenter : PickerPresenterBase
    {
        public TimePickerPresenter()
        {
            Time = DateTime.Now.TimeOfDay;

            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
        }

        /// <summary>
        /// Defines the <see cref="MinuteIncrement"/> Property
        /// </summary>
        public static readonly DirectProperty<TimePickerPresenter, int> MinuteIncrementProperty =
            AvaloniaProperty.RegisterDirect<TimePickerPresenter, int>("MinuteIncrement", x => x.MinuteIncrement,
                (x, v) => x.MinuteIncrement = v);

        /// <summary>
        /// Defines the <see cref="ClockIdentifier"/> Property
        /// </summary>
        public static readonly DirectProperty<TimePickerPresenter, string> ClockIdentifierProperty =
           AvaloniaProperty.RegisterDirect<TimePickerPresenter, string>("ClockIdentifier", x => x.ClockIdentifier,
               (x, v) => x.ClockIdentifier = v);

        /// <summary>
        /// Defines the <see cref="Time"/> Property
        /// </summary>
        public static readonly DirectProperty<TimePickerPresenter, TimeSpan> TimeProperty =
            AvaloniaProperty.RegisterDirect<TimePickerPresenter, TimeSpan>("Time", x => x.Time, (x, v) => x.Time = v);

        /// <summary>
        /// Defines the <see cref="SelectorItemTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> SelectorItemTemplateProperty =
            AvaloniaProperty.Register<TimePickerPresenter, IDataTemplate>("SelectorItemTemplate");

        /// <summary>
        /// Gets or sets the MinuteIncrement
        /// </summary>
        public int MinuteIncrement
        {
            get => _minuteIncrement;
            set
            {
                if (value < 1 || value > 59)
                    throw new ArgumentOutOfRangeException("1 >= MinuteIncrement <= 59");
                SetAndRaise(MinuteIncrementProperty, ref _minuteIncrement, value);
                _hasMinuteIncChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets the clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public string ClockIdentifier
        {
            get => _clockIdentifier;
            set
            {
                if (!(string.IsNullOrEmpty(value) || value == "" || value == "12HourClock" || value == "24HourClock"))
                    throw new ArgumentException("Invalid ClockIdentifier");
                SetAndRaise(ClockIdentifierProperty, ref _clockIdentifier, value);
                _hasClockChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets the time in the selectors
        /// </summary>
        public TimeSpan Time
        {
            get => _Time;
            set
            {
                var old = _Time;
                SetAndRaise(TimeProperty, ref _Time, value);
                //SelectedTimeChanged?.Invoke(this, new TimePickerSelectedValueChangedEventArgs(old, value));
                //SetSelectedTimeText();
            }
        }

        /// <summary>
        /// Gets or sets the template for items in the selectors
        /// </summary>
        public IDataTemplate SelectorItemTemplate
        {
            get => GetValue(SelectorItemTemplateProperty);
            set => SetValue(SelectorItemTemplateProperty, value);
        }

        /// <summary>
        /// Raised when the AcceptButton is clicked and the time changes
        /// </summary>
        public event EventHandler<TimePickerValueChangedEventArgs> TimeChanged;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            //Requirement, throw if not found
            _pickerGrid = e.NameScope.Get<Grid>("PickerHost");
            _firstPickerHost = e.NameScope.Get<Border>("FirstPickerHost");
            _secondPickerHost = e.NameScope.Get<Border>("SecondPickerHost");
            _thirdPickerHost = e.NameScope.Get<Border>("ThirdPickerHost");

            _firstSplitter = e.NameScope.Find<Rectangle>("FirstPickerSpacing");
            _secondSplitter = e.NameScope.Find<Rectangle>("SecondPickerSpacing");

            _acceptButton = e.NameScope.Find<Button>("AcceptButton");
            _dismissButton = e.NameScope.Find<Button>("DismissButton");

            if (_acceptButton != null)
                _acceptButton.Click += OnAcceptButtonClicked;
            if (_dismissButton != null)
                _dismissButton.Click += OnDismissButtonClicked;

        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    _hostPopup.IsOpen = false;
                    e.Handled = true;
                    break;
                case Key.Tab:
                    var nextFocus = KeyboardNavigationHandler.GetNext(FocusManager.Instance.Current, NavigationDirection.Next);
                    KeyboardDevice.Instance?.SetFocusedElement(nextFocus, NavigationMethod.Tab, KeyModifiers.None);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    OnConfirmed();
                    e.Handled = true;
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnConfirmed()
        {
            var hr = (_hourSelector.SelectedItem as TimePickerPresenterItem).GetStoredTime().Hours;
            var min = (_minuteSelector.SelectedItem as TimePickerPresenterItem).GetStoredTime().Minutes;
            var period = _periodSelector != null ? _periodSelector.SelectedIndex : -1;

            //Adjust hour to store correctly when using 12HourClock
            if (ClockIdentifier == "12HourClock") //PM
            {
                if (hr == 12 && period == 0)
                    hr = 0;
                else if (period == 1)
                    hr = hr == 12 ? 12 : hr + 12;
            }

            Time = new TimeSpan(hr, min, 0);

            OnTimeChanged(new TimePickerValueChangedEventArgs(_initTime, Time));
            _hostPopup.IsOpen = false;
        }

        protected virtual void OnTimeChanged(TimePickerValueChangedEventArgs args)
        {
            TimeChanged?.Invoke(this, args);
        }

        /// <inheritdoc/>
        public override void ShowAt(Control target)
        {
            if (_hostPopup == null)
            {
                _hostPopup = new Popup();
                _hostPopup.Child = this;
                _hostPopup.PlacementMode = PlacementMode.Bottom;
                _hostPopup.StaysOpen = false;
                ((ISetLogicalParent)_hostPopup).SetParent(target);
                _hostPopup.Closed += OnPopupClosed;
                _hostPopup.Opened += OnPopupOpened;
                _hostPopup.WindowManagerAddShadowHint = false;
                _hostPopup.Focusable = false;
            }

            if (target == null)
                throw new ArgumentNullException("Target cannot be null");

            _hostPopup.PlacementTarget = target;

            this.Width = target.Bounds.Width - 2;

            //Need to open the popup first, so the template is applied & our 
            //template items are available
            _hostPopup.IsOpen = true;

            EnsureSelectorsAndItems();

            SetGrid();

            //Set focus on HourContainer
            KeyboardDevice.Instance?.SetFocusedElement(_hourSelector, NavigationMethod.Pointer, KeyModifiers.None);

            _initTime = Time;

            SetInitialSelection();

            OnOpened();
        }

        /// <summary>
        /// Ensures selectors and items are creates and ready to go
        /// </summary>
        private void EnsureSelectorsAndItems()
        {
            Contract.Requires<NullReferenceException>(_pickerGrid != null);

            var clock = ClockIdentifier;

            if (_hourSelector == null)
            {
                _hourSelector = new LoopingSelector();
                _hourSelector.ShouldLoop = true;
                _hourSelector.ItemTemplate = SelectorItemTemplate;
                _firstPickerHost.Child = _hourSelector;


                if (_hourItems == null)
                {
                    _hourItems = new AvaloniaList<TimePickerPresenterItem>();
                    DateTimeFormatter formatter = new DateTimeFormatter("{hour}");
                    formatter.Clock = clock;

                    int numItems = clock == "12HourClock" ? 12 : 24;

                    for (int i = 0; i < numItems; i++)
                    {
                        var hr = clock == "12HourClock" ? TimeSpan.FromHours(i + 1) : TimeSpan.FromHours(i);
                        TimePickerPresenterItem tppi = new TimePickerPresenterItem(hr);
                        tppi.DisplayText = formatter.Format(hr);
                        _hourItems.Add(tppi);
                    }

                    _hourSelector.Items = _hourItems;
                }
                else if (_hasClockChanged)
                {
                    _hourItems.Clear();
                    DateTimeFormatter formatter = new DateTimeFormatter("{hour}");
                    formatter.Clock = clock;

                    int numItems = clock == "12HourClock" ? 12 : 24;

                    for (int i = 0; i < numItems; i++)
                    {
                        var hr = clock == "12HourClock" ? TimeSpan.FromHours(i + 1) : TimeSpan.FromHours(i);
                        TimePickerPresenterItem tppi = new TimePickerPresenterItem(hr);
                        tppi.DisplayText = formatter.Format(hr);
                        _hourItems.Add(tppi);
                    }
                }
            }

            if (_minuteSelector == null)
            {
                _minuteSelector = new LoopingSelector();
                _minuteSelector.ShouldLoop = true;
                _minuteSelector.ItemTemplate = SelectorItemTemplate;
                _secondPickerHost.Child = _minuteSelector;

                if (_minuteItems == null)
                {
                    _minuteItems = new AvaloniaList<TimePickerPresenterItem>();
                    DateTimeFormatter formatter = new DateTimeFormatter("{minute}");

                    var inc = MinuteIncrement;
                    for (int i = 0; i < 60; i += inc)
                    {
                        var min = TimeSpan.FromMinutes(i);
                        TimePickerPresenterItem tppi = new TimePickerPresenterItem(min);
                        tppi.DisplayText = formatter.Format(min);
                        _minuteItems.Add(tppi);
                    }

                    _minuteSelector.Items = _minuteItems;
                }
                else if (_hasMinuteIncChanged)
                {
                    _minuteItems.Clear();
                    DateTimeFormatter formatter = new DateTimeFormatter("{minute}");

                    var inc = MinuteIncrement;
                    for (int i = 0; i < 60; i += inc)
                    {
                        var min = TimeSpan.FromMinutes(i);
                        TimePickerPresenterItem tppi = new TimePickerPresenterItem(min);
                        tppi.DisplayText = formatter.Format(min);
                        _minuteItems.Add(tppi);
                    }
                }
            }

            if (_periodSelector == null && clock == "12HourClock")
            {
                _periodSelector = new LoopingSelector();
                _periodSelector.ShouldLoop = false;
                _periodSelector.ItemTemplate = SelectorItemTemplate;
                _thirdPickerHost.Child = _periodSelector;

                if (_periodItems == null || _periodItems.Count == 0)
                {
                    _periodItems = new AvaloniaList<TimePickerPresenterItem>();
                    TimePickerPresenterItem amItem = new TimePickerPresenterItem(TimeSpan.Zero);
                    amItem.DisplayText = CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;

                    TimePickerPresenterItem pmItem = new TimePickerPresenterItem(TimeSpan.Zero);
                    pmItem.DisplayText = CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;

                    _periodItems.Add(amItem);
                    _periodItems.Add(pmItem);

                    _periodSelector.Items = _periodItems;
                }
            }
            else if (_periodSelector != null && clock == "24HourClock")
            {
                _thirdPickerHost.Child = null;
                _periodSelector.Items = null;
                _periodSelector = null;
                if (_periodItems != null && _periodItems.Count > 0)
                    _periodItems.Clear();
            }

            _hasMinuteIncChanged = false;
            _hasClockChanged = false;
        }

        /// <summary>
        /// Sets the selector container grid
        /// </summary>
        private void SetGrid()
        {
            if (ClockIdentifier == "12HourClock")
            {
                _pickerGrid.ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto,*");
                _secondSplitter.IsVisible = true;
            }
            else
            {
                _pickerGrid.ColumnDefinitions = new ColumnDefinitions("*,Auto,*");
                _secondSplitter.IsVisible = false;
            }
        }

        /// <summary>
        /// Sets the SelectedIndex of the selectors when first loading
        /// </summary>
        private void SetInitialSelection()
        {
            //Set selection on Hour & Period Selectors
            if (ClockIdentifier == "12HourClock")
            {
                var hr = Time.Hours;
                _periodSelector.SelectedIndex = hr >= 12 ? 1 : 0;

                if (hr == 0)
                    hr += 12;
                if (hr > 12)
                    hr -= 12;

                _hourSelector.SelectedIndex = hr - 1;
            }
            else
            {
                var hr = Time.Hours;
                _hourSelector.SelectedIndex = hr;
            }

            //Set selection on Minute Selector
            //This can be trickier
            //If MinuteIncrement != 1, we can't necessarily set the SelectedIndex
            //Instead, we need to find the closest item, and set it to that
            var minInc = MinuteIncrement;
            var min = Time.Minutes;
            if (minInc == 1)
            {
                _minuteSelector.SelectedIndex = min - 1;
            }
            else
            {
                //Basically, Get the minutes by their increment, we just regenerate here for simplicity
                //Ascending sort by the difference between the current minute & item then get the first item
                //Then find the index of that item
                var items = Enumerable.Range(0, 60).Where(i => i % minInc == 0);
                var nearest = items.OrderBy(x => Math.Abs(x - min)).First();
                _minuteSelector.SelectedIndex = items.ToList().IndexOf(nearest);
            }
        }

        private void OnDismissButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _hostPopup.IsOpen = false;
        }

        private void OnAcceptButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            OnConfirmed();
        }

        private void OnPopupOpened(object sender, EventArgs e)
        {
            //TODO: figure out a dynamic way to set the offset
            //for now, hardcode what lines up with default behavior of 9 items displayed
            //This offset occurs AFTER adjustment for screen bounds occurs, so may need to adjust for that too
            _hostPopup.Host.ConfigurePosition(_hostPopup.PlacementTarget, PlacementMode.Top, new Point(1, -191.5));
        }

        private void OnPopupClosed(object sender, PopupClosedEventArgs e)
        {
            _hostPopup.PlacementTarget.Focus();
            KeyboardDevice.Instance?.SetFocusedElement(_hostPopup.PlacementTarget, NavigationMethod.Pointer, KeyModifiers.None);
            OnClosed();
        }


        //ItemsLists
        private IList<TimePickerPresenterItem> _hourItems;
        private IList<TimePickerPresenterItem> _minuteItems;
        private IList<TimePickerPresenterItem> _periodItems;

        //Template Items
        private Button _acceptButton;
        private Button _dismissButton;
        private Border _firstPickerHost;
        private Border _secondPickerHost;
        private Border _thirdPickerHost;
        private Rectangle _firstSplitter;
        private Rectangle _secondSplitter;
        private Grid _pickerGrid;

        //Selectors
        private LoopingSelector _hourSelector;
        private LoopingSelector _minuteSelector;
        private LoopingSelector _periodSelector;

        private TimeSpan _initTime;
        private bool _hasMinuteIncChanged;
        private bool _hasClockChanged;

        private TimeSpan _Time;
        private int _minuteIncrement;
        private string _clockIdentifier;
    }
}
