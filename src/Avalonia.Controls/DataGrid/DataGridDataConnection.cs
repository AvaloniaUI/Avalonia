// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;

namespace Avalonia.Controls
{
    /*
    public interface ICollectionViewBase
    {
        //
        // Summary:
        //     Gets a value that indicates whether this view supports filtering by way of the
        //     System.ComponentModel.ICollectionView.Filter property.
        //
        // Returns:
        //     true if this view supports filtering; otherwise, false.
        bool CanFilter { get; }
        //
        // Summary:
        //     Gets a value that indicates whether this view supports grouping by way of the
        //     System.ComponentModel.ICollectionView.GroupDescriptions property.
        //
        // Returns:
        //     true if this view supports grouping; otherwise, false.
        bool CanGroup { get; }
        //
        // Summary:
        //     Gets a value that indicates whether this view supports sorting by way of the
        //     System.ComponentModel.ICollectionView.SortDescriptions property.
        //
        // Returns:
        //     true if this view supports sorting; otherwise, false.
        bool CanSort { get; }
        //
        // Summary:
        //     Gets or sets the cultural information for any operations of the view that may
        //     differ by culture, such as sorting.
        //
        // Returns:
        //     The culture information to use during culture-sensitive operations.
        CultureInfo Culture { get; set; }
        //
        // Summary:
        //     Gets the current item in the view.
        //
        // Returns:
        //     The current item in the view or null if there is no current item.
        object CurrentItem { get; }
        //
        // Summary:
        //     Gets the ordinal position of the System.ComponentModel.ICollectionView.CurrentItem
        //     in the view.
        //
        // Returns:
        //     The ordinal position of the System.ComponentModel.ICollectionView.CurrentItem
        //     in the view.
        int CurrentPosition { get; }
        //
        // Summary:
        //     Gets or sets a callback that is used to determine whether an item is appropriate
        //     for inclusion in the view.
        //
        // Returns:
        //     A method that is used to determine whether an item is appropriate for inclusion
        //     in the view.
        Predicate<object> Filter { get; set; }
        //
        // Summary:
        //     Gets a collection of System.ComponentModel.GroupDescription objects that describe
        //     how the items in the collection are grouped in the view.
        //
        // Returns:
        //     A collection of objects that describe how the items in the collection are grouped
        //     in the view.
        //ObservableCollection<GroupDescription> GroupDescriptions { get; }
        //
        // Summary:
        //     Gets the top-level groups.
        //
        // Returns:
        //     A read-only collection of the top-level groups or null if there are no groups.
        //ReadOnlyObservableCollection<object> Groups { get; }
        
            
            
        //
        // Summary:
        //     Gets a value that indicates whether the System.ComponentModel.ICollectionView.CurrentItem
        //     of the view is beyond the end of the collection.
        //
        // Returns:
        //     true if the System.ComponentModel.ICollectionView.CurrentItem of the view is
        //     beyond the end of the collection; otherwise, false.
        bool IsCurrentAfterLast { get; }
        //
        // Summary:
        //     Gets a value that indicates whether the System.ComponentModel.ICollectionView.CurrentItem
        //     of the view is beyond the start of the collection.
        //
        // Returns:
        //     true if the System.ComponentModel.ICollectionView.CurrentItem of the view is
        //     beyond the start of the collection; otherwise, false.
        bool IsCurrentBeforeFirst { get; }
        //
        // Summary:
        //     Gets a value that indicates whether the view is empty.
        //
        // Returns:
        //     true if the view is empty; otherwise, false.
        bool IsEmpty { get; }
        //
        // Summary:
        //     Gets a collection of System.ComponentModel.SortDescription instances that describe
        //     how the items in the collection are sorted in the view.
        //
        // Returns:
        //     A collection of values that describe how the items in the collection are sorted
        //     in the view.
        //SortDescriptionCollection SortDescriptions { get; }
        
            
        //
        // Summary:
        //     Gets the underlying collection.
        //
        // Returns:
        //     The underlying collection.
        IEnumerable SourceCollection { get; }

        //
        // Summary:
        //     Occurs after the current item has been changed.
        event EventHandler CurrentChanged;
        //
        // Summary:
        //     Occurs before the current item changes.
        event CurrentChangingEventHandler CurrentChanging;

        //
        // Summary:
        //     Indicates whether the specified item belongs to this collection view.
        //
        // Parameters:
        //   item:
        //     The object to check.
        //
        // Returns:
        //     true if the item belongs to this collection view; otherwise, false.
        bool Contains(object item);
        //
        // Summary:
        //     Enters a defer cycle that you can use to merge changes to the view and delay
        //     automatic refresh.
        //
        // Returns:
        //     The typical usage is to create a using scope with an implementation of this method
        //     and then include multiple view-changing calls within the scope. The implementation
        //     should delay automatic refresh until after the using scope exits.
        IDisposable DeferRefresh();
        //
        // Summary:
        //     Sets the specified item in the view as the System.ComponentModel.ICollectionView.CurrentItem.
        //
        // Parameters:
        //   item:
        //     The item to set as the current item.
        //
        // Returns:
        //     true if the resulting System.ComponentModel.ICollectionView.CurrentItem is an
        //     item in the view; otherwise, false.
        bool MoveCurrentTo(object item);
        //
        // Summary:
        //     Sets the first item in the view as the System.ComponentModel.ICollectionView.CurrentItem.
        //
        // Returns:
        //     true if the resulting System.ComponentModel.ICollectionView.CurrentItem is an
        //     item in the view; otherwise, false.
        bool MoveCurrentToFirst();
        //
        // Summary:
        //     Sets the last item in the view as the System.ComponentModel.ICollectionView.CurrentItem.
        //
        // Returns:
        //     true if the resulting System.ComponentModel.ICollectionView.CurrentItem is an
        //     item in the view; otherwise, false.
        bool MoveCurrentToLast();
        //
        // Summary:
        //     Sets the item after the System.ComponentModel.ICollectionView.CurrentItem in
        //     the view as the System.ComponentModel.ICollectionView.CurrentItem.
        //
        // Returns:
        //     true if the resulting System.ComponentModel.ICollectionView.CurrentItem is an
        //     item in the view; otherwise, false.
        bool MoveCurrentToNext();
        //
        // Summary:
        //     Sets the item at the specified index to be the System.ComponentModel.ICollectionView.CurrentItem
        //     in the view.
        //
        // Parameters:
        //   position:
        //     The index to set the System.ComponentModel.ICollectionView.CurrentItem to.
        //
        // Returns:
        //     true if the resulting System.ComponentModel.ICollectionView.CurrentItem is an
        //     item in the view; otherwise, false.
        bool MoveCurrentToPosition(int position);

        /// <summary>
        /// Sets the item before the System.ComponentModel.ICollectionView.CurrentItem in
        /// the view to the System.ComponentModel.ICollectionView.CurrentItem.
        /// </summary>
        /// <returns>
        /// true if the resulting System.ComponentModel.ICollectionView.CurrentItem is an
        /// item in the view; otherwise, false.
        /// </returns>
        bool MoveCurrentToPrevious();

        /// <summary>
        /// Recreates the view.
        /// </summary>
        void Refresh();

    }
    public interface ICollectionView
    {
        int Count { get; }
        int IndexOf(object item);
    }
    public interface IEditableCollectionView : ICollectionView
    {
        //
        // Summary:
        //     Gets a value that indicates whether a new item can be added to the collection.
        //
        // Returns:
        //     true if a new item can be added to the collection; otherwise, false.
        bool CanAddNew { get; }
        //
        // Summary:
        //     Gets a value that indicates whether the collection view can discard pending changes
        //     and restore the original values of an edited object.
        //
        // Returns:
        //     true if the collection view can discard pending changes and restore the original
        //     values of an edited object; otherwise, false.
        bool CanCancelEdit { get; }
        //
        // Summary:
        //     Gets a value that indicates whether an item can be removed from the collection.
        //
        // Returns:
        //     true if an item can be removed from the collection; otherwise, false.
        bool CanRemove { get; }
        //
        // Summary:
        //     Gets the item that is being added during the current add transaction.
        //
        // Returns:
        //     The item that is being added if System.ComponentModel.IEditableCollectionView.IsAddingNew
        //     is true; otherwise, null.
        object CurrentAddItem { get; }
        //
        // Summary:
        //     Gets the item in the collection that is being edited.
        //
        // Returns:
        //     The item that is being edited if System.ComponentModel.IEditableCollectionView.IsEditingItem
        //     is true; otherwise, null.
        object CurrentEditItem { get; }
        //
        // Summary:
        //     Gets a value that indicates whether an add transaction is in progress.
        //
        // Returns:
        //     true if an add transaction is in progress; otherwise, false.
        bool IsAddingNew { get; }
        //
        // Summary:
        //     Gets a value that indicates whether an edit transaction is in progress.
        //
        // Returns:
        //     true if an edit transaction is in progress; otherwise, false.
        bool IsEditingItem { get; }
        //
        // Summary:
        //     Gets or sets the position of the new item placeholder in the collection view.
        //
        // Returns:
        //     An enumeration value that specifies the position of the new item placeholder
        //     in the collection view.
        //NewItemPlaceholderPosition NewItemPlaceholderPosition { get; set; }

        //
        // Summary:
        //     Adds a new item to the underlying collection.
        //
        // Returns:
        //     The new item that is added to the collection.
        object AddNew();
        //
        // Summary:
        //     Ends the edit transaction and, if possible, restores the original value of the
        //     item.
        void CancelEdit();
        //
        // Summary:
        //     Ends the add transaction and discards the pending new item.
        void CancelNew();
        //
        // Summary:
        //     Ends the edit transaction and saves the pending changes.
        void CommitEdit();
        //
        // Summary:
        //     Ends the add transaction and saves the pending new item.
        void CommitNew();
        //
        // Summary:
        //     Begins an edit transaction on the specified item.
        //
        // Parameters:
        //   item:
        //     The item to edit.
        void EditItem(object item);
        //
        // Summary:
        //     Removes the specified item from the collection.
        //
        // Parameters:
        //   item:
        //     The item to remove.
        void Remove(object item);
        //
        // Summary:
        //     Removes the item at the specified position from the collection.
        //
        // Parameters:
        //   index:
        //     Index of item to remove.
        void RemoveAt(int index);
    }
    */

