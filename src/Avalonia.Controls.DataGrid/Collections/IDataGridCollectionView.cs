// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;

namespace Avalonia.Collections
{
    /// <summary>Provides data for the <see cref="E:Avalonia.Collections.ICollectionView.CurrentChanging" /> event.</summary>
    public class DataGridCurrentChangingEventArgs : EventArgs
    {
        private bool _cancel;
        private bool _isCancelable;

        /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.CurrentChangingEventArgs" /> class and sets the <see cref="P:System.ComponentModel.CurrentChangingEventArgs.IsCancelable" /> property to true.</summary>
        public DataGridCurrentChangingEventArgs()
        {
            Initialize(true);
        }

        /// <summary>Initializes a new instance of the <see cref="T:System.ComponentModel.CurrentChangingEventArgs" /> class and sets the <see cref="P:System.ComponentModel.CurrentChangingEventArgs.IsCancelable" /> property to the specified value.</summary>
        /// <param name="isCancelable">true to disable the ability to cancel a <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> change; false to enable cancellation.</param>
        public DataGridCurrentChangingEventArgs(bool isCancelable)
        {
            Initialize(isCancelable);
        }

        private void Initialize(bool isCancelable)
        {
            _isCancelable = isCancelable;
        }

        /// <summary>Gets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> change can be canceled. </summary>
        /// <returns>true if the event can be canceled; false if the event cannot be canceled.</returns>
        public bool IsCancelable
        {
            get
            {
                return _isCancelable;
            }
        }

        /// <summary>Gets or sets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> change should be canceled. </summary>
        /// <returns>true if the event should be canceled; otherwise, false. The default is false.</returns>
        /// <exception cref="T:System.InvalidOperationException">The <see cref="P:System.ComponentModel.CurrentChangingEventArgs.IsCancelable" /> property value is false.</exception>
        public bool Cancel
        {
            get
            {
                return _cancel;
            }
            set
            {
                if (IsCancelable)
                    _cancel = value;
                else if (value)
                    throw new InvalidOperationException("CurrentChanging Cannot Be Canceled");
            }
        }
    }

    /// <summary>Enables collections to have the functionalities of current record management, custom sorting, filtering, and grouping.</summary>
    internal interface IDataGridCollectionView : IEnumerable, INotifyCollectionChanged
    {
        /// <summary>Gets or sets the cultural information for any operations of the view that may differ by culture, such as sorting.</summary>
        /// <returns>The culture information to use during culture-sensitive operations. </returns>
        CultureInfo Culture { get; set; }

        /// <summary>Indicates whether the specified item belongs to this collection view. </summary>
        /// <returns>true if the item belongs to this collection view; otherwise, false.</returns>
        /// <param name="item">The object to check. </param>
        bool Contains(object item);

        /// <summary>Gets the underlying collection.</summary>
        /// <returns>The underlying collection.</returns>
        IEnumerable SourceCollection { get; }

        /// <summary>Gets or sets a callback that is used to determine whether an item is appropriate for inclusion in the view. </summary>
        /// <returns>A method that is used to determine whether an item is appropriate for inclusion in the view.</returns>
        Func<object, bool> Filter { get; set; }

        /// <summary>Gets a value that indicates whether this view supports filtering by way of the <see cref="P:System.ComponentModel.ICollectionView.Filter" /> property.</summary>
        /// <returns>true if this view supports filtering; otherwise, false.</returns>
        bool CanFilter { get; }

        /// <summary>Gets a collection of <see cref="T:System.ComponentModel.SortDescription" /> instances that describe how the items in the collection are sorted in the view.</summary>
        /// <returns>A collection of values that describe how the items in the collection are sorted in the view.</returns>
        DataGridSortDescriptionCollection SortDescriptions { get; }

        /// <summary>Gets a value that indicates whether this view supports sorting by way of the <see cref="P:System.ComponentModel.ICollectionView.SortDescriptions" /> property.</summary>
        /// <returns>true if this view supports sorting; otherwise, false.</returns>
        bool CanSort { get; }

        /// <summary>Gets a value that indicates whether this view supports grouping by way of the <see cref="P:System.ComponentModel.ICollectionView.GroupDescriptions" /> property.</summary>
        /// <returns>true if this view supports grouping; otherwise, false.</returns>
        bool CanGroup { get; }

        /// <summary>Gets a collection of <see cref="T:System.ComponentModel.GroupDescription" /> objects that describe how the items in the collection are grouped in the view. </summary>
        /// <returns>A collection of objects that describe how the items in the collection are grouped in the view. </returns>
        //ObservableCollection<GroupDescription> GroupDescriptions { get; }

        bool IsGrouping { get; }
        int GroupingDepth { get; }
        string GetGroupingPropertyNameAtDepth(int level);

        /// <summary>Gets the top-level groups.</summary>
        /// <returns>A read-only collection of the top-level groups or null if there are no groups.</returns>
        IAvaloniaReadOnlyList<object> Groups { get; }

