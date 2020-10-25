// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Collections.Generic;       // List<T>
using System.Collections.ObjectModel;   // ObservableCollection<T>
using System.Collections.Specialized;   // NotifyCollectionChangedEventHandler
using System.ComponentModel;            // DesignerSerializationVisibility
using System.Diagnostics;               // Debug
using Avalonia.Data;                    // Binding.IndexerName

//using MS.Internal;                      // Helper


namespace Avalonia.Controls
{
    /// <summary>
    /// a List of GridViewColumn. Cooperating with GridViewRowPresenter, can insert and remove column.efficiently.
    /// </summary>

    public class GridViewColumnCollection : ObservableCollection<GridViewColumn>
    {
        //-------------------------------------------------------------------
        //
        //  Constructors
        //
        //-------------------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Methods / Event
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// called by base class ObservableCollection&lt;T&gt; when the list is being cleared;
        /// GridViewColumnCollection override this method to do some internal preprocess work
        /// </summary>
        protected override void ClearItems()
        {
            VerifyAccess();
            _internalEventArg = ClearPreprocess();
            base.ClearItems();
        }

        /// <summary>
        /// called by base class ObservableCollection&lt;T&gt; when an column is removed from list;
        /// GridViewColumnCollection override this method to do some internal preprocess work
        /// </summary>
        protected override void RemoveItem(int index)
        {
            VerifyAccess();
            _internalEventArg = RemoveAtPreprocess(index);
            base.RemoveItem(index);
        }

        /// <summary>
        /// called by base class ObservableCollection&lt;T&gt; when an item is added to list;
        /// GridViewColumnCollection override this method to do some internal preprocess work
        /// </summary>
        protected override void InsertItem(int index, GridViewColumn column)
        {
            VerifyAccess();
            _internalEventArg = InsertPreprocess(index, column);
            base.InsertItem(index, column);
        }

        /// <summary>
        /// called by base class ObservableCollection&lt;T&gt; when an column is set in list;
        /// GridViewColumnCollection override this method to do some internal preprocess work
        /// </summary>
        protected override void SetItem(int index, GridViewColumn column)
        {
            VerifyAccess();
            _internalEventArg = SetPreprocess(index, column);
            if (_internalEventArg != null) // the new column is equals to the old one. 
            {
                base.SetItem(index, column);
            }
        }

        /// <summary>
        /// Move column to a different index
        /// </summary>
        /// <param name="oldIndex">index of the column which is being moved</param>
        /// <param name="newIndex">index of the column to be move to</param>
        protected override void MoveItem(int oldIndex, int newIndex)
        {
            if (oldIndex != newIndex)
            {
                VerifyAccess();
                _internalEventArg = MovePreprocess(oldIndex, newIndex);
                base.MoveItem(oldIndex, newIndex);
            }
        }

        // Override OnCollectionChanged method to ensure InternalCollectionChanged event is raised before public one.
        #region OnCollectionChanged

        /// <summary>
        /// raise CollectionChanged event to any listeners
        /// </summary>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            OnInternalCollectionChanged();
            base.OnCollectionChanged(e);
        }

        #endregion OnCollectionChanged

        #endregion

        //------------------------------------------------------
        //
        //  Internal Methods / Properties
        //
        //------------------------------------------------------

        #region Internal Methods / Properties

        internal event NotifyCollectionChangedEventHandler InternalCollectionChanged
        {
            add { _internalCollectionChanged += value; }
            remove { _internalCollectionChanged -= value; }
        }

        /// <summary>
        /// Make the collection not changabel. 
        /// Should call UnblockWrite the same times as BlockWrite to make collection writable.
        /// </summary>
        internal void BlockWrite()
        {
            Debug.Assert(IsImmutable != true, "IsImmutable is true before BlockWrite");
            IsImmutable = true;
        }

        /// <summary>
        /// Counterfact the effect of BlockWrite() - restore collection to a changable state.
        /// </summary>
        internal void UnblockWrite()
        {
            Debug.Assert(IsImmutable != false, "IsImmutable is flase before UnblockWrite");
            IsImmutable = false;
        }

        // Column list. Columns in this list are organized in the order 
        // that they were inserted into this collection. So Move operation 
        // won't change this list.
        internal List<GridViewColumn> ColumnCollection { get { return _columns; } }

        // Actual index list of GridViewColumn in ColumnCollection
        // this[i] == ColumnCollection[IndexList[i]]
        internal List<int> IndexList { get { return _actualIndices; } }