    internal class DataGridDataConnection
    {
        #region Data
        
        private int _backupSlotForCurrentChanged;
        private int _columnForCurrentChanged;
        private PropertyInfo[] _dataProperties;
        private IEnumerable _dataSource;
        private Type _dataType;
        private bool _expectingCurrentChanged;
        private object _itemToSelectOnCurrentChanged;
        private DataGrid _owner;
        private bool _scrollForCurrentChanged;
        private DataGridSelectionAction _selectionActionForCurrentChanged;

        /*
        private WeakEventListener<DataGridDataConnection, object, NotifyCollectionChangedEventArgs> _weakCollectionChangedListener;
        private WeakEventListener<DataGridDataConnection, object, NotifyCollectionChangedEventArgs> _weakSortDescriptionsCollectionChangedListener;
        private WeakEventListener<DataGridDataConnection, object, CurrentChangingEventArgs> _weakCurrentChangingListener;
        private WeakEventListener<DataGridDataConnection, object, EventArgs> _weakCurrentChangedListener;
        */

        #endregion Data

        public DataGridDataConnection(DataGrid owner)
        {
            _owner = owner;
        }

        #region Properties

        public bool AllowEdit
        {
            get
            {
                if (List == null)
                {
                    return true;
                }
                else
                {
                    return !List.IsReadOnly;
                }
            }
        }

