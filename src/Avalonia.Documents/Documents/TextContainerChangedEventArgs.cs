// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: The arguments sent when a TextChangedEvent is fired in a TextContainer.
//

//using System;
//using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;

namespace System.Windows.Documents
{
    /// <summary>
    ///  The TextContainerChangedEventArgs defines the event arguments sent when a 
    ///  TextContainer is changed.
    /// </summary>
    internal class TextContainerChangedEventArgs : EventArgs
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TextContainerChangedEventArgs()
        {
            _changes = new SortedList<int, TextChange>();
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Called by TextElement when a local property value changes.
        // Sets the HasLocalPropertyValueChange property true.
        internal void SetLocalPropertyValueChanged()
        {
            _hasLocalPropertyValueChange = true;
        }

        // Adds a new change to the Delta list.
        internal void AddChange(PrecursorTextChangeType textChange, int offset, int length, bool collectTextChanges)
        {
            if (textChange == PrecursorTextChangeType.ContentAdded ||
                textChange == PrecursorTextChangeType.ElementAdded ||
                textChange == PrecursorTextChangeType.ContentRemoved ||
                textChange == PrecursorTextChangeType.ElementExtracted)
            {
                _hasContentAddedOrRemoved = true;
            }

            if (!collectTextChanges)
            {
                return;
            }

            // We only want to add or remove the edges for empty elements (and ElementAdded /
            // ElementRemoved are only used with empty elements).  Note that since we're making
            // two changes instead of one the order matters.  We have to treat the two cases differently.
            if (textChange == PrecursorTextChangeType.ElementAdded)
            {
                AddChangeToList(textChange, offset, 1);
                AddChangeToList(textChange, offset + length - 1, 1);
            }
            else if (textChange == PrecursorTextChangeType.ElementExtracted)
            {
                AddChangeToList(textChange, offset + length - 1, 1);
                AddChangeToList(textChange, offset, 1);
            }
            else if (textChange == PrecursorTextChangeType.PropertyModified)
            {
                // ignore property changes
                return;
            }
            else
            {
                AddChangeToList(textChange, offset, length);
            }
        }

#if PROPERTY_CHANGES
        // We don't merge property changes with each other when they're added, so that when an element is removed
        // we can remove the property changes associated with it.  This method merges all of them at once.
        internal void MergePropertyChanges()
        {
            TextChange leftChange = null;
            for (int index = 0; index < Changes.Count; index++)
            {
                TextChange curChange = Changes.Values[index];
                if (leftChange == null || leftChange.Offset + leftChange.PropertyCount < curChange.Offset)
                {
                    leftChange = curChange;
                }
                else if (leftChange.Offset + leftChange.PropertyCount >= curChange.Offset)
                {
                    if (MergePropertyChange(leftChange, curChange))
                    {
                        // Changes merged.  If the right-hand change is now empty, remove it.
                        if (curChange.RemovedLength == 0 && curChange.AddedLength == 0)
                        {
                            Changes.RemoveAt(index--); // decrement index since we're removing an element, otherwise we'll skip one
                        }
                    }
                }
            }
        }
#endif

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// Returns true if at least one of the TextChangeSegments in this TextChangeCollection
        /// is not of type TextChange.LayoutAffected or TextChange.RenderAffect.  This means that
        /// content was added or removed -- rather than content only affected by AvaloniaProperty
        /// values changes on scoping elements.
        /// </summary>
        /// <value></value>
        internal bool HasContentAddedOrRemoved
        {
            get
            {
                return _hasContentAddedOrRemoved;
            }
        }

        /// <summary>
        /// Returns true if the collection contains one or more entries that
        /// result from a local property value changing on a TextElement.
        /// </summary>
        /// <remarks>
        /// Note "local property value" does NOT include property changes
        /// that arrive via inheritance or styles.
        /// </remarks>
        internal bool HasLocalPropertyValueChange
        {
            get
            {
                return _hasLocalPropertyValueChange;
            }
        }

        /// <summary>
        /// List of TextChanges representing the aggregate changes made to the container.
        /// </summary>
        internal SortedList<int, TextChange> Changes
        {
            get
            {
                return _changes;
            }
        }

        #endregion Internal Properties

        #region Private Methods

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        //
        // First we discover where the new change goes in the sorted list.  If there's already
        // a change there, merge the new one with the old one.
        //
        // Next, if the new change is an insertion or deletion, see if the offset of the change
        // is within a range covered by an insertion at a lower offset.  If so, merge the change at 
        // the higher offset into the previous change and delete the change at the higher offset.
        //
        // Next, if the change is a deletion, see if we delete enough characters to run into a
        // change at a higher offset.  If so, merge the change at the higher offset into this newer
        // change and delete the older, higher-offset change.
        //
        // Finally, update all offsets higher than the current change to reflect the number of
        // characters inserted or deleted.
        //        
        private void AddChangeToList(PrecursorTextChangeType textChange, int offset, int length)
        {
            int offsetDelta = 0; // Value by which to adjust other offsets in list
            int curIndex;        // loop counter
            TextChange change = null; // initialize to satisfy compiler
            bool isDeletion = false;
#if PROPERTY_CHANGES
            bool shiftPropertyChanges = false;
#endif

            //
            // Is there already a change at this offset in our list?  If so, use it
            //
            int keyIndex = Changes.IndexOfKey(offset);
            if (keyIndex != -1)
            {
                change = Changes.Values[keyIndex];
            }
            else
            {
                change = new TextChange();
                change.Offset = offset;
                Changes.Add(offset, change);
                keyIndex = Changes.IndexOfKey(offset);
            }

            //
            // Merge values from new change into empty or pre-existing change
            //
            if (textChange == PrecursorTextChangeType.ContentAdded || textChange == PrecursorTextChangeType.ElementAdded)
            {
                change.AddedLength += length;
                offsetDelta = length;
#if PROPERTY_CHANGES
                if (change.PropertyCount > 0)
                {
                    shiftPropertyChanges = true;
                }
#endif
            }
            else if (textChange == PrecursorTextChangeType.ContentRemoved || textChange == PrecursorTextChangeType.ElementExtracted)
            {
                // Any deletions made after an addition just cancel out the earlier addition
                change.RemovedLength += Math.Max(0, length - change.AddedLength);
                change.AddedLength = Math.Max(0, change.AddedLength - length);

#if PROPERTY_CHANGES
                // If an element was extracted, any property change made on that element should be removed as well
                if (textChange == PrecursorTextChangeType.ElementExtracted)
                {
                    change.SetPropertyCount(0);
                }
                else
                {
                    change.SetPropertyCount(Math.Max(0, change.PropertyCount - length));
                }
#endif

                offsetDelta = -length;
                isDeletion = true;
            }
#if PROPERTY_CHANGES
            else
            {
                // Property change
                if (change.PropertyCount < length)
                {
                    change.SetPropertyCount(length);
                }
            }
#endif

            //
            // Done with simple case.  Now look for changes that intersect.
            // There are two possible (non-exclusive) merge conditions:
            //   1. A positionally preceding change spans this offset (this change is inserting
            //        into or deleting from the middle or right edge of previous inserted text)
            //   2. On deletion, the change spans one or more positionally later changes
            //
            if (keyIndex > 0 && textChange != PrecursorTextChangeType.PropertyModified)
            {
                curIndex = keyIndex - 1;
                // Since we don't merge property changes, there could be an arbitrary number of
                // them between the new change and an overlapping insertion or overlapping property
                // change.  In fact, there could be multiple property changes that overlap this
                // change.  We need to adjust ALL of them.  There can be only one overlapping
                // insertion, but there's no way to leverage that fact.
                TextChange mergedChange = null;
                while (curIndex >= 0)
                {
                    TextChange prevChange = Changes.Values[curIndex];
#if PROPERTY_CHANGES
                    if (prevChange.Offset + prevChange.AddedLength >= offset || prevChange.Offset + prevChange.PropertyCount >= offset)
#else
                    if (prevChange.Offset + prevChange.AddedLength >= offset)
#endif
                    {
                        if (MergeTextChangeLeft(prevChange, change, isDeletion, length))
                        {
                            mergedChange = prevChange;
                        }
                    }
                    curIndex--;
                }
                if (mergedChange != null)
                {
                    // the change got merged.  Use the merged change as the basis for righthand merging.
                    change = mergedChange;
                }
                // changes may have been deleted, so update the index of the change we're working with
                keyIndex = Changes.IndexOfKey(change.Offset);
            }

            curIndex = keyIndex + 1;
            if (isDeletion && curIndex < Changes.Count)
            {
                while (curIndex < Changes.Count && Changes.Values[curIndex].Offset <= offset + length)
                {
                    // offset and length must be passed because if we've also merged left, we haven't yet adjusted indices.
                    // Note that MergeTextChangeRight() always removes Changes.Values[curIndex] from the list, so
                    // we don't need to increment curIndex.
                    MergeTextChangeRight(Changes.Values[curIndex], change, offset, length);
                }
                // changes may have been deleted, so update the index of the change we're working with
                keyIndex = Changes.IndexOfKey(change.Offset);
            }

            // Update all changes to reflect new offsets.
            // If offsetDelta is positive, go right to left; otherwise, go left to right.
            // The order of the offsets will never change, so we can use an indexer safely.
            // To avoid nasty N-squared perf, create a new list instead of moving things within
            // the old one.
            // Change the implementation of Changes to a more efficient structure.
            if (offsetDelta != 0)
            {
                SortedList<int, TextChange> newChanges = new SortedList<int, TextChange>(Changes.Count);
                for (curIndex = 0; curIndex < Changes.Count; curIndex++)
                {
                    TextChange curChange = Changes.Values[curIndex];
                    if (curIndex > keyIndex)
                    {
                        curChange.Offset += offsetDelta;
                    }
                    newChanges.Add(curChange.Offset, curChange);
                }
                _changes = newChanges;
            }
            
            DeleteChangeIfEmpty(change);

#if PROPERTY_CHANGES
            // Finally, if the change was an insertion and there are property changes starting
            // at this location, the insertion is not part of the property changes.  Shift the
            // property changes forward by the length of the insertion.
            if (shiftPropertyChanges)
            {
                int propertyCount = change.PropertyCount;
                change.SetPropertyCount(0);
                AddChangeToList(PrecursorTextChangeType.PropertyModified, offset + offsetDelta, propertyCount);
            }
#endif
        }

        private void DeleteChangeIfEmpty(TextChange change)
        {
#if PROPERTY_CHANGES
            if (change.AddedLength == 0 && change.RemovedLength == 0 && change.PropertyCount == 0)
#else
            if (change.AddedLength == 0 && change.RemovedLength == 0)
#endif
            {
                Changes.Remove(change.Offset);
            }
        }

        // returns true if the change merged into an earlier insertion
        private bool MergeTextChangeLeft(TextChange oldChange, TextChange newChange, bool isDeletion, int length)
        {
            // newChange is inserting or deleting text inside oldChange.
            int overlap;

#if PROPERTY_CHANGES
            // Check for a property change in the old change that overlaps the new change
            if (oldChange.Offset + oldChange.PropertyCount >= newChange.Offset)
            {
                if (isDeletion)
                {
                    overlap = oldChange.PropertyCount - (newChange.Offset - oldChange.Offset);
                    oldChange.SetPropertyCount(oldChange.PropertyCount - Math.Min(overlap, length));
                    DeleteChangeIfEmpty(oldChange);
                }
                else
                {
                    oldChange.SetPropertyCount(oldChange.PropertyCount + length);
                }
            }
#endif
            
            if (oldChange.Offset + oldChange.AddedLength >= newChange.Offset)
            {
                // If any text was deleted in the new change, adjust the added count of the
                // previous change accordingly.  The removed count of the new change must be
                // adjusted by the same amount.
                if (isDeletion)
                {
                    overlap = oldChange.AddedLength - (newChange.Offset - oldChange.Offset);
                    int cancelledCount = Math.Min(overlap, newChange.RemovedLength);
                    oldChange.AddedLength -= cancelledCount;
                    oldChange.RemovedLength += (length - cancelledCount);
                }
                else
                {
                    oldChange.AddedLength += length;
                }
#if PROPERTY_CHANGES
                if (newChange.PropertyCount == 0)
                {
                    // We've merged the data from the new change into an older one, so we can
                    // delete the change from the list.
                    Changes.Remove(newChange.Offset);
                }
                else
                {
                    // Can't delete the change, since there's pre-existing property change data, so
                    // just clear the data instead.
                    newChange.AddedLength = 0;
                    newChange.RemovedLength = 0;
                }
#else
                // We've merged the data from the new change into an older one, so we can
                // delete the change from the list.
                Changes.Remove(newChange.Offset);
#endif
                return true;
            }
            return false;
        }

        private void MergeTextChangeRight(TextChange oldChange, TextChange newChange, int offset, int length)
        {
            // If the old change is an addition, find the length of the overlap
            // and adjust the addition and new deletion counts accordingly
            int addedLengthOverlap = oldChange.AddedLength > 0 ? offset + length - oldChange.Offset : 0;

#if PROPERTY_CHANGES
            // Check for a property change in the new change that overlaps the old change
            int propertyOverlap = newChange.Offset + newChange.PropertyCount - oldChange.Offset;
            if (propertyOverlap > 0)
            {
                int delta = Math.Min(propertyOverlap, addedLengthOverlap);
                newChange.SetPropertyCount(newChange.PropertyCount - delta);
                // Don't need to adjust oldChange.PropertyCount, since oldChange is about to be removed
            }
#endif
            
            // adjust removed count
            if (addedLengthOverlap >= oldChange.AddedLength)
            {
                // old change is entirely within new one
                newChange.RemovedLength += (oldChange.RemovedLength - oldChange.AddedLength);
                Changes.Remove(oldChange.Offset);
            }
            else
            {
                newChange.RemovedLength += (oldChange.RemovedLength - addedLengthOverlap);
                newChange.AddedLength += (oldChange.AddedLength - addedLengthOverlap);
                Changes.Remove(oldChange.Offset);
            }
        }

#if PROPERTY_CHANGES
        private bool MergePropertyChange(TextChange leftChange, TextChange rightChange)
        {
            if (leftChange.Offset + leftChange.PropertyCount >= rightChange.Offset)
            {
                if (leftChange.Offset + leftChange.PropertyCount < rightChange.Offset + rightChange.PropertyCount)
                {
                    // right change is partially inside left, but not completely.
                    int overlap = leftChange.Offset + leftChange.PropertyCount - rightChange.Offset;
                    int delta = rightChange.PropertyCount - overlap;
                    leftChange.SetPropertyCount(leftChange.PropertyCount + delta);
                }
                rightChange.SetPropertyCount(0);
                return true;
            }
            return false;
        }
#endif

        #endregion Private Methods
        
        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        // true if at least one of the changes in this TextChangeCollection
        // is not of type TextChange.LayoutAffected or TextChange.RenderAffect.
        private bool _hasContentAddedOrRemoved;

        // True if the collection contains one or more entries that
        // result from a local property value changing on a TextElement.
        private bool _hasLocalPropertyValueChange;

        private SortedList<int, TextChange> _changes;

        #endregion Private Fields
    }
}
