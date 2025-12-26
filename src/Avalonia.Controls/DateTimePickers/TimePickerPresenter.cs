using System;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the presenter used for selecting a time. Intended for use with
    /// <see cref="TimePicker"/> but can be used independently
    /// </summary>
    [TemplatePart(TemplateItems.AcceptButtonName,      typeof(Button), IsRequired = true)]
    [TemplatePart(TemplateItems.DismissButtonName,     typeof(Button))]
    [TemplatePart(TemplateItems.HourDownButtonName,    typeof(Button))]
    [TemplatePart(TemplateItems.HourSelectorName,      typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(TemplateItems.HourUpButtonName,      typeof(Button))]
    [TemplatePart(TemplateItems.MinuteDownButtonName,  typeof(Button))]
    [TemplatePart(TemplateItems.MinuteSelectorName,    typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(TemplateItems.MinuteUpButtonName,    typeof(Button))]
    [TemplatePart(TemplateItems.SecondDownButtonName,  typeof(Button))]
    [TemplatePart(TemplateItems.SecondHostName,        typeof(Panel))]
    [TemplatePart(TemplateItems.SecondSelectorName,    typeof(DateTimePickerPanel))]
    [TemplatePart(TemplateItems.SecondUpButtonName,    typeof(Button))]
    [TemplatePart(TemplateItems.PeriodDownButtonName,  typeof(Button))]
    [TemplatePart(TemplateItems.PeriodHostName,        typeof(Panel), IsRequired = true)]
    [TemplatePart(TemplateItems.PeriodSelectorName,    typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(TemplateItems.PeriodUpButtonName,    typeof(Button))]
    [TemplatePart(TemplateItems.PickerContainerName,   typeof(Grid), IsRequired = true)]
    [TemplatePart(TemplateItems.SecondSpacerName,      typeof(Control), IsRequired = true)]
    [TemplatePart(TemplateItems.ThirdSpacerName,       typeof(Control))]
    public class TimePickerPresenter : PickerPresenterBase
    {
        /// <summary>
        /// Defines the <see cref="MinuteIncrement"/> property
        /// </summary>
        public static readonly StyledProperty<int> MinuteIncrementProperty =
            TimePicker.MinuteIncrementProperty.AddOwner<TimePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="SecondIncrement"/> property
        /// </summary>
        public static readonly StyledProperty<int> SecondIncrementProperty =
            TimePicker.SecondIncrementProperty.AddOwner<TimePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="ClockIdentifier"/> property
        /// </summary>
        public static readonly StyledProperty<string> ClockIdentifierProperty =
            TimePicker.ClockIdentifierProperty.AddOwner<TimePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="UseSeconds"/> property
        /// </summary>
        public static readonly StyledProperty<bool> UseSecondsProperty =
            TimePicker.UseSecondsProperty.AddOwner<TimePickerPresenter>();

        /// <summary>
        /// Defines the <see cref="Time"/> property
        /// </summary>
        public static readonly StyledProperty<TimeSpan> TimeProperty =
            AvaloniaProperty.Register<TimePickerPresenter, TimeSpan>(nameof(Time));

        public TimePickerPresenter()
        {
            SetCurrentValue(TimeProperty, DateTime.Now.TimeOfDay);
        }

        static TimePickerPresenter()
        {
            KeyboardNavigation.TabNavigationProperty.OverrideDefaultValue<TimePickerPresenter>(KeyboardNavigationMode.Cycle);
        }

        private struct TemplateItems
        {
            public Grid _pickerContainer;
            public const string PickerContainerName = "PART_PickerContainer";

            public Button _acceptButton; 
            public const string AcceptButtonName = "PART_AcceptButton";
            public Button? _dismissButton; 
            public const string DismissButtonName = "PART_DismissButton";

            public Control _secondSpacer; // the 2nd spacer, not seconds of time
            public const string SecondSpacerName = "PART_SecondSpacer";
            public Control? _thirdSpacer; 
            public const string ThirdSpacerName = "PART_ThirdSpacer";

            public Panel? _secondHost; 
            public const string SecondHostName = "PART_SecondHost";
            public Panel _periodHost; 
            public const string PeriodHostName = "PART_PeriodHost";

            public DateTimePickerPanel _hourSelector; 
            public const string HourSelectorName = "PART_HourSelector";
            public DateTimePickerPanel _minuteSelector; 
            public const string MinuteSelectorName = "PART_MinuteSelector";
            public DateTimePickerPanel? _secondSelector; 
            public const string SecondSelectorName = "PART_SecondSelector";
            public DateTimePickerPanel _periodSelector; 
            public const string PeriodSelectorName = "PART_PeriodSelector";

            public Button? _hourUpButton; 
            public const string HourUpButtonName = "PART_HourUpButton";
            public Button? _minuteUpButton; 
            public const string MinuteUpButtonName = "PART_MinuteUpButton";
            public Button? _secondUpButton; 
            public const string SecondUpButtonName = "PART_SecondUpButton";
            public Button? _periodUpButton; 
            public const string PeriodUpButtonName = "PART_PeriodUpButton";
            public Button? _hourDownButton; 
            public const string HourDownButtonName = "PART_HourDownButton";
            public Button? _minuteDownButton; 
            public const string MinuteDownButtonName = "PART_MinuteDownButton";
            public Button? _secondDownButton; 
            public const string SecondDownButtonName = "PART_SecondDownButton";
            public Button? _periodDownButton; 
            public const string PeriodDownButtonName = "PART_PeriodDownButton";
        }

        private TemplateItems? _templateItems;

        /// <summary>
        /// Gets or sets the minute increment in the selector
        /// </summary>
        public int MinuteIncrement
        {
            get => GetValue(MinuteIncrementProperty);
            set => SetValue(MinuteIncrementProperty, value);
        }

        /// <summary>
        /// Gets or sets the second increment in the selector
        /// </summary>
        public int SecondIncrement
        {
            get => GetValue(SecondIncrementProperty);
            set => SetValue(SecondIncrementProperty, value);
        }

        /// <summary>
        /// Gets or sets the current clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public string ClockIdentifier
        {
            get => GetValue(ClockIdentifierProperty);
            set => SetValue(ClockIdentifierProperty, value);
        }

        /// <summary>
        /// Gets or sets the current clock identifier, either 12HourClock or 24HourClock
        /// </summary>
        public bool UseSeconds
        {
            get => GetValue(UseSecondsProperty);
            set => SetValue(UseSecondsProperty, value);
        }

        /// <summary>
        /// Gets or sets the current time
        /// </summary>
        public TimeSpan Time
        {
            get => GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _templateItems = new()
            {
                _pickerContainer = e.NameScope.Get<Grid>(TemplateItems.PickerContainerName),
                _periodHost = e.NameScope.Get<Panel>(TemplateItems.PeriodHostName),
                _secondHost = e.NameScope.Find<Panel>(TemplateItems.SecondHostName),

                _hourSelector = e.NameScope.Get<DateTimePickerPanel>(TemplateItems.HourSelectorName),
                _minuteSelector = e.NameScope.Get<DateTimePickerPanel>(TemplateItems.MinuteSelectorName),
                _secondSelector = e.NameScope.Find<DateTimePickerPanel>(TemplateItems.SecondSelectorName),
                _periodSelector = e.NameScope.Get<DateTimePickerPanel>(TemplateItems.PeriodSelectorName),

                _secondSpacer = e.NameScope.Get<Control>(TemplateItems.SecondSpacerName),
                _thirdSpacer = e.NameScope.Find<Control>(TemplateItems.ThirdSpacerName),

                _acceptButton = e.NameScope.Get<Button>(TemplateItems.AcceptButtonName),

                _hourUpButton = SelectorButton(TemplateItems.HourUpButtonName, DateTimePickerPanelType.Hour, SpinDirection.Decrease),
                _hourDownButton = SelectorButton(TemplateItems.HourDownButtonName, DateTimePickerPanelType.Hour, SpinDirection.Increase),

                _minuteUpButton = SelectorButton(TemplateItems.MinuteUpButtonName, DateTimePickerPanelType.Minute, SpinDirection.Decrease),
                _minuteDownButton = SelectorButton(TemplateItems.MinuteDownButtonName, DateTimePickerPanelType.Minute, SpinDirection.Increase),

                _secondUpButton = SelectorButton(TemplateItems.SecondUpButtonName, DateTimePickerPanelType.Second, SpinDirection.Decrease),
                _secondDownButton = SelectorButton(TemplateItems.SecondDownButtonName, DateTimePickerPanelType.Second, SpinDirection.Increase),

                _periodUpButton = SelectorButton(TemplateItems.PeriodUpButtonName, DateTimePickerPanelType.TimePeriod, SpinDirection.Decrease),
                _periodDownButton = SelectorButton(TemplateItems.PeriodDownButtonName, DateTimePickerPanelType.TimePeriod, SpinDirection.Increase),

                _dismissButton = e.NameScope.Find<Button>(TemplateItems.DismissButtonName),
            };

            _templateItems.Value._acceptButton.Click += OnAcceptButtonClicked;
            if (_templateItems.Value._dismissButton is { } dismissButton)
            {
                dismissButton.Click += OnDismissButtonClicked;
            }

            InitPicker();

            Button? SelectorButton(string name, DateTimePickerPanelType type, SpinDirection direction)
            {
                if (e.NameScope.Find<Button>(name) is { } button)
                {
                    button.Click += (s, e) => OnSelectorButtonClick(type, direction);
                    return button;
                }
                return null;
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == MinuteIncrementProperty ||
                change.Property == SecondIncrementProperty ||
                change.Property == ClockIdentifierProperty ||
                change.Property == UseSecondsProperty ||
                change.Property == TimeProperty)
            {
                InitPicker();
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    OnDismiss();
                    e.Handled = true;
                    break;
                case Key.Tab:
                    if (FocusManager.GetFocusManager(this)?.GetFocusedElement() is { } focus)
                    {
                        var nextFocus = KeyboardNavigationHandler.GetNext(focus, NavigationDirection.Next);
                        nextFocus?.Focus(NavigationMethod.Tab);
                        e.Handled = true;
                    }
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
            if (_templateItems is { } items)
            {
                var hr = items._hourSelector.SelectedValue;
                var min = items._minuteSelector.SelectedValue;
                var sec = items._secondSelector?.SelectedValue ?? 0;
                var per = items._periodSelector.SelectedValue;

                if (ClockIdentifier == "12HourClock")
                {
                    hr = per == 1 ? (hr == 12) ? 12 : hr + 12 : per == 0 && hr == 12 ? 0 : hr;
                }

                SetCurrentValue(TimeProperty, new TimeSpan(hr, min, UseSeconds ? sec : 0));
            }
            base.OnConfirmed();
        }

        private void InitPicker()
        {
            if (_templateItems is not { } items)
                return;

            bool clock12 = ClockIdentifier == "12HourClock";
            items._hourSelector.MaximumValue = clock12 ? 12 : 23;
            items._hourSelector.MinimumValue = clock12 ? 1 : 0;
            items._hourSelector.ItemFormat = "%h";
            var hr = Time.Hours;
            items._hourSelector.SelectedValue = !clock12 ? hr :
                hr > 12 ? hr - 12 : hr == 0 ? 12 : hr;

            items._minuteSelector.MaximumValue = 59;
            items._minuteSelector.MinimumValue = 0;
            items._minuteSelector.Increment = MinuteIncrement;
            items._minuteSelector.ItemFormat = "mm";
            items._minuteSelector.SelectedValue = Time.Minutes;

            if (items._secondSelector is { } secondSelector)
            {
                secondSelector.MaximumValue = 59;
                secondSelector.MinimumValue = 0;
                secondSelector.Increment = SecondIncrement;
                secondSelector.ItemFormat = "ss";
                secondSelector.SelectedValue = Time.Seconds;
            }

            items._periodSelector.MaximumValue = 1;
            items._periodSelector.MinimumValue = 0;
            items._periodSelector.SelectedValue = hr >= 12 ? 1 : 0;

            SetGrid(items);
            items._hourSelector.Focus(NavigationMethod.Pointer);
        }

        private void SetGrid(TemplateItems items)
        {
            var use24HourClock = ClockIdentifier == "24HourClock";

            var columnsD = new ColumnDefinitions
            {
                new(GridLength.Star),
                new(GridLength.Auto),
                new(GridLength.Star)
            };

            if (items._secondHost is not null && items._thirdSpacer is not null)
            {
                if (UseSeconds)
                {
                    columnsD.Add(new ColumnDefinition(GridLength.Auto));
                    columnsD.Add(new ColumnDefinition(GridLength.Star));
                }

                items._secondSpacer.IsVisible = UseSeconds;
                items._secondHost.IsVisible = UseSeconds;
                items._thirdSpacer.IsVisible = !use24HourClock;
                items._periodHost.IsVisible = !use24HourClock;

                var amPmColumn = (UseSeconds) ? 6 : 4;

                Grid.SetColumn(items._secondSpacer, UseSeconds ? 3 : 0);
                Grid.SetColumn(items._secondHost, UseSeconds ? 4 : 0);
                Grid.SetColumn(items._thirdSpacer, use24HourClock ? 0 : amPmColumn - 1);
                Grid.SetColumn(items._periodHost, use24HourClock ? 0 : amPmColumn);
            }
            else
            {
                items._secondSpacer.IsVisible = !use24HourClock;
                items._periodHost.IsVisible = !use24HourClock;
                Grid.SetColumn(items._secondSpacer, use24HourClock ? 0 : 3);
                Grid.SetColumn(items._periodHost, use24HourClock ? 0 : 4);
            }

            if (!use24HourClock)
            {
                columnsD.Add(new ColumnDefinition(GridLength.Auto));
                columnsD.Add(new ColumnDefinition(GridLength.Star));
            }

            items._pickerContainer.ColumnDefinitions = columnsD;
        }

        private void OnDismissButtonClicked(object? sender, RoutedEventArgs e)
        {
            OnDismiss();
        }

        private void OnAcceptButtonClicked(object? sender, RoutedEventArgs e)
        {
            OnConfirmed();
        }

        private void OnSelectorButtonClick(DateTimePickerPanelType type, SpinDirection direction)
        {
            var target = type switch
            {
                DateTimePickerPanelType.Hour => _templateItems?._hourSelector,
                DateTimePickerPanelType.Minute => _templateItems?._minuteSelector,
                DateTimePickerPanelType.Second => _templateItems?._secondSelector,
                DateTimePickerPanelType.TimePeriod => _templateItems?._periodSelector,
                _ => throw new NotImplementedException(),
            };

            switch (direction)
            {
                case SpinDirection.Increase:
                    target?.ScrollDown();
                    break;
                case SpinDirection.Decrease:
                    target?.ScrollUp();
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        internal double GetOffsetForPopup()
        {
            if (_templateItems is not { } items)
                return 0;

            var acceptDismissButtonHeight = items._acceptButton.Bounds.Height;
            return -(MaxHeight - acceptDismissButtonHeight) / 2 - (items._hourSelector.ItemHeight / 2);
        }
    }
}
