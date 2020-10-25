// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;       // IList<GridViewColumn>
using System.Collections.Specialized;   // NotifyCollectionChangedEventArgs
using System.Collections.ObjectModel;   // Collection, ReadOnlyCollection
using System.Diagnostics;               // Assert

namespace Avalonia.Controls
{
    /// <summary>
    /// Argument for GridViewColumnCollectionChanged event
    /// </summary>
    internal class GridViewColumnCollectionChangedEventArgs : NotifyCollectionChangedEventArgs
    {
        /// <summary>
        /// constructor (for a property of one column changed)
        /// </summary>
        /// <param name="column">column whose property changed</param>
        /// <param name="propertyName">Name of the changed property</param>
        internal GridViewColumnCollectionChangedEventArgs(GridViewColumn column, string propertyName)
            : base(NotifyCollectionChangedAction.Reset) // NotifyCollectionChangedEventArgs doesn't have 0 parameter constructor, so pass in an arbitrary parameter.
        {
            _column = column;
            _propertyName = propertyName;
        }

        /// <summary>
        /// constructor (for clear)
        /// </summary>
        /// <param name="action">must be NotifyCollectionChangedAction.Reset</param>
        /// <param name="clearedColumns">Columns removed in reset action</param>
        internal GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction action, GridViewColumn[] clearedColumns)
            : base(action)
        {
            _clearedColumns = System.Array.AsReadOnly<GridViewColumn>(clearedColumns);
        }

        /// <summary>
        /// Construct for one-column Add/Remove event.
        /// </summary>
        internal GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction action, GridViewColumn changedItem, int index, int actualIndex)
            : base(action, changedItem, index)
        {
            Debug.Assert(action == NotifyCollectionChangedAction.Add || action == NotifyCollectionChangedAction.Remove,
                "This constructor only supports Add/Remove action.");
            Debug.Assert(changedItem != null, "changedItem can't be null");
            Debug.Assert(index >= 0, "index must >= 0");
            Debug.Assert(actualIndex >= 0, "actualIndex must >= 0");

            _actualIndex = actualIndex;
        }

        /// <summary>
        /// Construct for a one-column Replace event.
        /// </summary>
        internal GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction action, GridViewColumn newItem, GridViewColumn oldItem, int index, int actualIndex)
            : base(action, newItem, oldItem, index)
        {
            Debug.Assert(newItem != null, "newItem can't be null");
            Debug.Assert(oldItem != null, "oldItem can't be null");
            Debug.Assert(index >= 0, "index must >= 0");
            Debug.Assert(actualIndex >= 0, "actualIndex must >= 0");

            _actualIndex = actualIndex;
        }

        /// <summary>
        /// Construct for a one-column Move event.
        /// </summary>
        internal GridViewColumnCollectionChangedEventArgs(NotifyCollectionChangedAction action, GridViewColumn changedItem, int index, int oldIndex, int actualIndex)
            : base(action, changedItem, index, oldIndex)
        {
            Debug.Assert(changedItem != null, "changedItem can't be null");
            Debug.Assert(index >= 0, "index must >= 0");
            Debug.Assert(oldIndex >= 0, "oldIndex must >= 0");
            Debug.Assert(actualIndex >= 0, "actualIndex must >= 0");

            _actualIndex = actualIndex;
        }

        /// <summary>
        /// index of the changed column in the internal column list.
        /// </summary>
        internal int ActualIndex
        {
            get { return _actualIndex; }
        }

        private int _actualIndex = -1;

        /// <summary>
        /// Columns removed in reset action.
        /// </summary>
        internal ReadOnlyCollection<GridViewColumn> ClearedColumns
        {
            get { return _clearedColumns; }
        }

        private ReadOnlyCollection<GridViewColumn> _clearedColumns;


        // The following two properties are used to store information of GridViewColumns.PropertyChanged event.
        //
        // GridViewColumnCollection hookup GridViewColumns.PropertyChanged event. When GridViewColumns.PropertyChanged
        // event is raised, GridViewColumnCollection will raised CollectionChanged event with GridViewColumnCollectionChangedEventArgs.
        // In the event arg the following two properties will be set, so GridViewRowPresenter will be informed.
        //
        // GridViewRowPresenter needn't hookup PropertyChanged event of each column, which cost a lot of time in scroll operation.

        /// <summary>
        /// Column whose property changed
        /// </summary>
        internal GridViewColumn Column
        {
            get { return _column; }
        }

        private GridViewColumn _column;

        /// <summary>
        /// Name of the changed property
        /// </summary>
        internal string PropertyName
        {
            get { return _propertyName; }
        }

        private string _propertyName;
    }
}
