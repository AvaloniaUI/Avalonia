using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control to allow the user to select a date
    /// </summary>
    [TemplatePart("PART_ButtonContentGrid", typeof(Grid))]
    [TemplatePart("PART_DayTextBlock",      typeof(TextBlock))]
    [TemplatePart("PART_FirstSpacer",       typeof(Rectangle))]
    [TemplatePart("PART_FlyoutButton",      typeof(Button))]
    [TemplatePart("PART_MonthTextBlock",    typeof(TextBlock))]
    [TemplatePart("PART_PickerPresenter",   typeof(DatePickerPresenter))]
    [TemplatePart("PART_Popup",             typeof(Popup))]
    [TemplatePart("PART_SecondSpacer",      typeof(Rectangle))]
    [TemplatePart("PART_YearTextBlock",     typeof(TextBlock))]
    [PseudoClasses(":hasnodate")]
    public class DatePicker : TemplatedControl
    {
        /// <summary>
        /// Define the <see cref="DayFormat"/> Property
        /// </summary>
        public static readonly StyledProperty<string> DayFormatProperty =
            AvaloniaProperty.Register<DatePicker, string>(nameof(DayFormat), "%d");

        /// <summary>
        /// Defines the <see cref="DayVisible"/> Property
        /// </summary>
        public static readonly StyledProperty<bool> DayVisibleProperty =
            AvaloniaProperty.Register<DatePicker, bool>(nameof(DayVisible), true);

        /// <summary>
        /// Defines the <see cref="MaxYear"/> Property
        /// </summary>
        public static readonly StyledProperty<DateTimeOffset> MaxYearProperty =
            AvaloniaProperty.Register<DatePicker, DateTimeOffset>(nameof(MaxYear), DateTimeOffset.MaxValue, coerce: CoerceMaxYear);

        /// <summary>
        /// Defines the <see cref="MinYear"/> Property
        /// </summary>
        public static readonly StyledProperty<DateTimeOffset> MinYearProperty =
            AvaloniaProperty.Register<DatePicker, DateTimeOffset>(nameof(MinYear), DateTimeOffset.MinValue, coerce: CoerceMinYear);

        /// <summary>
        /// Defines the <see cref="MonthFormat"/> Property
        /// </summary>
        public static readonly StyledProperty<string> MonthFormatProperty =
            AvaloniaProperty.Register<DatePicker, string>(nameof(MonthFormat), "MMMM");

        /// <summary>
        /// Defines the <see cref="MonthVisible"/> Property
        /// </summary>
        public static readonly StyledProperty<bool> MonthVisibleProperty =
            AvaloniaProperty.Register<DatePicker, bool>(nameof(MonthVisible), true);

        /// <summary>
        /// Defines the <see cref="YearFormat"/> Property
        /// </summary>
        public static readonly StyledProperty<string> YearFormatProperty =
            AvaloniaProperty.Register<DatePicker, string>(nameof(YearFormat), "yyyy");

        /// <summary>
        /// Defines the <see cref="YearVisible"/> Property
        /// </summary>
        public static readonly StyledProperty<bool> YearVisibleProperty =
            AvaloniaProperty.Register<DatePicker, bool>(nameof(YearVisible), true);

        /// <summary>
        /// Defines the <see cref="SelectedDate"/> Property
        /// </summary>
        public static readonly StyledProperty<DateTimeOffset?> SelectedDateProperty =
            AvaloniaProperty.Register<DatePicker, DateTimeOffset?>(nameof(SelectedDate),
                defaultBindingMode: BindingMode.TwoWay, enableDataValidation: true);

        // Template Items
        private Button? _flyoutButton;
        private TextBlock? _dayText;
        private TextBlock? _monthText;
        private TextBlock? _yearText;
        private Grid? _container;
        private Rectangle? _spacer1;
        private Rectangle? _spacer2;
        private Popup? _popup;
        private DatePickerPresenter? _presenter;

        private bool _areControlsAvailable;

        public DatePicker()
        {
            PseudoClasses.Set(":hasnodate", true);
            var now = DateTimeOffset.Now;
            SetCurrentValue(MinYearProperty, new DateTimeOffset(now.Date.Year - 100, 1, 1, 0, 0, 0, now.Offset));
            SetCurrentValue(MaxYearProperty, new DateTimeOffset(now.Date.Year + 100, 12, 31, 0, 0, 0, now.Offset));
        }

        private static void OnGridVisibilityChanged(DatePicker sender, AvaloniaPropertyChangedEventArgs e) => sender.SetGrid();

        public string DayFormat
        {
            get => GetValue(DayFormatProperty);
            set => SetValue(DayFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the day is visible
        /// </summary>
        public bool DayVisible
        {
            get => GetValue(DayVisibleProperty);
            set => SetValue(DayVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum year for the picker
        /// </summary>
        public DateTimeOffset MaxYear
        {
            get => GetValue(MaxYearProperty);
            set => SetValue(MaxYearProperty, value);
        }

        private static DateTimeOffset CoerceMaxYear(AvaloniaObject sender, DateTimeOffset value)
        {
            if (value < sender.GetValue(MinYearProperty))
            {
                throw new InvalidOperationException($"{MaxYearProperty.Name} cannot be less than {MinYearProperty.Name}");
            }

            return value;
        }

        private void OnMaxYearChanged(DateTimeOffset? value)
        {
            if (SelectedDate.HasValue && SelectedDate.Value > value)
                SetCurrentValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum year for the picker
        /// </summary>
        public DateTimeOffset MinYear
        {
            get => GetValue(MinYearProperty);
            set => SetValue(MinYearProperty, value);
        }

        private static DateTimeOffset CoerceMinYear(AvaloniaObject sender, DateTimeOffset value)
        {
            if (value > sender.GetValue(MaxYearProperty))
            {
                throw new InvalidOperationException($"{MinYearProperty.Name} cannot be greater than {MaxYearProperty.Name}");
            }

            return value;
        }

        private void OnMinYearChanged(DateTimeOffset? value)
        {
            if (SelectedDate.HasValue && SelectedDate.Value < value)
                SetCurrentValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Gets or sets the month format
        /// </summary>
        public string MonthFormat
        {
            get => GetValue(MonthFormatProperty);
            set => SetValue(MonthFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the month is visible
        /// </summary>
        public bool MonthVisible
        {
            get => GetValue(MonthVisibleProperty);
            set => SetValue(MonthVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the year format
        /// </summary>
        public string YearFormat
        {
            get => GetValue(YearFormatProperty);
            set => SetValue(YearFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the year is visible
        /// </summary>
        public bool YearVisible
        {
            get => GetValue(YearVisibleProperty);
            set => SetValue(YearVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the Selected Date for the picker, can be null
        /// </summary>
        public DateTimeOffset? SelectedDate
        {
            get => GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Raised when the <see cref="SelectedDate"/> changes
        /// </summary>
        public event EventHandler<DatePickerSelectedValueChangedEventArgs>? SelectedDateChanged;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            _areControlsAvailable = false;
            if (_flyoutButton != null)
                _flyoutButton.Click -= OnFlyoutButtonClicked;
            if (_presenter != null)
            {
                _presenter.Confirmed -= OnConfirmed;
                _presenter.Dismissed -= OnDismissPicker;
            }

            base.OnApplyTemplate(e);
            _flyoutButton = e.NameScope.Find<Button>("PART_FlyoutButton");
            _dayText = e.NameScope.Find<TextBlock>("PART_DayTextBlock");
            _monthText = e.NameScope.Find<TextBlock>("PART_MonthTextBlock");
            _yearText = e.NameScope.Find<TextBlock>("PART_YearTextBlock");
            _container = e.NameScope.Find<Grid>("PART_ButtonContentGrid");
            _spacer1 = e.NameScope.Find<Rectangle>("PART_FirstSpacer");
            _spacer2 = e.NameScope.Find<Rectangle>("PART_SecondSpacer");
            _popup = e.NameScope.Find<Popup>("PART_Popup");
            _presenter = e.NameScope.Find<DatePickerPresenter>("PART_PickerPresenter");

            _areControlsAvailable = true;

            SetGrid();
            SetSelectedDateText();

            if (_flyoutButton != null)
                _flyoutButton.Click += OnFlyoutButtonClicked;

            if (_presenter != null)
            {
                _presenter.Confirmed += OnConfirmed;
                _presenter.Dismissed += OnDismissPicker;

                _presenter[!DatePickerPresenter.MaxYearProperty] = this[!MaxYearProperty];
                _presenter[!DatePickerPresenter.MinYearProperty] = this[!MinYearProperty];

                _presenter[!DatePickerPresenter.MonthVisibleProperty] = this[!MonthVisibleProperty];
                _presenter[!DatePickerPresenter.MonthFormatProperty] = this[!MonthFormatProperty];

                _presenter[!DatePickerPresenter.DayVisibleProperty] = this[!DayVisibleProperty];
                _presenter[!DatePickerPresenter.DayFormatProperty] = this[!DayFormatProperty];

                _presenter[!DatePickerPresenter.YearVisibleProperty] = this[!YearVisibleProperty];
                _presenter[!DatePickerPresenter.YearFormatProperty] = this[!YearFormatProperty];
            }
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == DayVisibleProperty || change.Property == MonthVisibleProperty || change.Property == YearVisibleProperty)
            {
                SetGrid();
            }
            else if (change.Property == MaxYearProperty)
            {
                OnMaxYearChanged(change.GetNewValue<DateTimeOffset>());
            }
            else if (change.Property == MinYearProperty)
            {
                OnMinYearChanged(change.GetNewValue<DateTimeOffset>());
            }
            else if (change.Property == SelectedDateProperty)
            {
                SetSelectedDateText();

                var (oldValue, newValue) = change.GetOldAndNewValue<DateTimeOffset?>();
                OnSelectedDateChanged(this, new DatePickerSelectedValueChangedEventArgs(oldValue, newValue));
            }
        }

        private void OnDismissPicker(object? sender, EventArgs e)
        {
            _popup!.Close();
            Focus();
        }

        private void OnConfirmed(object? sender, EventArgs e)
        {
            _popup!.Close();
            SetCurrentValue(SelectedDateProperty, _presenter!.Date);
        }

        private void SetGrid()
        {
            if (_container == null)
                return;

            var fmt = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
            var columns = new List<(TextBlock?, int)>
            {
                (_monthText, MonthVisible ? fmt.IndexOf("m", StringComparison.OrdinalIgnoreCase) : -1),
                (_yearText, YearVisible ? fmt.IndexOf("y", StringComparison.OrdinalIgnoreCase) : -1),
                (_dayText, DayVisible ? fmt.IndexOf("d", StringComparison.OrdinalIgnoreCase) : -1),
            };

            columns.Sort((x, y) => x.Item2 - y.Item2);
            _container.ColumnDefinitions.Clear();

            var columnIndex = 0;

            foreach (var column in columns)
            {
                if (column.Item1 is null)
                    continue;

                column.Item1.IsVisible = column.Item2 != -1;

                if (column.Item2 != -1)
                {
                    if (columnIndex > 0)
                    {
                        _container.ColumnDefinitions.Add(new ColumnDefinition(0, GridUnitType.Auto));
                    }

                    _container.ColumnDefinitions.Add(
                        new ColumnDefinition(column.Item1 == _monthText ? 138 : 78, GridUnitType.Star));

                    if (column.Item1.Parent is null)
                    {
                        _container.Children.Add(column.Item1);
                    }

                    Grid.SetColumn(column.Item1, (columnIndex++ * 2));
                }
            }

            var isSpacer1Visible = columnIndex > 1;
            var isSpacer2Visible = columnIndex > 2;
            // ternary conditional operator is used to make sure grid cells will be validated
            Grid.SetColumn(_spacer1!, isSpacer1Visible ? 1 : 0);
            Grid.SetColumn(_spacer2!, isSpacer2Visible ? 3 : 0);

            _spacer1!.IsVisible = isSpacer1Visible;
            _spacer2!.IsVisible = isSpacer2Visible;
        }

        private void SetSelectedDateText()
        {
            if (!_areControlsAvailable)
                return;

            if (SelectedDate.HasValue)
            {
                PseudoClasses.Set(":hasnodate", false);
                var selDate = SelectedDate.Value;
                _monthText!.Text = selDate.ToString(MonthFormat);
                _yearText!.Text = selDate.ToString(YearFormat);
                _dayText!.Text = selDate.ToString(DayFormat);
            }
            else
            {
                PseudoClasses.Set(":hasnodate", true);
                _monthText!.Text = "month";
                _yearText!.Text = "year";
                _dayText!.Text = "day";
            }
        }

        private void OnFlyoutButtonClicked(object? sender, RoutedEventArgs e)
        {
            if (_presenter == null)
                throw new InvalidOperationException("No DatePickerPresenter found.");
            if (_popup == null)
                throw new InvalidOperationException("No Popup found.");

            _presenter.Date = SelectedDate ?? DateTimeOffset.Now;

            _popup.Placement = PlacementMode.AnchorAndGravity;
            _popup.PlacementAnchor = Primitives.PopupPositioning.PopupAnchor.Bottom;
            _popup.PlacementGravity = Primitives.PopupPositioning.PopupGravity.Bottom;
            _popup.PlacementConstraintAdjustment = Primitives.PopupPositioning.PopupPositionerConstraintAdjustment.SlideY;
            _popup.IsOpen = true;

            // Overlay popup hosts won't get measured until the next layout pass, but we need the
            // template to be applied to `_presenter` now. Detect this case and force a layout pass.
            if (!_presenter.IsMeasureValid)
                (VisualRoot as ILayoutRoot)?.LayoutManager?.ExecuteInitialLayoutPass();

            var deltaY = _presenter.GetOffsetForPopup();

            // The extra 5 px I think is related to default popup placement behavior
            _popup.VerticalOffset = deltaY + 5;
        }

        protected virtual void OnSelectedDateChanged(object? sender, DatePickerSelectedValueChangedEventArgs e)
        {
            SelectedDateChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// Clear <see cref="SelectedDate"/>.
        /// </summary>
        public void Clear()
        {
            SetCurrentValue(SelectedDateProperty, null);
        }

        protected override void UpdateDataValidation(AvaloniaProperty property, BindingValueType state, Exception? error)
        {
            base.UpdateDataValidation(property, state, error);

            if (property == SelectedDateProperty)
                DataValidationErrors.SetError(this, error);
        }
    }
}