        /// <summary>
        /// True if the collection view says it can sort.
        /// </summary>
        //TODO
        //CollectionView
        //Edit
        public bool AllowSort
        {
            get
            {
                return false;
                //if (CollectionView == null ||
                //    (EditableCollectionView != null && (EditableCollectionView.IsAddingNew || EditableCollectionView.IsEditingItem)))
                //{
                //    return false;
                //}
                //else
                //{
                //    return CollectionView.CanSort;
                //}
            }
        }

        public bool CommittingEdit
        {
            get;
            private set;
        }

        public int Count
        {
            get
            {
                IList list = List;
                if (list != null)
                {
                    return list.Count;
                }

                if(DataSource is PagedCollectionView cv)
                {
                    return cv.Count;
                }

                return DataSource?.Cast<object>().Count() ?? 0;
            }
        }

        public bool DataIsPrimitive
        {
            get
            {
                return DataTypeIsPrimitive(DataType);
            }
        }

        public PropertyInfo[] DataProperties
        {
            get
            {
                if (_dataProperties == null)
                {
                    UpdateDataProperties();
                }
                return _dataProperties;
            }
        }

        public IEnumerable DataSource
        {
            get
            {
                return _dataSource;
            }
            set
            {
                _dataSource = value;
                // Because the DataSource is changing, we need to reset our cached values for DataType and DataProperties,
                // which are dependent on the current DataSource
                _dataType = null;
                UpdateDataProperties();
            }
        }

