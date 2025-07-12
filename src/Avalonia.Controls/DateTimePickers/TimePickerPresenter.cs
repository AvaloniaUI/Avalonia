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
    [TemplatePart(nameof(TemplateItems.PART_AcceptButton),      typeof(Button), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_DismissButton),     typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_HourDownButton),    typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_HourSelector),      typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_HourUpButton),      typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_MinuteDownButton),  typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_MinuteSelector),    typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_MinuteUpButton),    typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_SecondDownButton),  typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_SecondHost),        typeof(Panel))]
    [TemplatePart(nameof(TemplateItems.PART_SecondSelector),    typeof(DateTimePickerPanel))]
    [TemplatePart(nameof(TemplateItems.PART_SecondUpButton),    typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_PeriodDownButton),  typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_PeriodHost),        typeof(Panel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_PeriodSelector),    typeof(DateTimePickerPanel), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_PeriodUpButton),    typeof(Button))]
    [TemplatePart(nameof(TemplateItems.PART_PickerContainer),   typeof(Grid), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_SecondSpacer),      typeof(Control), IsRequired = true)]
    [TemplatePart(nameof(TemplateItems.PART_ThirdSpacer),       typeof(Control))]
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
            public Grid PART_PickerContainer;

            public Button PART_AcceptButton;
            public Button? PART_DismissButton;

            public Control PART_SecondSpacer; // the 2nd spacer, not seconds of time
            public Control? PART_ThirdSpacer;

            public Panel? PART_SecondHost;
            public Panel PART_PeriodHost;

            public DateTimePickerPanel PART_HourSelector;
            public DateTimePickerPanel PART_MinuteSelector;
            public DateTimePickerPanel? PART_SecondSelector;
            public DateTimePickerPanel PART_PeriodSelector;

            public Button? PART_HourUpButton;
            public Button? PART_MinuteUpButton;
            public Button? PART_SecondUpButton;
            public Button? PART_PeriodUpButton;
            public Button? PART_HourDownButton;
            public Button? PART_MinuteDownButton;
            public Button? PART_SecondDownButton;
            public Button? PART_PeriodDownButton;
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
                PART_PickerContainer = e.NameScope.Get<Grid>(nameof(TemplateItems.PART_PickerContainer)),
                PART_PeriodHost = e.NameScope.Get<Panel>(nameof(TemplateItems.PART_PeriodHost)),
                PART_SecondHost = e.NameScope.Find<Panel>(nameof(TemplateItems.PART_SecondHost)),

                PART_HourSelector = e.NameScope.Get<DateTimePickerPanel>(nameof(TemplateItems.PART_HourSelector)),
                PART_MinuteSelector = e.NameScope.Get<DateTimePickerPanel>(nameof(TemplateItems.PART_MinuteSelector)),
                PART_SecondSelector = e.NameScope.Find<DateTimePickerPanel>(nameof(TemplateItems.PART_SecondSelector)),
                PART_PeriodSelector = e.NameScope.Get<DateTimePickerPanel>(nameof(TemplateItems.PART_PeriodSelector)),

                PART_SecondSpacer = e.NameScope.Get<Control>(nameof(TemplateItems.PART_SecondSpacer)),
                PART_ThirdSpacer = e.NameScope.Find<Control>(nameof(TemplateItems.PART_ThirdSpacer)),

                PART_AcceptButton = e.NameScope.Get<Button>(nameof(TemplateItems.PART_AcceptButton)),

                PART_HourUpButton = SelectorButton(nameof(TemplateItems.PART_HourUpButton), DateTimePickerPanelType.Hour, SpinDirection.Decrease),
                PART_HourDownButton = SelectorButton(nameof(TemplateItems.PART_HourDownButton), DateTimePickerPanelType.Hour, SpinDirection.Increase),

                PART_MinuteUpButton = SelectorButton(nameof(TemplateItems.PART_MinuteUpButton), DateTimePickerPanelType.Minute, SpinDirection.Decrease),
                PART_MinuteDownButton = SelectorButton(nameof(TemplateItems.PART_MinuteDownButton), DateTimePickerPanelType.Minute, SpinDirection.Increase),

                PART_SecondUpButton = SelectorButton(nameof(TemplateItems.PART_SecondUpButton), DateTimePickerPanelType.Second, SpinDirection.Decrease),
                PART_SecondDownButton = SelectorButton(nameof(TemplateItems.PART_SecondDownButton), DateTimePickerPanelType.Second, SpinDirection.Increase),

                PART_PeriodUpButton = SelectorButton(nameof(TemplateItems.PART_PeriodUpButton), DateTimePickerPanelType.TimePeriod, SpinDirection.Decrease),
                PART_PeriodDownButton = SelectorButton(nameof(TemplateItems.PART_PeriodDownButton), DateTimePickerPanelType.TimePeriod, SpinDirection.Increase),

                PART_DismissButton = e.NameScope.Find<Button>(nameof(TemplateItems.PART_DismissButton)),
            };

            _templateItems.Value.PART_AcceptButton.Click += OnAcceptButtonClicked;
            if (_templateItems.Value.PART_DismissButton is { } dismissButton)
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
                var hr = items.PART_HourSelector.SelectedValue;
                var min = items.PART_MinuteSelector.SelectedValue;
                var sec = items.PART_SecondSelector?.SelectedValue ?? 0;
                var per = items.PART_PeriodSelector.SelectedValue;

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
            items.PART_HourSelector.MaximumValue = clock12 ? 12 : 23;
            items.PART_HourSelector.MinimumValue = clock12 ? 1 : 0;
            items.PART_HourSelector.ItemFormat = "%h";
            var hr = Time.Hours;
            items.PART_HourSelector.SelectedValue = !clock12 ? hr :
                hr > 12 ? hr - 12 : hr == 0 ? 12 : hr;

            items.PART_MinuteSelector.MaximumValue = 59;
            items.PART_MinuteSelector.MinimumValue = 0;
            items.PART_MinuteSelector.Increment = MinuteIncrement;
            items.PART_MinuteSelector.ItemFormat = "mm";
            items.PART_MinuteSelector.SelectedValue = Time.Minutes;

            if (items.PART_SecondSelector is { } secondSelector)
            {
                secondSelector.MaximumValue = 59;
                secondSelector.MinimumValue = 0;
                secondSelector.Increment = SecondIncrement;
                secondSelector.ItemFormat = "ss";
                secondSelector.SelectedValue = Time.Seconds;
            }

            items.PART_PeriodSelector.MaximumValue = 1;
            items.PART_PeriodSelector.MinimumValue = 0;
            items.PART_PeriodSelector.SelectedValue = hr >= 12 ? 1 : 0;

            SetGrid(items);
            items.PART_HourSelector.Focus(NavigationMethod.Pointer);
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

            if (items.PART_SecondHost is not null && items.PART_ThirdSpacer is not null)
            {
                if (UseSeconds)
                {
                    columnsD.Add(new ColumnDefinition(GridLength.Auto));
                    columnsD.Add(new ColumnDefinition(GridLength.Star));
                }

                items.PART_SecondSpacer.IsVisible = UseSeconds;
                items.PART_SecondHost.IsVisible = UseSeconds;
                items.PART_ThirdSpacer.IsVisible = !use24HourClock;
                items.PART_PeriodHost.IsVisible = !use24HourClock;

                var amPmColumn = (UseSeconds) ? 6 : 4;

                Grid.SetColumn(items.PART_SecondSpacer, UseSeconds ? 3 : 0);
                Grid.SetColumn(items.PART_SecondHost, UseSeconds ? 4 : 0);
                Grid.SetColumn(items.PART_ThirdSpacer, use24HourClock ? 0 : amPmColumn - 1);
                Grid.SetColumn(items.PART_PeriodHost, use24HourClock ? 0 : amPmColumn);
            }
            else
            {
                items.PART_SecondSpacer.IsVisible = !use24HourClock;
                items.PART_PeriodHost.IsVisible = !use24HourClock;
                Grid.SetColumn(items.PART_SecondSpacer, use24HourClock ? 0 : 3);
                Grid.SetColumn(items.PART_PeriodHost, use24HourClock ? 0 : 4);
            }

            if (!use24HourClock)
            {
                columnsD.Add(new ColumnDefinition(GridLength.Auto));
                columnsD.Add(new ColumnDefinition(GridLength.Star));
            }

            items.PART_PickerContainer.ColumnDefinitions = columnsD;
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
                DateTimePickerPanelType.Hour => _templateItems?.PART_HourSelector,
                DateTimePickerPanelType.Minute => _templateItems?.PART_MinuteSelector,
                DateTimePickerPanelType.Second => _templateItems?.PART_SecondSelector,
                DateTimePickerPanelType.TimePeriod => _templateItems?.PART_PeriodSelector,
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

            var acceptDismissButtonHeight = items.PART_AcceptButton.Bounds.Height;
            return -(MaxHeight - acceptDismissButtonHeight) / 2 - (items.PART_HourSelector.ItemHeight / 2);
        }
    }
}
