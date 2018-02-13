// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Collections;
using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Avalonia.Controls.Primitives
{
    public class CalendarBlackoutDatesCollection : ObservableCollection<CalendarDateRange>, IAvaloniaReadOnlyList<CalendarDateRange>
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.Primitives.CalendarBlackoutDatesCollection" />
        /// class.
        /// </summary>
        public CalendarBlackoutDatesCollection()
        { }

        /// <summary>
        /// Adds all dates before <see cref="P:System.DateTime.Today" /> to the
        /// collection.
        /// </summary>
        public void AddDatesInPast()
        {
            Add(new CalendarDateRange(DateTime.MinValue, DateTime.Today.AddDays(-1)));
        }

        /// <summary>
        /// Returns a value that represents whether this collection contains the
        /// specified date.
        /// </summary>
        /// <param name="date">The date to search for.</param>
        /// <returns>
        /// True if the collection contains the specified date; otherwise,
        /// false.
        /// </returns>
        public bool Contains(DateTime date)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
            {
                if (DateTimeHelper.InRange(date, this[i]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a value that represents whether this collection contains the
        /// specified range of dates.
        /// </summary>
        /// <param name="start">The start of the date range.</param>
        /// <param name="end">The end of the date range.</param>
        /// <returns>
        /// True if all dates in the range are contained in the collection;
        /// otherwise, false.
        /// </returns>
        public bool Contains(DateTime start, DateTime end)
        {
            DateTime rangeStart;
            DateTime rangeEnd;

            if (DateTime.Compare(end, start) > -1)
            {
                rangeStart = DateTimeHelper.DiscardTime(start).Value;
                rangeEnd = DateTimeHelper.DiscardTime(end).Value;
            }
            else
            {
                rangeStart = DateTimeHelper.DiscardTime(end).Value;
                rangeEnd = DateTimeHelper.DiscardTime(start).Value;
            }

            int count = Count;
            for (int i = 0; i < count; i++)
            {
                CalendarDateRange range = this[i];
                if (DateTime.Compare(range.Start, rangeStart) == 0 && DateTime.Compare(range.End, rangeEnd) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a value that represents whether this collection contains any
        /// date in the specified range.
        /// </summary>
        /// <param name="range">The range of dates to search for.</param>
        /// <returns>
        /// True if any date in the range is contained in the collection;
        /// otherwise, false.
        /// </returns>
        public bool ContainsAny(CalendarDateRange range)
        {
            return this.Any(r => r.ContainsAny(range));
        }
    }

}