        public Type DataType
        {
            get
            {
                // We need to use the raw ItemsSource as opposed to DataSource because DataSource
                // may be the ItemsSource wrapped in a collection view, in which case we wouldn't
                // be able to take T to be the type if we're given IEnumerable<T>
                if (_dataType == null && _owner.Items != null)
                {
                    _dataType = _owner.Items.GetItemType();
                }
                return _dataType;
            }
        }

        public bool EventsWired
        {
            get;
            private set;
        }

        //TODO
        //Grouping
        private bool IsGrouping
        {
            get
            {
                return false;
                //return (CollectionView != null)
                //    && (CollectionView.CanGroup)
                //    && (CollectionView.GroupDescriptions != null)
                //    && (CollectionView.GroupDescriptions.Count > 0);
            }
        }

        public IList List
        {
            get
            {
                return DataSource as IList;
            }
        }

        public bool ShouldAutoGenerateColumns
        {
            get
            {
                return false;
                //return _owner.AutoGenerateColumns
                //    && (_owner.ColumnsInternal.AutogeneratedColumnCount == 0)
                //    && ((DataProperties != null && DataProperties.Length > 0) || DataIsPrimitive);
            }
        }

        public ICollectionView CollectionView
        {
            get
            {
                return DataSource as ICollectionView;
            }
        }
        public IEditableCollectionView EditableCollectionView
        {
            get
            {
                return DataSource as IEditableCollectionView;
            }
        }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// Puts the entity into editing mode if possible
        /// </summary>
        /// <param name="dataItem">The entity to edit</param>
        /// <returns>True if editing was started</returns>
        //TODO
        //Edit
        public bool BeginEdit(object dataItem)
        {
            throw new NotImplementedException();
            //if (dataItem == null)
            //{
            //    return false;
            //}

            //IEditableCollectionView editableCollectionView = EditableCollectionView;
            //if (editableCollectionView != null)
            //{
            //    if (editableCollectionView.IsEditingItem && (dataItem == editableCollectionView.CurrentEditItem))
            //    {
            //        return true;
            //    }
            //    else
            //    {
            //        editableCollectionView.EditItem(dataItem);
            //        return editableCollectionView.IsEditingItem;
            //    }
            //}

            //if (dataItem is IEditableObject editableDataItem)
            //{
            //    editableDataItem.BeginEdit();
            //    return true;
            //}

            //return true;
        }

        /// <summary>
        /// Cancels the current entity editing and exits the editing mode.
        /// </summary>
        /// <param name="dataItem">The entity being edited</param>
        /// <returns>True if a cancellation operation was invoked.</returns>
        //TODO
        //Edit
        public bool CancelEdit(object dataItem)
        {
            throw new NotImplementedException();
            //IEditableCollectionView editableCollectionView = EditableCollectionView;
            //if (editableCollectionView != null)
            //{
            //    if (editableCollectionView.CanCancelEdit)
            //    {
            //        editableCollectionView.CancelEdit();
            //        return true;
            //    }
            //    return false;
            //}

            //IEditableObject editableDataItem = dataItem as IEditableObject;
            //if (editableDataItem != null)
            //{
            //    editableDataItem.CancelEdit();
            //    return true;
            //}

            //return true;
        }

        public static bool CanEdit(Type type)
        {
            Debug.Assert(type != null);

            type = type.GetNonNullableType();

            return
                type.IsEnum
                || type == typeof(System.String)
                || type == typeof(System.Char)
                || type == typeof(System.DateTime)
                || type == typeof(System.Boolean)
                || type == typeof(System.Byte)
                || type == typeof(System.SByte)
                || type == typeof(System.Single)
                || type == typeof(System.Double)
                || type == typeof(System.Decimal)
                || type == typeof(System.Int16)
                || type == typeof(System.Int32)
                || type == typeof(System.Int64)
                || type == typeof(System.UInt16)
                || type == typeof(System.UInt32)
                || type == typeof(System.UInt64);
        }

