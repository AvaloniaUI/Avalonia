// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Helper class for TextContainer, handles all undo operations.
//

using Avalonia;
using Avalonia.Data;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    // This static class is logically an extension of TextContainer.  It contains
    // TextContainer undo related helpers.
    internal static class TextTreeUndo
    {
        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Adds a TextTreeInsertUndoUnit to the open parent undo unit, if any.
        // Called from TextContainer.InsertText and TextContainer.InsertEmbeddedObject.
        internal static void CreateInsertUndoUnit(TextContainer tree, int symbolOffset, int symbolCount)
        {
            UndoManager undoManager;

            undoManager = GetOrClearUndoManager(tree);
            if (undoManager == null)
                return;

            undoManager.Add(new TextTreeInsertUndoUnit(tree, symbolOffset, symbolCount));
        }

        // Adds a TextTreeInsertElementUndoUnit to the open parent undo unit, if any.
        // Called from TextContainer.InsertElement.
        internal static void CreateInsertElementUndoUnit(TextContainer tree, int symbolOffset, bool deep)
        {
            UndoManager undoManager;

            undoManager = GetOrClearUndoManager(tree);
            if (undoManager == null)
                return;

            undoManager.Add(new TextTreeInsertElementUndoUnit(tree, symbolOffset, deep));
        }

        // Adds a TextTreePropertyUndoUnit to the open parent undo unit, if any.
        // Called by TextElement's property change listener.
        //internal static void CreatePropertyUndoUnit(TextElement element, AvaloniaPropertyChangedEventArgs e)
        //{
        //    UndoManager undoManager;
        //    PropertyRecord record;
        //    TextContainer textContainer = element.TextContainer;

        //    undoManager = GetOrClearUndoManager(textContainer);
        //    if (undoManager == null)
        //        return;

        //    record = new PropertyRecord();
        //    record.Property = e.Property;
        //    record.Value = e.Priority == BindingPriority.LocalValue ? e.OldValue : AvaloniaProperty.UnsetValue;

        //    undoManager.Add(new TextTreePropertyUndoUnit(textContainer, element.TextElementNode.GetSymbolOffset(textContainer.Generation) + 1, record));
        //}

        // Adds a DeleteContentUndoUnit to the open parent undo unit, if any.
        // Called by TextContainer.DeleteContent.
        internal static TextTreeDeleteContentUndoUnit CreateDeleteContentUndoUnit(TextContainer tree, TextPointer start, TextPointer end)
        {
            UndoManager undoManager;
            TextTreeDeleteContentUndoUnit undoUnit;

            if (start.CompareTo(end) == 0)
                return null;

            undoManager = GetOrClearUndoManager(tree);
            if (undoManager == null)
                return null;

            undoUnit = new TextTreeDeleteContentUndoUnit(tree, start, end);

            undoManager.Add(undoUnit);

            return undoUnit;
        }

        // Adds a TextTreeExtractElementUndoUnit to the open parent undo unit, if any.
        // Called by TextContainer.ExtractElement.
        internal static TextTreeExtractElementUndoUnit CreateExtractElementUndoUnit(TextContainer tree, TextTreeTextElementNode elementNode)
        {
            UndoManager undoManager;
            TextTreeExtractElementUndoUnit undoUnit;

            undoManager = GetOrClearUndoManager(tree);
            if (undoManager == null)
                return null;

            undoUnit = new TextTreeExtractElementUndoUnit(tree, elementNode);

            undoManager.Add(undoUnit);

            return undoUnit;
        }

        // Returns the local UndoManager.
        // Returns null if there is no undo service or if the service exists
        // but is disabled or if there is no open parent undo unit.
        internal static UndoManager GetOrClearUndoManager(ITextContainer textContainer)
        {
            UndoManager undoManager;

            undoManager = textContainer.UndoManager;
            if (undoManager == null)
                return null;

            if (!undoManager.IsEnabled)
                return null;

            if (undoManager.OpenedUnit == null)
            {
                // There's no parent undo unit, so we can't open a child.
                //
                // Clear the undo stack -- since we depend on symbol offsets
                // matching the original document state when an undo unit is
                // executed, any of our units currently in the stack will be
                // corrupted after we finished the operation in progress.
                undoManager.Clear();
                return null;
            }

            return undoManager;
        }

        #endregion Internal methods
    }
}

