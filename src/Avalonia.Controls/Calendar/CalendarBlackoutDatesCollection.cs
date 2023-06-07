// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;

namespace Avalonia.Controls.Primitives
{
    public sealed class CalendarBlackoutDatesCollection : ObservableCollection<CalendarDateRange>
    {
        /// <summary>
        /// The Calendar whose dates this object represents.
        /// </summary>
        private readonly Calendar _owner;
        
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.Primitives.CalendarBlackoutDatesCollection" />
        /// class.
        /// </summary>
        /// <param name="owner">
        /// The <see cref="T:Avalonia.Controls.Calendar" /> whose dates
        /// this object represents.
        /// </param>
        public CalendarBlackoutDatesCollection(Calendar owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

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
                rangeStart = DateTimeHelper.DiscardTime(start);
                rangeEnd = DateTimeHelper.DiscardTime(end);
            }
            else
            {
                rangeStart = DateTimeHelper.DiscardTime(end);
                rangeEnd = DateTimeHelper.DiscardTime(start);
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

        /// <summary>
        /// Removes all items from the collection.
        /// </summary>
        /// <remarks>
        /// This implementation raises the CollectionChanged event.
        /// </remarks>
        protected override void ClearItems()
        {
            EnsureValidThread();

            base.ClearItems();
            _owner.UpdateMonths();
        }

        /// <summary>
        /// Inserts an item into the collection at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index at which item should be inserted.
        /// </param>
        /// <param name="item">The object to insert.</param>
        /// <remarks>
        /// This implementation raises the CollectionChanged event.
        /// </remarks>
        protected override void InsertItem(int index, CalendarDateRange item)
        {
            EnsureValidThread();

            if (!IsValid(item))
            {
                throw new ArgumentOutOfRangeException(nameof(item), "Value is not valid.");
            }

            base.InsertItem(index, item);
            _owner.UpdateMonths();
        }

        /// <summary>
        /// Removes the item at the specified index of the collection.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to remove.
        /// </param>
        /// <remarks>
        /// This implementation raises the CollectionChanged event.
        /// </remarks>
        protected override void RemoveItem(int index)
        {
            EnsureValidThread();

            base.RemoveItem(index);
            _owner.UpdateMonths();
        }

        /// <summary>
        /// Replaces the element at the specified index.
        /// </summary>
        /// <param name="index">
        /// The zero-based index of the element to replace.
        /// </param>
        /// <param name="item">
        /// The new value for the element at the specified index.
        /// </param>
        /// <remarks>
        /// This implementation raises the CollectionChanged event.
        /// </remarks>
        protected override void SetItem(int index, CalendarDateRange item)
        {
            EnsureValidThread();

            if (!IsValid(item))
            {
                throw new ArgumentOutOfRangeException(nameof(item), "Value is not valid.");
            }

            base.SetItem(index, item);
            _owner.UpdateMonths();
        }
        
        private bool IsValid(CalendarDateRange item)
        {
            foreach (DateTime day in _owner.SelectedDates)
            {
                if (DateTimeHelper.InRange(day, item))
                {
                    return false;
                }
            }

            return true;
        }
        
        private static void EnsureValidThread()
        {
            Dispatcher.UIThread.VerifyAccess();
        }
    }
}