        /// <summary>
        /// Commits the current entity editing and exits the editing mode.
        /// </summary>
        /// <param name="dataItem">The entity being edited</param>
        /// <returns>True if a commit operation was invoked.</returns>
        //TODO
        //Edit
        public bool EndEdit(object dataItem)
        {
            throw new NotImplementedException();
            //IEditableCollectionView editableCollectionView = EditableCollectionView;
            //if (editableCollectionView != null)
            //{
            //    // IEditableCollectionView.CommitEdit can potentially change currency. If it does,
            //    // we don't want to attempt a second commit inside our CurrentChanging event handler.
            //    _owner.NoCurrentCellChangeCount++;
            //    CommittingEdit = true;
            //    try
            //    {
            //        editableCollectionView.CommitEdit();
            //    }
            //    finally
            //    {
            //        _owner.NoCurrentCellChangeCount--;
            //        CommittingEdit = false;
            //    }
            //    return true;
            //}

            //IEditableObject editableDataItem = dataItem as IEditableObject;
            //if (editableDataItem != null)
            //{
            //    editableDataItem.EndEdit();
            //}

            //return true;
        }

        //TODO
        // Assumes index >= 0, returns null if index >= Count
        public object GetDataItem(int index)
        {
            Debug.Assert(index >= 0);

            IList list = List;
            if (list != null)
            {
                return (index < list.Count) ? list[index] : null;
            }

            if (DataSource is PagedCollectionView collectionView)
            {
                return (index < collectionView.Count) ? collectionView.GetItemAt(index) : null;
            }

            IEnumerable enumerable = DataSource;
            if (enumerable != null)
            {
                IEnumerator enumerator = enumerable.GetEnumerator();
                int i = -1;
                while (enumerator.MoveNext() && i < index)
                {
                    i++;
                    if (i == index)
                    {
                        return enumerator.Current;
                    }
                }
            }
            return null;
        }

        public bool GetPropertyIsReadOnly(string propertyName)
        {
            if (DataType != null)
            {
                if (!String.IsNullOrEmpty(propertyName))
                {
                    Type propertyType = DataType;
                    PropertyInfo propertyInfo = null;
                    List<string> propertyNames = TypeHelper.SplitPropertyPath(propertyName);
                    for (int i = 0; i < propertyNames.Count; i++)
                    {
                        propertyInfo = propertyType.GetPropertyOrIndexer(propertyNames[i], out object[] index);
                        if (propertyInfo == null || propertyType.GetIsReadOnly() || propertyInfo.GetIsReadOnly())
                        {
                            // Either the data type is read-only, the property doesn't exist, or it does exist but is read-only
                            return true;
                        }

                        // Check if EditableAttribute is defined on the property and if it indicates uneditable
                        //object[] attributes = propertyInfo.GetCustomAttributes(typeof(EditableAttribute), true);
                        //if (attributes != null && attributes.Length > 0)
                        //{
                        //    EditableAttribute editableAttribute = attributes[0] as EditableAttribute;
                        //    Debug.Assert(editableAttribute != null);
                        //    if (!editableAttribute.AllowEdit)
                        //    {
                        //        return true;
                        //    }
                        //}
                        propertyType = propertyInfo.PropertyType.GetNonNullableType();
                    }
                    return propertyInfo == null || !propertyInfo.CanWrite || !AllowEdit || !CanEdit(propertyType);
                }
                else if (DataType.GetIsReadOnly())
                {
                    return true;
                }
            }
            return !AllowEdit;
        }

        public int IndexOf(object dataItem)
        {
            IList list = List;
            if (list != null)
            {
                return list.IndexOf(dataItem);
            }

            if (DataSource is PagedCollectionView cv)
            {
                return cv.IndexOf(dataItem);
            }

            IEnumerable enumerable = DataSource;
            if (enumerable != null && dataItem != null)
            {
                int index = 0;
                foreach (object dataItemTmp in enumerable)
                {
                    if ((dataItem == null && dataItemTmp == null) ||
                        dataItem.Equals(dataItemTmp))
                    {
                        return index;
                    }
                    index++;
                }
            }
            return -1;
        }

        #endregion Public methods

        #region Internal Methods

        internal void ClearDataProperties()
        {
            _dataProperties = null;
        }

