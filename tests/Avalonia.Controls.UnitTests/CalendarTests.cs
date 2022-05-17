using Xunit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Avalonia.Controls.UnitTests
{
    public class CalendarTests
    {
        private static bool CompareDates(DateTime first, DateTime second)
        {
            return first.Year == second.Year && 
                first.Month == second.Month &&
                first.Day == second.Day;
        }

        [Fact]
        public void SelectedDatesChanged_Should_Fire_When_SelectedDate_Set()
        {
            bool handled = false;
            Calendar calendar = new Calendar();
            calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            calendar.SelectedDatesChanged += new EventHandler<SelectionChangedEventArgs>(delegate
            {
                handled = true;
            });
            DateTime value = new DateTime(2000, 10, 10);
            calendar.SelectedDate = value;
            Assert.True(handled);
        }

        [Fact]
        public void DisplayDateChanged_Should_Fire_When_DisplayDate_Set()
        {
            bool handled = false;
            Calendar calendar = new Calendar();
            calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            calendar.DisplayDateChanged += new EventHandler<CalendarDateChangedEventArgs>(delegate
            {
                handled = true;
            });
            DateTime value = new DateTime(2000, 10, 10);
            calendar.DisplayDate = value;
            Assert.True(handled);
        }

        [Fact]
        public void Setting_Selected_Date_To_Blackout_Date_Should_Throw()
        {
            Calendar calendar = new Calendar();
            calendar.BlackoutDates.AddDatesInPast();

            Assert.ThrowsAny<ArgumentOutOfRangeException>(
                () => calendar.SelectedDate = DateTime.Today.AddDays(-1));
        }

        [Fact]
        public void Setting_Selected_Date_To_Blackout_Date_Should_Throw_Range()
        {
            Calendar calendar = new Calendar();
            calendar.BlackoutDates.Add(new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(10)));

            calendar.SelectedDate = DateTime.Today.AddDays(-1);
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(-1)));
            Assert.True(CompareDates(calendar.SelectedDate.Value, calendar.SelectedDates[0]));

            calendar.SelectedDate = DateTime.Today.AddDays(11);
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(11)));
            Assert.True(CompareDates(calendar.SelectedDate.Value, calendar.SelectedDates[0]));

            Assert.ThrowsAny<ArgumentOutOfRangeException>(
                () => calendar.SelectedDate = DateTime.Today.AddDays(5));
        }

        [Fact]
        public void Adding_Blackout_Dates_Containing_Selected_Date_Should_Throw()
        {
            Calendar calendar = new Calendar();
            calendar.SelectedDate = DateTime.Today.AddDays(5);

            Assert.ThrowsAny<ArgumentOutOfRangeException>(
                () => calendar.BlackoutDates.Add(new CalendarDateRange(DateTime.Today, DateTime.Today.AddDays(10))));
        }

        [Fact]
        public void DisplayDateStartEnd_Should_Constrain_Display_Date()
        {
            Calendar calendar = new Calendar();
            calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            calendar.DisplayDateStart = new DateTime(2005, 12, 30);

            DateTime value = new DateTime(2005, 12, 15);
            calendar.DisplayDate = value;
            Assert.True(CompareDates(calendar.DisplayDate, calendar.DisplayDateStart.Value));

            value = new DateTime(2005, 12, 30);
            calendar.DisplayDate = value;
            Assert.True(CompareDates(calendar.DisplayDate, value));

            value = DateTime.MaxValue;
            calendar.DisplayDate = value;
            Assert.True(CompareDates(calendar.DisplayDate, value));

            calendar.DisplayDateEnd = new DateTime(2010, 12, 30);
            Assert.True(CompareDates(calendar.DisplayDate, calendar.DisplayDateEnd.Value));
        }

        [Fact]
        public void Setting_DisplayDateEnd_Should_Alter_DispalyDate_And_DisplayDateStart()
        {
            Calendar calendar = new Calendar();
            DateTime value = new DateTime(2000, 1, 30);

            calendar.DisplayDate = value;
            calendar.DisplayDateEnd = value;
            calendar.DisplayDateStart = value;
            Assert.True(CompareDates(calendar.DisplayDateStart.Value, value));
            Assert.True(CompareDates(calendar.DisplayDateEnd.Value, value));

            value = value.AddMonths(2);
            calendar.DisplayDateStart = value;
            Assert.True(CompareDates(calendar.DisplayDateStart.Value, value));
            Assert.True(CompareDates(calendar.DisplayDateEnd.Value, value));
            Assert.True(CompareDates(calendar.DisplayDate, value));
        }

        [Fact]
        public void Display_Date_Range_End_Will_Contain_SelectedDate()
        {
            Calendar calendar = new Calendar();
            calendar.SelectionMode = CalendarSelectionMode.SingleDate;

            calendar.SelectedDate = DateTime.MaxValue;
            Assert.True(CompareDates((DateTime)calendar.SelectedDate, DateTime.MaxValue));

            calendar.DisplayDateEnd = DateTime.MaxValue.AddDays(-1);
            Assert.True(CompareDates((DateTime)calendar.DisplayDateEnd, DateTime.MaxValue));
        }


        /// <summary>
        /// The days added to the SelectedDates collection.
        /// </summary>
        private IList<object> _selectedDatesChangedAddedDays;

        /// <summary>
        /// The days removed from the SelectedDates collection.
        /// </summary>
        private IList<object> _selectedDateChangedRemovedDays;

        /// <summary>
        /// The number of times the SelectedDatesChanged event has been fired.
        /// </summary>
        private int _selectedDatesChangedCount;

        /// <summary>
        /// Handle the SelectedDatesChanged event.
        /// </summary>
        /// <param name="sender">The calendar.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDatesChangedAddedDays =
                e.AddedItems
                 .Cast<object>()
                 .ToList();
            _selectedDateChangedRemovedDays = 
                e.RemovedItems
                 .Cast<object>()
                 .ToList();
            _selectedDatesChangedCount++;
        }

        /// <summary>
        /// Clear the variables used to track the SelectedDatesChanged event.
        /// </summary>
        private void ResetSelectedDatesChanged()
        {
            if (_selectedDatesChangedAddedDays != null)
            {
                _selectedDatesChangedAddedDays.Clear();
            }

            if (_selectedDateChangedRemovedDays != null)
            {
                _selectedDateChangedRemovedDays.Clear();
            }

            _selectedDatesChangedCount = 0;
        }

        [Fact]
        public void SingleDate_Selection_Behavior()
        {
            ResetSelectedDatesChanged();
            Calendar calendar = new Calendar();
            calendar.SelectedDatesChanged += new EventHandler<SelectionChangedEventArgs>(OnSelectedDatesChanged);
            calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            calendar.SelectedDate = DateTime.Today;
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
            Assert.True(calendar.SelectedDates.Count == 1);
            Assert.True(CompareDates(calendar.SelectedDates[0], DateTime.Today));
            Assert.True(_selectedDatesChangedCount == 1);
            Assert.True(_selectedDatesChangedAddedDays.Count == 1);
            Assert.True(_selectedDateChangedRemovedDays.Count == 0);
            ResetSelectedDatesChanged();

            calendar.SelectedDate = DateTime.Today;
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
            Assert.True(calendar.SelectedDates.Count == 1);
            Assert.True(CompareDates(calendar.SelectedDates[0], DateTime.Today));
            Assert.True(_selectedDatesChangedCount == 0);

            calendar.ClearValue(Calendar.SelectedDateProperty);

            calendar.SelectionMode = CalendarSelectionMode.None;
            Assert.True(calendar.SelectedDates.Count == 0);
            Assert.Null(calendar.SelectedDate);

            calendar.SelectionMode = CalendarSelectionMode.SingleDate;

            calendar.SelectedDates.Add(DateTime.Today.AddDays(1));
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(1)));
            Assert.True(calendar.SelectedDates.Count == 1);

            Assert.ThrowsAny<InvalidOperationException>(
                () => calendar.SelectedDates.Add(DateTime.Today.AddDays(2)));
        }

        [Fact]
        public void SingleRange_Selection_Behavior()
        {
            ResetSelectedDatesChanged();
            Calendar calendar = new Calendar();
            calendar.SelectedDatesChanged += new EventHandler<SelectionChangedEventArgs>(OnSelectedDatesChanged);
            calendar.SelectionMode = CalendarSelectionMode.SingleRange;
            calendar.SelectedDate = DateTime.Today;
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
            Assert.True(calendar.SelectedDates.Count == 1);
            Assert.True(CompareDates(calendar.SelectedDates[0], DateTime.Today));
            Assert.True(_selectedDatesChangedCount == 1);
            Assert.True(_selectedDatesChangedAddedDays.Count == 1);
            Assert.True(_selectedDateChangedRemovedDays.Count == 0);
            ResetSelectedDatesChanged();

            calendar.SelectedDates.Clear();
            Assert.Null(calendar.SelectedDate);
            ResetSelectedDatesChanged();

            calendar.SelectedDates.AddRange(DateTime.Today, DateTime.Today.AddDays(10));
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today));
            Assert.True(calendar.SelectedDates.Count == 11);
            ResetSelectedDatesChanged();

            calendar.SelectedDates.AddRange(DateTime.Today, DateTime.Today.AddDays(10));
            Assert.True(calendar.SelectedDates.Count == 11);
            Assert.True(_selectedDatesChangedCount == 0);
            ResetSelectedDatesChanged();

            calendar.SelectedDates.AddRange(DateTime.Today.AddDays(-20), DateTime.Today);
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(-20)));
            Assert.True(calendar.SelectedDates.Count == 21);
            Assert.True(_selectedDatesChangedCount == 1);
            Assert.True(_selectedDatesChangedAddedDays.Count == 21);
            Assert.True(_selectedDateChangedRemovedDays.Count == 11);
            ResetSelectedDatesChanged();

            calendar.SelectedDates.Add(DateTime.Today.AddDays(100));
            Assert.True(CompareDates(calendar.SelectedDate.Value, DateTime.Today.AddDays(100)));
            Assert.True(calendar.SelectedDates.Count == 1);
        }
    }
}
