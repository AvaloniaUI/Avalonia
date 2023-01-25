// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Threading;
using System;
using System.Collections.ObjectModel;

namespace Avalonia.Controls.Primitives
{
    public sealed class SelectedDatesCollection : ObservableCollection<DateTime>
    {
        /// <summary>
        /// Inherited code: Requires comment.
        /// </summary>
        private Collection<DateTime> _addedItems;
        
        /// <summary>
        /// Inherited code: Requires comment.
        /// </summary>
        private bool _isCleared;

        /// <summary>
        /// Inherited code: Requires comment.
        /// </summary>
        private bool _isRangeAdded;

        /// <summary>
        /// Inherited code: Requires comment.
        /// </summary>
        private Calendar _owner;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:Avalonia.Controls.Primitives.SelectedDatesCollection" />
        /// class.
        /// </summary>
        /// <param name="owner">
        /// The <see cref="T:Avalonia.Controls.Calendar" /> associated
        /// with this object.
        /// </param>
        public SelectedDatesCollection(Calendar owner)
        {
            _owner = owner;
            _addedItems = new Collection<DateTime>();
        }

        private void InvokeCollectionChanged(System.Collections.IList removedItems, System.Collections.IList addedItems)
        {
            _owner.OnSelectedDatesCollectionChanged(new SelectionChangedEventArgs(SelectingItemsControl.SelectionChangedEvent, removedItems, addedItems));
        }

