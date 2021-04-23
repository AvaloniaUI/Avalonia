// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 
// Description: Static methods for all TextElementCollection<T> instances.
//

using Avalonia;

namespace System.Windows.Documents
{
    // This class maintains a static list of "clean" TextElementCollection
    // instances.  Clean in this sense means guarantee not to have been 
    // modified in a way that could invalidate their _indexCache members.
    //
    // The cache is a simple list of WeakReferences to collections.
    // The size of the list limits the number of collections
    // that can be used to modify content simultaneously with good IList
    // performance.  The scenario we're concerned about is the parser,
    // which will allocate one collection for each scoping TextElement
    // in a document: <Paragraph><Italic><Run>hello world</Run></Italic></Paragraph>
    // requires a minimum cache size of 3 to keep load times O(n).
    internal static class TextElementCollectionHelper
    {
        //-------------------------------------------------------------------
        //
        //  Internal Methods
        //
        //-------------------------------------------------------------------

        #region Internal Methods

        // Invalidates any collection tracking parent's children.
        // Called by the TextContainer.
        internal static void MarkDirty(IAvaloniaObject parent)
        {
            if (parent == null)
            {
                return;
            }

            lock (_cleanParentList)
            {
                for (int i = 0; i < _cleanParentList.Length; i++)
                {
                    if (_cleanParentList[i] != null)
                    {
                        ParentCollectionPair pair = (ParentCollectionPair)_cleanParentList[i].Target;

                        if (pair == null || pair.Parent == parent)
                        {
                            _cleanParentList[i] = null;
                        }
                    }
                }
            }
        }

        // Tags a parent/collection as synchronized.
        // Since we use a most-recently-used algorithm, it's useful
        // to call this any time a collection is touched, even if it is
        // only read.
        internal static void MarkClean(IAvaloniaObject parent, object collection)
        {
            lock (_cleanParentList)
            {
                int firstFreeIndex;
                int index = GetCleanParentIndex(parent, collection, out firstFreeIndex);

                if (index == -1)
                {
                    index = firstFreeIndex >= 0 ? firstFreeIndex : _cleanParentList.Length - 1;
                    _cleanParentList[index] = new WeakReference(new ParentCollectionPair(parent, collection));
                }

                TouchCleanParent(index);
            }
        }

        // Returns true if the parent/collection pair are clean.
        // Since we use a most-recently-used algorithm, it's useful
        // to call this any time a collection is touched, even if it is
        // only read.
        internal static bool IsCleanParent(IAvaloniaObject parent, object collection)
        {
            int index = -1;

            lock (_cleanParentList)
            {
                int firstFreeIndex;
                index = GetCleanParentIndex(parent, collection, out firstFreeIndex);

                if (index >= 0)
                {
                    TouchCleanParent(index);
                }
            }

            return (index >= 0);
        }

        #endregion Internal Methods

        //-------------------------------------------------------------------
        //
        //  Private Methods
        //
        //-------------------------------------------------------------------

        #region Private Methods

        // Moves an entry to the top of the most-recently-used list.
        // Caller must hold a lock on _cleanParentList!
        private static void TouchCleanParent(int index)
        {
            WeakReference parentReference = _cleanParentList[index];
            // Shift preceding parents down, dropping the last parent.
            Array.Copy(_cleanParentList, 0, _cleanParentList, 1, index);
            // Put the mru parent at the head.
            _cleanParentList[0] = parentReference;
        }

        // Returns the index of an entry in the list, or -1 if not present.
        // Caller must hold a lock on _cleanParentList!
        private static int GetCleanParentIndex(IAvaloniaObject parent, object collection, out int firstFreeIndex)
        {
            int index = -1;
         
            firstFreeIndex = -1;

            for (int i = 0; i < _cleanParentList.Length; i++)
            {
                if (_cleanParentList[i] == null)
                {
                    if (firstFreeIndex == -1)
                    {
                        firstFreeIndex = i;
                    }
                }
                else
                {
                    ParentCollectionPair pair = (ParentCollectionPair)_cleanParentList[i].Target;

                    if (pair == null)
                    {
                        // WeakReference is dead, remove it.
                        _cleanParentList[i] = null;
                        if (firstFreeIndex == -1)
                        {
                            firstFreeIndex = i;
                        }
                    }
                    else if (pair.Parent == parent && pair.Collection == collection)
                    {
                        // Found a match.  Keep going to clean up any dead WeakReferences
                        // or set firstFreeIndex.
                        index = i;
                    }
                }
            }

            return index;
        }

        #endregion Private Methods

        //-------------------------------------------------------------------
        //
        //  Private Types
        //
        //-------------------------------------------------------------------

        #region Private Types

        // A parent/collection entry.
        private class ParentCollectionPair
        {
            internal ParentCollectionPair(IAvaloniaObject parent, object collection)
            {
                _parent = parent;
                _collection = collection;
            }

            internal IAvaloniaObject Parent { get { return _parent; } }

            internal object Collection { get { return _collection; } }

            private readonly IAvaloniaObject _parent;

            private readonly object _collection;
        }

        #endregion Private Types

        //-------------------------------------------------------------------
        //
        //  Private Fields
        //
        //-------------------------------------------------------------------

        #region Private Fields

        // Static list of clean parent/collection pairs.
        private static readonly WeakReference []_cleanParentList = new WeakReference[10];

        #endregion Private Fields
    }
}
