using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using System;
using System.Globalization;

namespace Avalonia.Controls
{
    //TODO: WinUI issue proposal for DurationPicker
    //https://github.com/microsoft/microsoft-ui-xaml/issues/1807
    //Could just extend this and add other time units, but design idea for this
    //is unknown

    /// <summary>
    /// A control to allow the user to select a time
    /// </summary>
    public class TimePicker : TemplatedControl
    {
        public TimePicker()
        {
            PseudoClasses.Set(":hasnotime", true);
            _presenter = new TimePickerPresenter();
            _presenter.TimeChanged += OnPresenterTimeChanged;

            //Init the clock property to the System setting
            var timePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
            if (timePattern.IndexOf("H") != -1)
                _clockIdentifier = "24HourClock";
        }

        /// <summary>
        /// Defines the <see cref="MinuteIncrement"/> Property
        /// </summary>
        public static readonly DirectProperty<TimePicker, int> MinuteIncrementProperty =
            AvaloniaProperty.RegisterDirect<TimePicker, int>("MinuteIncrement", x => x.MinuteIncrement,
                (x, v) => x.MinuteIncrement = v);

        /// <summary>
        /// Defines the <see cref="Header"/> Property
        /// </summary>
        public static readonly StyledProperty<object> HeaderProperty =
            AvaloniaProperty.Register<DatePicker, object>("Header");

        /// <summary>
        /// Defines the <see cref="HeaderTemplate"/> Property
        /// </summary>
        public static readonly StyledProperty<IDataTemplate> HeaderTemplateProperty =
            AvaloniaProperty.Register<DatePicker, IDataTemplate>("HeaderTemplate");

        /// <summary>
        /// Defines the <see cref="ClockIdentifier"/> Property
        /// </summary>
        public static readonly DirectProperty<TimePicker, string> ClockIdentifierProperty =
           AvaloniaProperty.RegisterDirect<TimePicker, string>("ClockIdentifier", x => x.ClockIdentifier,
               (x, v) => x.ClockIdentifier = v);