        #endregion

        #region InheritanceContext

        // The GridView that we're in
        internal AvaloniaObject Owner
        {
            get { return _owner; }
            set
            {
                if (value != _owner)
                {
                    /*if (value == null)
                    {
                        foreach (GridViewColumn c in _columns)
                        {
                            InheritanceContextHelper.RemoveContextFromObject(_owner, c);
                        }
                    }
                    else
                    {
                        foreach (GridViewColumn c in _columns)
                        {
                            InheritanceContextHelper.ProvideContextForObject(value, c);
                        }
                    }*/

                    _owner = value;
                }
            }
        }

        // The GridView that we're in
        [NonSerializedAttribute]
        private AvaloniaObject _owner = null;

        // the Owner is GridView
        internal bool InViewMode
        {
            get { return _inViewMode; }
            set { _inViewMode = value; }
        }

        private bool _inViewMode;

        #endregion InheritanceContext

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void OnInternalCollectionChanged()
        {
            if (_internalCollectionChanged != null && _internalEventArg != null)
            {
                _internalCollectionChanged(this, _internalEventArg);
                // This class member is used as parameter, so clear it after used.
                // For details, see definition.
                _internalEventArg = null;
            }
        }

        private void ColumnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            GridViewColumn column = sender as GridViewColumn;

            if (_internalCollectionChanged != null && column != null)
            {
                _internalCollectionChanged(this, new GridViewColumnCollectionChangedEventArgs(column, e.PropertyName));
            }
        }

        private GridViewColumnCollectionChangedEventArgs MovePreprocess(int oldIndex, int newIndex)
        {
            Debug.Assert(oldIndex != newIndex, "oldIndex==newIndex when perform move action.");

            VerifyIndexInRange(oldIndex, "oldIndex");
            VerifyIndexInRange(newIndex, "newIndex");

            int actualIndex = _actualIndices[oldIndex];

            if (oldIndex < newIndex)
            {
                for (int targetIndex = oldIndex; targetIndex < newIndex; targetIndex++)
                {
                    _actualIndices[targetIndex] = _actualIndices[targetIndex + 1];
                }
            }
            else //if (oldIndex > newIndex)
            {
                for (int targetIndex = oldIndex; targetIndex > newIndex; targetIndex--)
                {
                    _actualIndices[targetIndex] = _actualIndices[targetIndex - 1];
                }
            }

            _actualIndices[newIndex] = actualIndex;

            return new GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, _columns[actualIndex], newIndex, oldIndex, actualIndex);
        }

        private GridViewColumnCollectionChangedEventArgs ClearPreprocess()
        {
            GridViewColumn[] list = new GridViewColumn[Count];
            if (Count > 0)
            {
                CopyTo(list, 0);
            }

            // reset columns *before* remove
            foreach (GridViewColumn c in _columns)
            {
                c.ResetPrivateData();
                ((INotifyPropertyChanged)c).PropertyChanged -= new PropertyChangedEventHandler(ColumnPropertyChanged);

                ///InheritanceContextHelper.RemoveContextFromObject(_owner, c);
            }

            _columns.Clear();
            _actualIndices.Clear();

            return new GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, list);
        }

        private GridViewColumnCollectionChangedEventArgs RemoveAtPreprocess(int index)
        {
            VerifyIndexInRange(index, "index");

            int actualIndex = _actualIndices[index];
            GridViewColumn column = _columns[actualIndex];

            column.ResetPrivateData();
            ((INotifyPropertyChanged)column).PropertyChanged -= new PropertyChangedEventHandler(ColumnPropertyChanged);

            _columns.RemoveAt(actualIndex);

            UpdateIndexList(actualIndex, index);

            UpdateActualIndexInColumn(actualIndex);

            ///InheritanceContextHelper.RemoveContextFromObject(_owner, column);

            return new GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, column, index, actualIndex);
        }

        // Remove the index of the removed column in _actualIndices
        // Correct index in _actualIndices which is bigger than actualIndex
        private void UpdateIndexList(int actualIndex, int index)
        {
            for (int sourceIndex = 0; sourceIndex < index; sourceIndex++)
            {
                int i = _actualIndices[sourceIndex];
                if (i > actualIndex)
                {
                    _actualIndices[sourceIndex] = i - 1;
                }
            }

            for (int sourceIndex = index + 1; sourceIndex < _actualIndices.Count; sourceIndex++)
            {
                int i = _actualIndices[sourceIndex];
                if (i < actualIndex)
                {
                    _actualIndices[sourceIndex - 1] = i;
                }
                else if (i > actualIndex)
                {
                    _actualIndices[sourceIndex - 1] = i - 1;
                }
            }

            _actualIndices.RemoveAt(_actualIndices.Count - 1);
        }

        // pack the actual indeices in columns from after the removed one
        private void UpdateActualIndexInColumn(int iStart)
        {
            for (int i = iStart; i < _columns.Count; i++)
            {
                _columns[i].ActualIndex = i;
            }
        }

        private GridViewColumnCollectionChangedEventArgs InsertPreprocess(int index, GridViewColumn column)
        {
            int count = _columns.Count;
            if (index < 0 || index > count)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            ValidateColumnForInsert(column);

            _columns.Add(column);
            column.ActualIndex = count;

            _actualIndices.Insert(index, count);

            //
            // NOTE: 
            //
            //  To prevent a useless (if not error prone) event from being fired, we need to call 
            //  {ProvideContextForObject()} before {column.PropertyChanged += ...}.
            //
            //  Below is the case when a column property change event can be fired before the event
            //  that this column was added into the collection. 
            // 
            //  1. add a new column to the ColumnCollection, e.g.
            //
            //         <GridViewColumn CellTemplate="{DynamicResource ...}" .../>
            //
            //  2. once column is connected to the collection, DynamicResource will start loading
            //      the template
            //
            //  3. #1 will trigger a collection change event which will be fired when this method 
            //      is accomplished.
            //
            //  4. #2 will trigger a property change event which will be fired at the middle of this 
            //      method.
            //
            //  Ultimately, RowPresenter will receive both #3 and #4. But from above, #4 will come
            //      before #3. Therefore #4 is totally useless because #3 is unknown yet.
            //

            ///InheritanceContextHelper.ProvideContextForObject(_owner, column);

            ((INotifyPropertyChanged)column).PropertyChanged += new PropertyChangedEventHandler(ColumnPropertyChanged);

            return new GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, column, index, count /* actual index*/);
        }

        // This[index] = newColumn
        private GridViewColumnCollectionChangedEventArgs SetPreprocess(int index, GridViewColumn newColumn)
        {
            VerifyIndexInRange(index, "index");

            GridViewColumn oldColumn = this[index];

            if (oldColumn != newColumn)
            {
                int oldColumnActualIndex = _actualIndices[index];

                RemoveAtPreprocess(index);
                InsertPreprocess(index, newColumn);

                // NOTE: Insert Preprocess already updated InheritanceContext. 
                // Don't update here again.

                return new GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newColumn, oldColumn, index, oldColumnActualIndex);
            }

            return null;
        }

        private void VerifyIndexInRange(int index, string indexName)
        {
            Contract.Requires<ArgumentOutOfRangeException>(index > 0 && index < _actualIndices.Count);
        }

        // Throw if column is null or already existed in a GVCC
        private void ValidateColumnForInsert(GridViewColumn column)
        {
            Contract.Requires<ArgumentNullException>(column != null);
            Contract.Requires<InvalidOperationException>(column.ActualIndex < 0);
        }

        private void VerifyAccess()
        {
            Contract.Requires<InvalidOperationException>(!IsImmutable);

            // Although CheckReentrancy() is called in base class, we still need to call it here again,
            // otherwise, when Reentrancy is found and exception is thrown, our operation is done and can't be undo.
            CheckReentrancy();
        }

        #endregion

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // internal storage of CollumnCollection
        private List<GridViewColumn> _columns = new List<GridViewColumn>();

        // index of column in _columns
        // GridViewColumn this[i] in public interface is _columns[_actualIndices[i]]
        private List<int> _actualIndices = new List<int>();

        // GridViewHeaderRowPresenter will set this field to true once reorder is started.
        private bool IsImmutable
        {
            get { return _isImmutable; }
            set { _isImmutable = value; }
        }

        private bool _isImmutable;

        private event NotifyCollectionChangedEventHandler _internalCollectionChanged;

        // EventArgs for _internalCollectionChanged event.
        // We should raise internal event just before ColletonChanged event of base class,
        // but can't pass this to OnCollectionChanged method as parameter. So store it as a class member.
        [NonSerialized]
        private GridViewColumnCollectionChangedEventArgs _internalEventArg;

        #endregion
    }
}