        /// <summary>
        /// Creates a collection view around the DataGrid's source. ICollectionViewFactory is
        /// used if the source implements it. Otherwise a PagedCollectionView is returned.
        /// </summary>
        /// <param name="source">Enumerable source for which to create a view</param>
        /// <returns>ICollectionView view over the provided source</returns>
        internal static ICollectionView CreateView(IEnumerable source)
        {
            Debug.Assert(source != null, "source unexpectedly null");
            Debug.Assert(!(source is ICollectionView), "source is an ICollectionView");

            ICollectionView collectionView = null;

            if (source is ICollectionViewFactory collectionViewFactory)
            {
                // If the source is a collection view factory, give it a chance to produce a custom collection view.
                collectionView = collectionViewFactory.CreateView();
                // Intentionally not catching potential exception thrown by ICollectionViewFactory.CreateView().
            }
            if (collectionView == null)
            {
                // If we still do not have a collection view, default to a PagedCollectionView.
                collectionView = new PagedCollectionView(source);
            }
            return collectionView;
        }

        internal static bool DataTypeIsPrimitive(Type dataType)
        {
            if (dataType != null)
            {
                Type type = TypeHelper.GetNonNullableType(dataType);  // no-opt if dataType isn't nullable
                return type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(Decimal);
            }
            else
            {
                return false;
            }
        }

        //TODO Grouping
        internal void MoveCurrentTo(object item, int backupSlot, int columnIndex, DataGridSelectionAction action, bool scrollIntoView)
        {
            if (CollectionView != null)
            {
                _expectingCurrentChanged = true;
                _columnForCurrentChanged = columnIndex;
                _itemToSelectOnCurrentChanged = item;
                _selectionActionForCurrentChanged = action;
                _scrollForCurrentChanged = scrollIntoView;
                _backupSlotForCurrentChanged = backupSlot;

                CollectionView.MoveCurrentTo(item);
                //CollectionView.MoveCurrentTo(item is CollectionViewGroup ? null : item);

                _expectingCurrentChanged = false;
            }
        }

        //TODO Sorting
        internal void UnWireEvents(IEnumerable value)
        {
            if (value is INotifyCollectionChanged notifyingDataSource)
            {
                notifyingDataSource.CollectionChanged -= NotifyingDataSource_CollectionChanged;
            }

            //if (SortDescriptions != null)
            //{
            //    ((INotifyCollectionChanged)SortDescriptions).CollectionChanged -= CollectionView_SortDescriptions_CollectionChanged;
            //}

            if (CollectionView != null)
            {
                CollectionView.CurrentChanged -= CollectionView_CurrentChanged;
                CollectionView.CurrentChanging -= CollectionView_CurrentChanging;
            }

            EventsWired = false;
        }

        //TODO Sorting
        internal void WireEvents(IEnumerable value)
        {
            if (value is INotifyCollectionChanged notifyingDataSource)
            {
                notifyingDataSource.CollectionChanged += NotifyingDataSource_CollectionChanged;
            }

            //if (SortDescriptions != null)
            //{
            //    ((INotifyCollectionChanged)SortDescriptions).CollectionChanged += new NotifyCollectionChangedEventHandler(CollectionView_SortDescriptions_CollectionChanged);
            //}

            if (CollectionView != null)
            {
                //Avalonia.Utilities.WeakSubscriptionManager.Subscribe(CollectionView, "CurrentChanged", CollectionView_CurrentChanged);
                CollectionView.CurrentChanged += CollectionView_CurrentChanged;
                CollectionView.CurrentChanging += CollectionView_CurrentChanging;
            }

            EventsWired = true;
        }

        #endregion Internal Methods

        #region Private methods

        //TODO Grouping
        private void CollectionView_CurrentChanged(object sender, EventArgs e)
        {
            if (_expectingCurrentChanged)
            {
                // Committing Edit could cause our item to move to a group that no longer exists.  In
                // this case, we need to update the item.
                //CollectionViewGroup collectionViewGroup = _itemToSelectOnCurrentChanged as CollectionViewGroup;
                //if (collectionViewGroup != null)
                //{
                //    DataGridRowGroupInfo groupInfo = _owner.RowGroupInfoFromCollectionViewGroup(collectionViewGroup);
                //    if (groupInfo == null)
                //    {
                //        // Move to the next slot if the target slot isn't visible                        
                //        if (!_owner.IsSlotVisible(_backupSlotForCurrentChanged))
                //        {
                //            _backupSlotForCurrentChanged = _owner.GetNextVisibleSlot(_backupSlotForCurrentChanged);
                //        }
                //        // Move to the next best slot if we've moved past all the slots.  This could happen if multiple
                //        // groups were removed.
                //        if (_backupSlotForCurrentChanged >= _owner.SlotCount)
                //        {
                //            _backupSlotForCurrentChanged = _owner.GetPreviousVisibleSlot(_owner.SlotCount);
                //        }
                //        // Update the itemToSelect
                //        int newCurrentPosition = -1;
                //        _itemToSelectOnCurrentChanged = _owner.ItemFromSlot(_backupSlotForCurrentChanged, ref newCurrentPosition);
                //    }
                //}

                _owner.ProcessSelectionAndCurrency(
                    _columnForCurrentChanged,
                    _itemToSelectOnCurrentChanged,
                    _backupSlotForCurrentChanged,
                    _selectionActionForCurrentChanged,
                    _scrollForCurrentChanged);
            }
            else if (CollectionView != null)
            {
                _owner.UpdateStateOnCurrentChanged(CollectionView.CurrentItem, CollectionView.CurrentPosition);
            }
        }