        /// <summary>
        /// Defines the <see cref="SelectedTime"/> Property
        /// </summary>
        public static readonly DirectProperty<TimePicker, TimeSpan?> SelectedTimeProperty =
            AvaloniaProperty.RegisterDirect<TimePicker, TimeSpan?>("Time", x => x.SelectedTime, (x, v) => x.SelectedTime = v);

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
                SetSelectedTimeText();
            }
        }

        /// <summary>
        /// Gets or sets the Header
        /// </summary>
        public object Header
        {
            get => GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the HeaderTemplate
        /// </summary>
        public IDataTemplate HeaderTemplate
        {
            get => GetValue(HeaderTemplateProperty);
            set => SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the ClockIdentifier, either 12HourClock or 24HourClock
        /// </summary>
        public string ClockIdentifier
        {
            get => _clockIdentifier;
            set
            {
                if (!(string.IsNullOrEmpty(value) || value == "" || value == "12HourClock" || value == "24HourClock"))
                    throw new ArgumentException("Invalid ClockIdentifier");
                SetAndRaise(ClockIdentifierProperty, ref _clockIdentifier, value);
                SetGrid();
                SetSelectedTimeText();
            }
        }

        public TimeSpan? SelectedTime
        {
            get => _selectedTime;
            set
            {
                var old = _selectedTime;
                SetAndRaise(SelectedTimeProperty, ref _selectedTime, value);
                OnSelectedTimeChanged(old, value);
                SetSelectedTimeText();
            }
        }

        /// <summary>
        /// Raised when the <see cref="SelectedTime"/> property changes
        /// </summary>
        public event EventHandler<TimePickerSelectedValueChangedEventArgs> SelectedTimeChanged;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _flyoutButton = e.NameScope.Find<Button>("FlyoutButton");

            _firstPickerHost = e.NameScope.Find<Border>("FirstPickerHost");
            _secondPickerHost = e.NameScope.Find<Border>("SecondPickerHost");
            _thirdPickerHost = e.NameScope.Find<Border>("ThirdPickerHost");

            _hourText = e.NameScope.Find<TextBlock>("HourTextBlock");
            _minuteText = e.NameScope.Find<TextBlock>("MinuteTextBlock");
            _periodText = e.NameScope.Find<TextBlock>("PeriodTextBlock");

            _firstSplitter = e.NameScope.Find<Rectangle>("FirstColumnDivider");
            _secondSplitter = e.NameScope.Find<Rectangle>("SecondColumnDivider");

            _contentGrid = e.NameScope.Find<Grid>("FlyoutButtonContentGrid");

            if (_flyoutButton != null)
                _flyoutButton.Click += OnFlyoutButtonClicked;

            SetGrid();
            SetSelectedTimeText();
        }

        protected virtual void OnSelectedTimeChanged(TimeSpan? oldTime, TimeSpan? newTime)
        {
            SelectedTimeChanged?.Invoke(this, new TimePickerSelectedValueChangedEventArgs(oldTime, newTime));
        }

        private void SetGrid()
        {
            if (_contentGrid == null)
                return;

            //This is much simpler than the DatePicker to setup
            //Hour and minute selectors are always present, and the period
            //selector only appears if we're using a 12HourClock

            bool use24HourClock = ClockIdentifier == "24HourClock";

            if (!use24HourClock)
            {
                _contentGrid.ColumnDefinitions = new ColumnDefinitions("*,Auto,*,Auto,*");
                _thirdPickerHost.IsVisible = true;
                _secondSplitter.IsVisible = true;

                Grid.SetColumn(_firstPickerHost, 0);
                Grid.SetColumn(_secondPickerHost, 2);
                Grid.SetColumn(_thirdPickerHost, 4);

                Grid.SetColumn(_firstSplitter, 1);
                Grid.SetColumn(_secondSplitter, 3);
            }
            else
            {
                _contentGrid.ColumnDefinitions = new ColumnDefinitions("*,Auto,*");
                _thirdPickerHost.IsVisible = false;
                _secondSplitter.IsVisible = false;

                Grid.SetColumn(_firstPickerHost, 0);
                Grid.SetColumn(_secondPickerHost, 2);

                Grid.SetColumn(_firstSplitter, 1);
            }
        }

        private void SetSelectedTimeText()
        {
            if (_hourText == null || _minuteText == null || _periodText == null)
                return;

            var time = SelectedTime;
            if (time.HasValue)
            {
                //DateTimeFormatter will automatically handle the correct format for 
                //hours/minutes based on culture settings
                DateTimeFormatter formatter = new DateTimeFormatter("{hour}");
                formatter.Clock = ClockIdentifier;
                _hourText.Text = formatter.Format(time.Value);

                formatter = new DateTimeFormatter("{minute}");
                formatter.Clock = ClockIdentifier;
                _minuteText.Text = formatter.Format(time.Value);
                PseudoClasses.Set(":hasnotime", false);

                if (time.Value.Hours >= 12)
                    _periodText.Text = CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
                else
                    _periodText.Text = CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
            }
            else
            {
                _hourText.Text = "hour";
                _minuteText.Text = "minute";
                PseudoClasses.Set(":hasnotime", true);

                if (DateTime.Now.Hour >= 12)
                    _periodText.Text = CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
                else
                    _periodText.Text = CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
            }
        }

        private void OnFlyoutButtonClicked(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _presenter.ClockIdentifier = ClockIdentifier;
            _presenter.MinuteIncrement = MinuteIncrement;
            _presenter.Time = SelectedTime.HasValue ? SelectedTime.Value : DateTime.Now.TimeOfDay;

            _presenter.ShowAt(this);
        }

        private void OnPresenterTimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            SelectedTime = e.NewTime;
        }


        //Template Items
        private Button _flyoutButton;
        private Border _firstPickerHost;
        private Border _secondPickerHost;
        private Border _thirdPickerHost;
        private TextBlock _hourText;
        private TextBlock _minuteText;
        public TextBlock _periodText;
        private Rectangle _firstSplitter;
        private Rectangle _secondSplitter;
        private Grid _contentGrid;

        private TimePickerPresenter _presenter;

        private TimeSpan? _selectedTime;
        private int _minuteIncrement = 1;
        private string _clockIdentifier = "12HourClock";
    }
}