        /// <summary>Gets a value that indicates whether the view is empty.</summary>
        /// <returns>true if the view is empty; otherwise, false.</returns>
        bool IsEmpty { get; }

        /// <summary>Recreates the view.</summary>
        void Refresh();

        /// <summary>Enters a defer cycle that you can use to merge changes to the view and delay automatic refresh. </summary>
        /// <returns>The typical usage is to create a using scope with an implementation of this method and then include multiple view-changing calls within the scope. The implementation should delay automatic refresh until after the using scope exits. </returns>
        IDisposable DeferRefresh();

        /// <summary>Gets the current item in the view.</summary>
        /// <returns>The current item in the view or null if there is no current item.</returns>
        object CurrentItem { get; }

        /// <summary>Gets the ordinal position of the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view.</summary>
        /// <returns>The ordinal position of the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view.</returns>
        int CurrentPosition { get; }

        /// <summary>Gets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the end of the collection.</summary>
        /// <returns>true if the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the end of the collection; otherwise, false.</returns>
        bool IsCurrentAfterLast { get; }

        /// <summary>Gets a value that indicates whether the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the start of the collection.</summary>
        /// <returns>true if the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> of the view is beyond the start of the collection; otherwise, false.</returns>
        bool IsCurrentBeforeFirst { get; }

        /// <summary>Sets the first item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
        /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
        bool MoveCurrentToFirst();

        /// <summary>Sets the last item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
        /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
        bool MoveCurrentToLast();

        /// <summary>Sets the item after the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
        /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
        bool MoveCurrentToNext();

        /// <summary>Sets the item before the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view to the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
        /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
        bool MoveCurrentToPrevious();

        /// <summary>Sets the specified item in the view as the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" />.</summary>
        /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
        /// <param name="item">The item to set as the current item.</param>
        bool MoveCurrentTo(object item);

        /// <summary>Sets the item at the specified index to be the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> in the view.</summary>
        /// <returns>true if the resulting <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> is an item in the view; otherwise, false.</returns>
        /// <param name="position">The index to set the <see cref="P:System.ComponentModel.ICollectionView.CurrentItem" /> to.</param>
        bool MoveCurrentToPosition(int position);

        /// <summary>Occurs before the current item changes.</summary>
        event EventHandler<DataGridCurrentChangingEventArgs> CurrentChanging;

        /// <summary>Occurs after the current item has been changed.</summary>
        event EventHandler CurrentChanged;
    }
    internal interface IDataGridEditableCollectionView
    {
        /// <summary>Gets a value that indicates whether a new item can be added to the collection.</summary>
        /// <returns>true if a new item can be added to the collection; otherwise, false.</returns>
        bool CanAddNew { get; }

        /// <summary>Adds a new item to the underlying collection.</summary>
        /// <returns>The new item that is added to the collection.</returns>
        object AddNew();

        /// <summary>Ends the add transaction and saves the pending new item.</summary>
        void CommitNew();

        /// <summary>Ends the add transaction and discards the pending new item.</summary>
        void CancelNew();

        /// <summary>Gets a value that indicates whether an add transaction is in progress.</summary>
        /// <returns>true if an add transaction is in progress; otherwise, false.</returns>
        bool IsAddingNew { get; }

        /// <summary>Gets the item that is being added during the current add transaction.</summary>
        /// <returns>The item that is being added if <see cref="P:System.ComponentModel.IEditableCollectionView.IsAddingNew" /> is true; otherwise, null.</returns>
        object CurrentAddItem { get; }

        /// <summary>Gets a value that indicates whether an item can be removed from the collection.</summary>
        /// <returns>true if an item can be removed from the collection; otherwise, false.</returns>
        bool CanRemove { get; }

        /// <summary>Removes the item at the specified position from the collection.</summary>
        /// <param name="index">Index of item to remove.</param>
        void RemoveAt(int index);

        /// <summary>Removes the specified item from the collection.</summary>
        /// <param name="item">The item to remove.</param>
        void Remove(object item);

        /// <summary>Begins an edit transaction on the specified item.</summary>
        /// <param name="item">The item to edit.</param>
        void EditItem(object item);

        /// <summary>Ends the edit transaction and saves the pending changes.</summary>
        void CommitEdit();

        /// <summary>Ends the edit transaction and, if possible, restores the original value of the item.</summary>
        void CancelEdit();

        /// <summary>Gets a value that indicates whether the collection view can discard pending changes and restore the original values of an edited object.</summary>
        /// <returns>true if the collection view can discard pending changes and restore the original values of an edited object; otherwise, false.</returns>
        bool CanCancelEdit { get; }

        /// <summary>Gets a value that indicates whether an edit transaction is in progress.</summary>
        /// <returns>true if an edit transaction is in progress; otherwise, false.</returns>
        bool IsEditingItem { get; }

        /// <summary>Gets the item in the collection that is being edited.</summary>
        /// <returns>The item that is being edited if <see cref="P:System.ComponentModel.IEditableCollectionView.IsEditingItem" /> is true; otherwise, null.</returns>
        object CurrentEditItem { get; }
    }
}