        private void CollectionView_CurrentChanging(object sender, CurrentChangingEventArgs e)
        {
            if (_owner.NoCurrentCellChangeCount == 0 &&
                !_expectingCurrentChanged &&
                !CommittingEdit &&
                !_owner.CommitEdit())
            {
                // If CommitEdit failed, then the user has most likely input invalid data.
                // We should cancel the current change if we can, otherwise we have to abort the edit.
                if (e.IsCancelable)
                {
                    e.Cancel = true;
                }
                else
                {
                    _owner.CancelEdit(DataGridEditingUnit.Row, false);
                }
            }
        }

        private void CollectionView_SortDescriptions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_owner.ColumnsItemsInternal.Count == 0)
            {
                return;
            }

            // refresh sort description
            foreach (DataGridColumn column in _owner.ColumnsItemsInternal)
            {
                column.HeaderCell.ApplyState();
            }
        }

        private void NotifyingDataSource_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_owner.LoadingOrUnloadingRow)
            {
                throw DataGridError.DataGrid.CannotChangeItemsWhenLoadingRows();
            }
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    Debug.Assert(e.NewItems != null, "Unexpected NotifyCollectionChangedAction.Add notification");
                    if (ShouldAutoGenerateColumns)
                    {
                        // The columns are also affected (not just rows) in this case so we need to reset everything
                        _owner.InitializeElements(false /*recycleRows*/);
                    }
                    else if (!IsGrouping)
                    {
                        // If we're grouping then we handle this through the CollectionViewGroup notifications
                        // According to WPF, Add is a single item operation
                        Debug.Assert(e.NewItems.Count == 1);
                        _owner.InsertRowAt(e.NewStartingIndex);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    IList removedItems = e.OldItems;
                    if (removedItems == null || e.OldStartingIndex < 0)
                    {
                        Debug.Assert(false, "Unexpected NotifyCollectionChangedAction.Remove notification");
                        return;
                    }
                    if (!IsGrouping)
                    {
                        // If we're grouping then we handle this through the CollectionViewGroup notifications
                        // According to WPF, Remove is a single item operation
                        foreach (object item in e.OldItems)
                        {
                            Debug.Assert(item != null);
                            _owner.RemoveRowAt(e.OldStartingIndex, item);
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException(); // 

                case NotifyCollectionChangedAction.Reset:
                    // Did the data type change during the reset?  If not, we can recycle
                    // the existing rows instead of having to clear them all.  We still need to clear our cached
                    // values for DataType and DataProperties, though, because the collection has been reset.
                    Type previousDataType = _dataType;
                    _dataType = null;
                    if (previousDataType != DataType)
                    {
                        ClearDataProperties();
                        _owner.InitializeElements(false /*recycleRows*/);
                    }
                    else
                    {
                        _owner.InitializeElements(!ShouldAutoGenerateColumns /*recycleRows*/);
                    }
                    break;
            }
        }


        private void UpdateDataProperties()
        {
            Type dataType = DataType;

            if (DataSource != null && dataType != null && !DataTypeIsPrimitive(dataType))
            {
                _dataProperties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                Debug.Assert(_dataProperties != null);
            }
            else
            {
                _dataProperties = null;
            }
        }

        #endregion Private Methods
    }

    #region Sorting

    /*public SortDescriptionCollection SortDescriptions
    {
        get
        {
            if (CollectionView != null && CollectionView.CanSort)
            {
                return CollectionView.SortDescriptions;
            }
            else
            {
                return (SortDescriptionCollection)null;
            }
        }
    }*/


    #endregion

    #region Properties


    #endregion
}
