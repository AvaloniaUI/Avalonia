// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Undo wrapper for any edit on or through a TextRange or
//              TextContainer.
//

//using System;
//using System.Windows;
using MS.Internal;
using MS.Internal.Documents;

namespace System.Windows.Documents
{
    // Undo wrapper for any edit on or through a TextRange or TextContainer.
    internal class ChangeBlockUndoRecord
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        // Constructor, called when a change block is opening.
        internal ChangeBlockUndoRecord(ITextContainer textContainer, string actionDescription)
        {
            if (textContainer.UndoManager != null)
            {
                _undoManager = textContainer.UndoManager;

                if (_undoManager.IsEnabled)
                {
                    // Don't bother opening an undo unit if the owning control is
                    // still being initialized (ie, programmatic load by parser).
                    if (textContainer.TextView != null)
                    {
                        // Don't bother opening a new undo unit if we're already nested
                        // inside another.
                        if (_undoManager.OpenedUnit == null)
                        {
                            if (textContainer.TextSelection != null)
                            {
                                _parentUndoUnit = new TextParentUndoUnit(textContainer.TextSelection);
                            }
                            else
                            {
                                _parentUndoUnit = new ParentUndoUnit(actionDescription);
                            }

                            _undoManager.Open(_parentUndoUnit);
                        }
                    }
                    else
                    {
                        // If the owning control isn't initialized (parser is still running),
                        // don't add anything to the undo record.  Instead, clear it.
                        _undoManager.Clear();
                    }
                }
            }
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        // Called when a change block is closing.
        internal void OnEndChange()
        {
            // Commit our undo unit.
            if (_parentUndoUnit != null)
            {
                IParentUndoUnit openedUnit;

                if (_parentUndoUnit.Container is UndoManager)
                {
                    openedUnit = ((UndoManager)_parentUndoUnit.Container).OpenedUnit;
                }
                else
                {
                    openedUnit = ((IParentUndoUnit)_parentUndoUnit.Container).OpenedUnit;
                }

                // UIElementPropertyUndoUnit can clear the undo stack if a change is made to a databound property
                if (openedUnit == _parentUndoUnit)
                {
                    if (_parentUndoUnit is TextParentUndoUnit)
                    {
                        ((TextParentUndoUnit)_parentUndoUnit).RecordRedoSelectionState();
                    }
                    Invariant.Assert(_undoManager != null);
                    _undoManager.Close(_parentUndoUnit, (_parentUndoUnit.LastUnit != null) ? UndoCloseAction.Commit : UndoCloseAction.Discard);
                }
            }
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly UndoManager _undoManager;

        private readonly IParentUndoUnit _parentUndoUnit;

        #endregion Private Fields
    }
}