        /// <summary>
        /// Adds all the dates in the specified range, which includes the first
        /// and last dates, to the collection.
        /// </summary>
        /// <param name="start">The first date to add to the collection.</param>
        /// <param name="end">The last date to add to the collection.</param>
        public void AddRange(DateTime start, DateTime end)
        {
            DateTime? rangeStart;

            // increment parameter specifies if the Days were selected in
            // Descending order or Ascending order based on this value, we add 
            // the days in the range either in Ascending order or in Descending
            // order
            int increment = (DateTime.Compare(end, start) >= 0) ? 1 : -1;

            _addedItems.Clear();

            rangeStart = start;
            _isRangeAdded = true;

            if (_owner.IsMouseSelection)
            {
                // In Mouse Selection we allow the user to be able to add
                // multiple ranges in one action in MultipleRange Mode.  In
                // SingleRange Mode, we only add the first selected range.
                while (rangeStart.HasValue && DateTime.Compare(end, rangeStart.Value) != -increment)
                {
                    if (Calendar.IsValidDateSelection(_owner, rangeStart))
                    {
                        Add(rangeStart.Value);
                    }
                    else
                    {
                        if (_owner.SelectionMode == CalendarSelectionMode.SingleRange)
                        {
                            _owner.HoverEnd = rangeStart.Value.AddDays(-increment);
                            break;
                        }
                    }

                    rangeStart = DateTimeHelper.AddDays(rangeStart.Value, increment);
                }
            }
            else
            {
                // If CalendarSelectionMode.SingleRange and a user
                // programmatically tries to add multiple ranges, we will throw
                // away the old range and replace it with the new one.  In order
                // to provide the removed items without an additional event, we
                // are calling ClearInternal
                if (_owner.SelectionMode == CalendarSelectionMode.SingleRange && Count > 0)
                {
                    foreach (DateTime item in this)
                    {
                        _owner.RemovedItems.Add(item);
                    }
                    ClearInternal();
                }

                while (rangeStart.HasValue && DateTime.Compare(end, rangeStart.Value) != -increment)
                {
                    Add(rangeStart.Value);
                    rangeStart = DateTimeHelper.AddDays(rangeStart.Value, increment);
                }
            }

            _owner.OnSelectedDatesCollectionChanged(new SelectionChangedEventArgs(SelectingItemsControl.SelectionChangedEvent, _owner.RemovedItems, _addedItems));
            _owner.RemovedItems.Clear();
            _owner.UpdateMonths();
            _isRangeAdded = false;
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

            Collection<DateTime> addedItems = new Collection<DateTime>();
            Collection<DateTime> removedItems = new Collection<DateTime>();

            foreach (DateTime item in this)
            {
                removedItems.Add(item);
            }

            base.ClearItems();

            // The event fires after SelectedDate changes
            if (_owner.SelectionMode != CalendarSelectionMode.None && _owner.SelectedDate != null)
            {
                _owner.SelectedDate = null;
            }

            if (removedItems.Count != 0)
            {
                InvokeCollectionChanged(removedItems, addedItems);
            }
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
        protected override void InsertItem(int index, DateTime item)
        {
            EnsureValidThread();

            if (!Contains(item))
            {
                Collection<DateTime> addedItems = new Collection<DateTime>();

                if (CheckSelectionMode())
                {
                    if (Calendar.IsValidDateSelection(_owner, item))
                    {
                        // If the Collection is cleared since it is SingleRange
                        // and it had another range set the index to 0
                        if (_isCleared)
                        {
                            index = 0;
                            _isCleared = false;
                        }

                        base.InsertItem(index, item);

                        // The event fires after SelectedDate changes
                        if (index == 0 && !(_owner.SelectedDate.HasValue && DateTime.Compare(_owner.SelectedDate.Value, item) == 0))
                        {
                            _owner.SelectedDate = item;
                        }

                        if (!_isRangeAdded)
                        {
                            addedItems.Add(item);

                            InvokeCollectionChanged(_owner.RemovedItems, addedItems);
                            _owner.RemovedItems.Clear();
                            int monthDifference = DateTimeHelper.CompareYearMonth(item, _owner.DisplayDateInternal);

                            if (monthDifference < 2 && monthDifference > -2)
                            {
                                _owner.UpdateMonths();
                            }
                        }
                        else
                        {
                            _addedItems.Add(item);
                        }
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException("SelectedDate value is not valid.");
                    }
                }
            }
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

            if (index >= Count)
            {
                base.RemoveItem(index);
            }
            else
            {
                Collection<DateTime> addedItems = new Collection<DateTime>();
                Collection<DateTime> removedItems = new Collection<DateTime>();
                int monthDifference = DateTimeHelper.CompareYearMonth(this[index], _owner.DisplayDateInternal);

                removedItems.Add(this[index]);
                base.RemoveItem(index);

                // The event fires after SelectedDate changes
                if (index == 0)
                {
                    if (Count > 0)
                    {
                        _owner.SelectedDate = this[0];
                    }
                    else
                    {
                        _owner.SelectedDate = null;
                    }
                }

                InvokeCollectionChanged(removedItems, addedItems);

                if (monthDifference < 2 && monthDifference > -2)
                {
                    _owner.UpdateMonths();
                }
            }
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
        protected override void SetItem(int index, DateTime item)
        {
            EnsureValidThread();

            if (!Contains(item))
            {
                Collection<DateTime> addedItems = new Collection<DateTime>();
                Collection<DateTime> removedItems = new Collection<DateTime>();

                if (index >= Count)
                {
                    base.SetItem(index, item);
                }
                else
                {
                    if (DateTime.Compare(this[index], item) != 0 && Calendar.IsValidDateSelection(_owner, item))
                    {
                        removedItems.Add(this[index]);
                        base.SetItem(index, item);
                        addedItems.Add(item);

                        // The event fires after SelectedDate changes
                        if (index == 0 && !(_owner.SelectedDate.HasValue && DateTime.Compare(_owner.SelectedDate.Value, item) == 0))
                        {
                            _owner.SelectedDate = item;
                        }
                        InvokeCollectionChanged(removedItems, addedItems);

                        int monthDifference = DateTimeHelper.CompareYearMonth(item, _owner.DisplayDateInternal);

                        if (monthDifference < 2 && monthDifference > -2)
                        {
                            _owner.UpdateMonths();
                        }
                    }
                }
            }
        }
        
        internal void ClearInternal()
        {
            base.ClearItems();
        }
        
        private bool CheckSelectionMode()
        {
            if (_owner.SelectionMode == CalendarSelectionMode.None)
            {
                throw new InvalidOperationException("The SelectedDate property cannot be set when the selection mode is None.");
            }
            if (_owner.SelectionMode == CalendarSelectionMode.SingleDate && Count > 0)
            {
                throw new InvalidOperationException("The SelectedDates collection can be changed only in a multiple selection mode. Use the SelectedDate in a single selection mode.");
            }

            // if user tries to add an item into the SelectedDates in
            // SingleRange mode, we throw away the old range and replace it with
            // the new one in order to provide the removed items without an
            // additional event, we are calling ClearInternal
            if (_owner.SelectionMode == CalendarSelectionMode.SingleRange && !_isRangeAdded && Count > 0)
            {
                foreach (DateTime item in this)
                {
                    _owner.RemovedItems.Add(item);
                }
                ClearInternal();
                _isCleared = true;
            }
            return true;
        }
        
        private static void EnsureValidThread()
        {
            Dispatcher.UIThread.VerifyAccess();
        }
    }
}
