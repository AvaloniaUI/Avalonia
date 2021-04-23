// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description:
//
//     See spec at Undo spec.htm
//

using System;
using System.Windows;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics;
//using MS.Utility;
//using System.Windows.Markup;
//using System.Windows.Documents;
//using System.Windows.Controls.Primitives;
using Avalonia;

namespace MS.Internal.Documents
{
    /// <summary>
    /// Enum for the state of the undo manager
    /// </summary>
    /// <ExternalAPI Inherit="true"/>
    internal enum UndoState
    {
        /// <summary>
        /// Ready to accept new undo units; not currently undoing or redoing
        /// </summary>
        /// <ExternalAPI/>
        Normal,

        /// <summary>
        /// In the process of undoing
        /// </summary>
        /// <ExternalAPI/>
        Undo,

        /// <summary>
        /// In the process of redoing
        /// </summary>
        /// <ExternalAPI/>
        Redo,

        /// <summary>
        /// In the process of rolling back an aborted undo unit
        /// </summary>
        /// <ExternalAPI/>
        Rollback
    };

    /// <summary>
    /// Undo Manager
    /// </summary>
    internal class UndoManager
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// UndoManager constructor
        /// </summary>
        internal UndoManager() : base()
        {
            _scope = null;
            _state = UndoState.Normal;
            _isEnabled = false;
            _undoStack = new List<IUndoUnit>(4);
            _redoStack = new Stack(2);
            _topUndoIndex = -1;
            _bottomUndoIndex = 0;
            _undoLimit = _undoLimitDefaultValue;
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        #region Internal Methods

        /// <summary>
        /// Defines a given FrameworkElement as a scope for undo service.
        /// New instance of UndoManager created and attached to this element.
        /// </summary>
        /// <param name="scope">
        /// FrameworkElement to which new instance of UndoManager is attached.
        /// </param>
        /// <param name="undoManager">
        /// </param>
        internal static void AttachUndoManager(IAvaloniaObject scope, UndoManager undoManager)
        {
            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            if (undoManager == null)
            {
                throw new ArgumentNullException(nameof(undoManager));
            }

            if (undoManager is UndoManager && ((UndoManager)undoManager)._scope != null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoManagerAlreadyAttached)*/);
            }

            // Detach existing instance of undo manager if any
            DetachUndoManager(scope);

            // Attach the service to the scope via private dependency property
            scope.SetValue(UndoManager.UndoManagerInstanceProperty, undoManager);
            if (undoManager is UndoManager)
            {
                Debug.Assert(((UndoManager)undoManager)._scope == null);
                ((UndoManager)undoManager)._scope = scope;
            }

            undoManager.IsEnabled = true;
        }

        /// <summary>
        /// Detaches an undo service from the given FrameworkElement.
        /// </summary>
        /// <param name="scope">
        /// A FrameworkElement with UndoManager attached to it.
        /// </param>
        /// <remarks>
        /// Throws an exception if the scope does not have undo service attached to it.
        /// </remarks>
        internal static void DetachUndoManager(IAvaloniaObject scope)
        {
            UndoManager undoManager;

            if (scope == null)
            {
                throw new ArgumentNullException(nameof(scope));
            }

            // Detach existing undo service if any
            undoManager = scope.ReadLocalValue(UndoManager.UndoManagerInstanceProperty) as UndoManager;
            if (undoManager != null)
            {
                // Disable the service while in detached state
                undoManager.IsEnabled = false;

                // Remove the service from a tre
                scope.ClearValue(UndoManager.UndoManagerInstanceProperty);

                // Break the linkage to its scope
                if (undoManager is UndoManager)
                {
                    Debug.Assert(((UndoManager)undoManager)._scope == scope);
                    ((UndoManager)undoManager)._scope = null;
                }
            }
        }

        /// <summary>
        /// Finds the nearest undo service for a given uielement as a target.
        /// </summary>
        /// <param name="target">
        /// A ui element which is a descendant (or self) of an element
        /// to which undo service is attached.
        /// </param>
        /// <returns></returns>
        internal static UndoManager GetUndoManager(IAvaloniaObject target)
        {
            if (target == null)
            {
                return null;
            }
            else
            {
                return target.GetValue(UndoManager.UndoManagerInstanceProperty) as UndoManager;
            }
        }

        /// <summary>
        /// Add the given parent undo unit to the undo manager, making it the OpenedUnit of the
        /// innermost open parent unit (or the undo manager itself, if no parents are open).
        /// </summary>
        /// <param name="unit">
        /// parent unit to add
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if UndoManager is disabled or passed unit is already open.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if passed unit is null.
        /// </exception>
        internal void Open(IParentUndoUnit unit)
        {
            IParentUndoUnit deepestOpen;

            if (!IsEnabled)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoServiceDisabled)*/);
            }

            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            deepestOpen = DeepestOpenUnit;
            if (deepestOpen == unit)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitCantBeOpenedTwice)*/);
            }

            if (deepestOpen == null)
            {
                if (unit != LastUnit)
                {
                    // Don't want to add the unit again if we're just reopening it
                    Add(unit as IUndoUnit);
                    SetLastUnit(unit as IUndoUnit);
                }
                SetOpenedUnit(unit);
                unit.Container = this;
            }
            else
            {
                unit.Container = deepestOpen;
                deepestOpen.Open(unit);
            }
        }

        /// <summary>
        /// Opens a closed undo unit on the top of the stack.
        /// </summary>
        /// <param name="unit">
        /// IParentUndoUnit to reopen
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if:
        ///     UndoManager is disabled
        ///     another unit is already open
        ///     the given unit is locked
        ///     the given unit is not on top of the stack
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if passed unit is null.
        /// </exception>
        internal void Reopen(IParentUndoUnit unit)
        {
            if (!IsEnabled)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoServiceDisabled)*/);
            }

            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            if (OpenedUnit != null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitAlreadyOpen)*/);
            }

            switch (State)
            {
                case UndoState.Normal:
                case UndoState.Redo:
                    {
                        if (UndoCount == 0 || PeekUndoStack() != unit)
                        {
                            throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitNotOnTopOfStack)*/);
                        }

                        break;
                    }

                case UndoState.Undo:
                    {
                        if (RedoStack.Count == 0 || (IParentUndoUnit)RedoStack.Peek() != unit)
                        {
                            throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitNotOnTopOfStack)*/);
                        }

                        break;
                    }

                case UndoState.Rollback:
                default:
                    // should only happen if someone changes the UndoState enum or parameter validation
                    Debug.Assert(false);
                    break;
            }
            if (unit.Locked)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitLocked)*/);
            }

            Open(unit);
            _lastReopenedUnit = unit;
        }

        /// <summary>
        /// Closes the current open unit, adding it to the containing unit's undo stack if committed.
        /// </summary>
        internal void Close(UndoCloseAction closeAction)
        {
            Close(OpenedUnit, closeAction);
        }

        /// <summary>
        /// Closes an open child parent unit, adding it to the containing unit's undo stack if committed.
        /// </summary>
        /// <param name="unit">
        /// IParentUndoUnit to close.  If NULL, this unit's OpenedUnit is closed.
        /// </param>
        /// <param name="closeAction">
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if:
        ///     UndoManager is disabled
        ///     no undo unit is currently open
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if unit is null
        /// </exception>
        internal void Close(IParentUndoUnit unit, UndoCloseAction closeAction)
        {
            if (!IsEnabled)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoServiceDisabled)*/);
            }

            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            if (OpenedUnit == null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoNoOpenUnit)*/);
            }

            // find the parent of the given unit
            if (OpenedUnit != unit)
            {
                IParentUndoUnit closeParent;

                closeParent = OpenedUnit;

                while (closeParent.OpenedUnit != null && closeParent.OpenedUnit != unit)
                {
                    closeParent = closeParent.OpenedUnit;
                }

                if (closeParent.OpenedUnit == null)
                {
                    throw new ArgumentException(/*SR.Get(SRID.UndoUnitNotFound), nameof(unit)*/);
                }

                closeParent.Close(closeAction);
                return;
            }

            //
            // Close our open unit
            //
            if (closeAction != UndoCloseAction.Commit)
            {
                // discard unit
                SetState(UndoState.Rollback);
                if (unit.OpenedUnit != null)
                {
                    unit.Close(closeAction);
                }

                if (closeAction == UndoCloseAction.Rollback)
                {
                    unit.Do();
                }

                PopUndoStack();

                SetOpenedUnit(null);
                OnNextDiscard();
                SetLastUnit(_topUndoIndex == -1 ? null : PeekUndoStack()); // can be null, which is fine
                SetState(UndoState.Normal);
            }
            else
            {
                // commit unit
                if (unit.OpenedUnit != null)
                {
                    unit.Close(UndoCloseAction.Commit);
                }

                // flush redo stack
                if (State != UndoState.Redo && State != UndoState.Undo && RedoStack.Count > 0)
                {
                    RedoStack.Clear();
                }

                SetOpenedUnit(null);
            }
        }

        /// <summary>
        /// Adds an undo unit to the undo/redo stack, depending on current state.
        /// </summary>
        /// <param name="unit">
        /// IUndoUnit to add
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if:
        ///     UndoManager is disabled
        ///     unit is not IParentUndoUnit and there is no open IParentUndoUnit
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if unit is null
        /// </exception>
        internal void Add(IUndoUnit unit)
        {
            IParentUndoUnit parent;

            if (!IsEnabled)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoServiceDisabled)*/);
            }

            if (unit == null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            parent = DeepestOpenUnit;
            if (parent != null)
            {
                parent.Add(unit);
            }
            else if (unit is IParentUndoUnit)
            {
                ((IParentUndoUnit)unit).Container = this;
                if (LastUnit is IParentUndoUnit)
                {
                    ((IParentUndoUnit)LastUnit).OnNextAdd();
                }

                SetLastUnit(unit);
                if (State == UndoState.Normal || State == UndoState.Redo)
                {
                    if (++_topUndoIndex == UndoLimit)
                    {
                        _topUndoIndex = 0;
                    }
                    if (!(_topUndoIndex < UndoStack.Count && PeekUndoStack() == null) // Non-null topmost stack item
                        && (UndoLimit == -1 || UndoStack.Count < UndoLimit))
                    {
                        UndoStack.Add(unit);
                    }
                    else
                    {
                        if (PeekUndoStack() != null)
                        {
                            if (++_bottomUndoIndex == UndoLimit)
                            {
                                _bottomUndoIndex = 0;
                            }
                        }
                        UndoStack[_topUndoIndex] = unit;
                    }
                }
                else if (State == UndoState.Undo)
                {
                    RedoStack.Push(unit);
                }
                else if (State == UndoState.Rollback)
                {
                    // do nothing, throwing out the unit
                }
            }
            else
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoNoOpenParentUnit)*/);
            }
        }

        /// <summary>
        /// Clear the undo and redo stacks, as well as LastUnit.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if UndoManager is disabled
        /// </exception>
        internal void Clear()
        {
            if (!IsEnabled)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoServiceDisabled)*/);
            }

            // In practice, we only clear when the public IsUndoEnabled property is set false.
            // We'll check that property again when _imeSupportModeEnabled transitions to false.
            // While _imeSupportModeEnabled == true, we must leave undo enabled.
            if (!_imeSupportModeEnabled)
            {
                DoClear();
            }
        }

        /// <summary>
        /// Instructs the undo manager to invoke the given number of undo actions.
        /// </summary>
        /// <param name="count">
        /// Number of undo units to undo
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if UndoManager is disabled
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if count is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown if there's an error performing the undo
        /// </exception>
        internal void Undo(int count)
        {
            if (!IsEnabled)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoServiceDisabled)*/);
            }

            if (count > UndoCount || count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (State != UndoState.Normal)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoNotInNormalState)*/);
            }

            if (OpenedUnit != null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitOpen)*/);
            }

            Invariant.Assert(UndoCount > _minUndoStackCount);

            SetState(UndoState.Undo);

            bool exceptionThrown = true;

            try
            {
                while (count > 0)
                {
                    IUndoUnit unit;

                    unit = PopUndoStack();
                    unit.Do();
                    count--;
                }
                exceptionThrown = false;
            }
            finally
            {
                if (exceptionThrown)
                {
                    Clear();
                }
            }

            SetState(UndoState.Normal);
        }


        /// <summary>
        /// Instructs the undo manager to invoke the given number of redo actions.
        /// </summary>
        /// <param name="count">
        /// Number of redo units to redo
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if UndoManager is disabled
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if count is out of range
        /// </exception>
        /// <exception cref="Exception">
        /// Thrown if there's an error performing the redo
        /// </exception>
        internal void Redo(int count)
        {
            if (!IsEnabled)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoServiceDisabled)*/);
            }

            if (count > RedoStack.Count || count <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (State != UndoState.Normal)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoNotInNormalState)*/);
            }

            if (OpenedUnit != null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitOpen)*/);
            }

            SetState(UndoState.Redo);

            bool exceptionThrown = true;

            try
            {
                while (count > 0)
                {
                    IUndoUnit unit;

                    unit = (IUndoUnit)RedoStack.Pop();
                    unit.Do();
                    count--;
                }
                exceptionThrown = false;
            }
            finally
            {
                if (exceptionThrown)
                {
                    Clear();
                }
            }
            SetState(UndoState.Normal);
        }

        /// <summary>
        /// Called when a unit is discarded.  Unlocks the most recent unit added before the discarded one.
        /// </summary>
        internal virtual void OnNextDiscard()
        {
            if (UndoCount > 0)
            {
                IParentUndoUnit lastParent = (IParentUndoUnit)PeekUndoStack();

                lastParent.OnNextDiscard();
            }
        }

        // Peek the unit of UndoStack and the return the top unit of UndoStack.
        internal IUndoUnit PeekUndoStack()
        {
            if (_topUndoIndex < 0 || _topUndoIndex == UndoStack.Count)
            {
                return null;
            }
            else
            {
                return UndoStack[_topUndoIndex] as IUndoUnit;
            }
        }

        /// <summary>
        /// Explicitly sets the redo stack.
        /// </summary>
        /// <remark>
        /// DANGER!  This method is internal (and not private) only so it can be accessed
        /// by IMEs.  Using this method can result in instability and is strongly discouraged.
        /// </remark>
        internal Stack SetRedoStack(Stack value)
        {
            Stack previousValue = _redoStack;

            if (value == null)
            {
                value = new Stack(2);
            }

            _redoStack = value;

            return previousValue;
        }

        #endregion Internal Methods

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        #region Internal Properties

        /// <summary>
        /// IME mode switch.
        /// </summary>
        /// <remarks>
        /// In the context of IME events (TextInputStart/TextInputUpdated/TextInput)
        /// the undo stack is used internally to juggle document state such that
        /// the IMEs perceive no reentrancy.
        ///
        /// While IsImeSupportModeEnabled is set by our IME handler code
        ///  - The undo stack must be enabled.
        ///  - The undo stack must not be limited in size.
        /// </remarks>
        internal bool IsImeSupportModeEnabled
        {
            get { return _imeSupportModeEnabled; }
            set
            {
                if (value != _imeSupportModeEnabled)
                {
                    if (value)
                    {
                        // While _imeSupportModeEnabled is in effect, the undo
                        // stack is not constrained.  If necessary, force it
                        // into the expected infinite stack configuration: growing from index 0.
                        if (_bottomUndoIndex != 0 && _topUndoIndex >= 0)
                        {
                            List<IUndoUnit> undoStack = new List<IUndoUnit>(UndoCount);
                            int i;

                            if (_bottomUndoIndex > _topUndoIndex)
                            {
                                for (i = _bottomUndoIndex; i < UndoLimit; i++)
                                {
                                    undoStack.Add(_undoStack[i]);
                                }
                                _bottomUndoIndex = 0;
                            }

                            for (i = _bottomUndoIndex; i <= _topUndoIndex; i++)
                            {
                                undoStack.Add(_undoStack[i]);
                            }

                            _undoStack = undoStack;
                            _bottomUndoIndex = 0;
                            _topUndoIndex = undoStack.Count - 1;
                        }

                        _imeSupportModeEnabled = value;
                    }
                    else
                    {
                        // Transitioning false.

                        _imeSupportModeEnabled = value;

                        if (!this.IsEnabled)
                        {
                            // If the stack was originally disabled, remove all content added.
                            DoClear();
                        }
                        else
                        {
                            // Free up units that exceed the original undo limit.
                            //
                            // NB: we are not clearing the undo stack here if UndoLimit changed
                            // while _imeSupportMode was true, which is at odds
                            // with behavior from the UndoLimit setter.  It always clears the
                            // stack when the UndoLimit changes.  We're skipping that step
                            // because supporting it would require adding an additional piece
                            // of state to this class (to delay the reset from happening
                            // until we get here).
                            //
                            // it would be more efficient to null out truncated values
                            // here rather than always reallocating the copying the entire
                            // stack.
                            if (UndoLimit >= 0 && _topUndoIndex >= UndoLimit)
                            {
                                List<IUndoUnit> undoStack = new List<IUndoUnit>(UndoLimit);

                                for (int i = _topUndoIndex + 1 - UndoLimit; i <= _topUndoIndex; i++)
                                {
                                    undoStack.Add(_undoStack[i]);
                                }

                                _undoStack = undoStack;
                                _bottomUndoIndex = 0;
                                _topUndoIndex = UndoLimit - 1;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Maximum number of undo units allowed on the stack
        /// </summary>
        internal int UndoLimit
        {
            get
            {
                return _imeSupportModeEnabled ? -1 : _undoLimit;
            }
            set
            {
                _undoLimit = value;

                // While _imeSupportModeEnabled == true, we must leave undo enabled and
                // the undo limit may not be cleared.
                if (!_imeSupportModeEnabled)
                {
                    DoClear();
                }
            }
        }

        /// <summary>
        /// Returns the current state of the undo manager
        /// </summary>
        internal UndoState State
        {
            get
            {
                return _state;
            }
        }

        /// <summary>
        /// Whether or not the Undo Manager is enabled.  If it isn't, any changes submitted
        /// to the undo manager will be ignored.
        /// </summary>
        /// <remarks>
        /// IsEnabled will evaluate to false even when explicitly set true, if the
        /// current UndoLimit is zero.
        /// </remarks>
        internal bool IsEnabled
        {
            get
            {
                return _imeSupportModeEnabled || (_isEnabled && _undoLimit != 0);
            }

            set
            {
                _isEnabled = value;
            }
        }

        /// <summary>
        /// Returns the topmost opened ParentUndoUnit on the current stack
        /// </summary>
        internal IParentUndoUnit OpenedUnit
        {
            get
            {
                return _openedUnit;
            }
        }

        /// <summary>
        /// Readonly access to the last unit added to the IParentUndoUnit
        /// </summary>
        internal IUndoUnit LastUnit
        {
            get
            {
                return _lastUnit;
            }
        }

        /// <summary>
        /// Readonly access to the most recently reopened unit.  Note that this unit is not
        /// guaranteed to still exist in the stack-- it's just the last unit to be reopened.
        /// </summary>
        /// <remarks>
        /// Used by TextBox to determine if a text change causes a new unit to be opened or
        /// an existing unit to be reopened.
        /// </remarks>
        internal IParentUndoUnit LastReopenedUnit
        {
            get
            {
                return _lastReopenedUnit;
            }
        }

        /// <summary>
        /// Number of top-level units in the undo stack
        /// </summary>
        internal int UndoCount
        {
            get
            {
                int count;
                if (UndoStack.Count == 0 || _topUndoIndex < 0)
                {
                    count = 0;
                }
                else if (_topUndoIndex == _bottomUndoIndex - 1 && PeekUndoStack() == null)
                {
                    count = 0;
                }
                else if (_topUndoIndex >= _bottomUndoIndex)
                {
                    count = _topUndoIndex - _bottomUndoIndex + 1;
                }
                else
                {
                    count = _topUndoIndex + (UndoLimit - _bottomUndoIndex) + 1;
                }
                return count;
            }
        }

        /// <summary>
        /// Number of top-level units in the redo stack
        /// </summary>
        internal int RedoCount
        {
            get
            {
                return RedoStack.Count;
            }
        }

        // Default value for UndoLimitProperty.
        internal static int UndoLimitDefaultValue
        {
            get
            {
                return _undoLimitDefaultValue;
            }
        }

        /// <summary>
        /// Returns the zero based unit in the undo stack.
        /// </summary>
        /// <remarks>
        /// DANGER!  This method is internal (and not private) only so it can be accessed
        /// by IMEs.  Using this method can result in instability and is strongly discouraged.
        ///
        /// This method may only be called while ImeSupportModeEnabled == true.
        /// It does not handle circular undo stacks (_bottomUndoIndex > _topUndoIndex).
        /// </remarks>
        internal IUndoUnit GetUndoUnit(int index)
        {
            Invariant.Assert(index < this.UndoCount);
            Invariant.Assert(index >= 0);
            Invariant.Assert(_bottomUndoIndex == 0);
            Invariant.Assert(_imeSupportModeEnabled);

            return _undoStack[index];
        }

        /// <summary>
        /// Removes a range of units in the undo stack.
        /// </summary>
        /// <remarks>
        /// DANGER!  This method is internal (and not private) only so it can be accessed
        /// by IMEs.  Using this method can result in instability and is strongly discouraged.
        ///
        /// This method may only be called while ImeSupportModeEnabled == true.
        /// It does not handle circular undo stacks (_bottomUndoIndex > _topUndoIndex).
        /// </remarks>
        internal void RemoveUndoRange(int index, int count)
        {
            Invariant.Assert(index >= 0);
            Invariant.Assert(count >= 0);
            Invariant.Assert(count + index <= this.UndoCount);
            Invariant.Assert(_bottomUndoIndex == 0);
            Invariant.Assert(_imeSupportModeEnabled);

            int i;

            // Slide following units backward to fill the gap.
            for (i = index + count; i <= _topUndoIndex; i++)
            {
                _undoStack[i - count] = _undoStack[i];
            }

            // null out old references.
            for (i = _topUndoIndex - (count - 1); i <= _topUndoIndex; i++)
            {
                _undoStack[i] = null;
            }

            // Decrement the top index.
            _topUndoIndex -= count;
        }

        /// <summary>
        /// The minimum allowed depth of the undo stack.
        /// </summary>
        /// <remarks>
        /// DANGER!  This method is internal (and not private) only so it can be accessed
        /// by IMEs.  Using this method can result in instability and is strongly discouraged.
        ///
        /// Calling Undo when UndoCount == MinUndoStackCount is an error (and
        /// will trigger an Invariant failure).
        ///
        /// This property is set during IME composition handling,
        /// to ensure that applications cannot undo IME changes
        /// inside the scope of TextInputEvent handlers.
        /// </remarks>
        internal int MinUndoStackCount
        {
            get
            {
                return _minUndoStackCount;
            }

            set
            {
                _minUndoStackCount = value;
            }
        }

        #endregion Internal Properties

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// State of the Undo Service
        /// </summary>
        /// <param name="value">
        /// UndoState to which State is to be set
        /// </param>
        protected void SetState(UndoState value)
        {
            _state = value;
        }

        /// <summary>
        /// current opened unit
        /// </summary>
        /// <param name="value">
        /// IParentUndoUnit to which OpenedUnit is to bet set
        /// </param>
        protected void SetOpenedUnit(IParentUndoUnit value)
        {
            _openedUnit = value;
        }

        /// <summary>
        /// Set LastUnit
        /// </summary>
        /// <param name="value">
        /// IUndoUnit to which LastUnit is to be set
        /// </param>
        protected void SetLastUnit(IUndoUnit value)
        {
            _lastUnit = value;
        }

        /// <summary>
        /// Returns the deepest open parent undo unit contained within this one.
        /// </summary>
        protected IParentUndoUnit DeepestOpenUnit
        {
            get
            {
                IParentUndoUnit openedUnit;

                openedUnit = OpenedUnit;
                if (openedUnit != null)
                {
                    while (openedUnit.OpenedUnit != null)
                    {
                        openedUnit = openedUnit.OpenedUnit;
                    }
                }
                return openedUnit;
            }
        }

        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// the undo stack
        /// </summary>
        protected List<IUndoUnit> UndoStack
        {
            get
            {
                return _undoStack;
            }
        }

        /// <summary>
        /// the redo stack
        /// </summary>
        protected Stack RedoStack
        {
            get
            {
                return _redoStack;
            }
        }

        #endregion Protected Properties

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        /// <summary>
        /// Performs the work of the clear operation.  Called by the public Clear() method,
        /// or by UndoManager itself if it wants to clear its stacks without regard to
        /// whether or not it's enabled.
        /// </summary>
        private void DoClear()
        {
            Invariant.Assert(!_imeSupportModeEnabled); // We can't clear the undo stack while ime code depends on it.

            if (UndoStack.Count > 0)
            {
                UndoStack.Clear();
                UndoStack.TrimExcess();
            }

            if (RedoStack.Count > 0)
            {
                RedoStack.Clear();
            }

            SetLastUnit(null);
            SetOpenedUnit(null);
            _topUndoIndex = -1;
            _bottomUndoIndex = 0;
        }

        private IUndoUnit PopUndoStack()
        {
            int undoCount = UndoCount - 1;
            IUndoUnit unit = (IUndoUnit)UndoStack[_topUndoIndex];
            UndoStack[_topUndoIndex--] = null;
            if (_topUndoIndex < 0 && undoCount > 0)
            {
                Invariant.Assert(UndoLimit > 0);
                _topUndoIndex = UndoLimit - 1;  // This should never be possible with an unlimited stack
            }

            return unit;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        #region Private Properties

        /// <summary>
        /// Property that identifies this service in ui element tree.
        /// </summary>
        private static readonly AttachedProperty<UndoManager> UndoManagerInstanceProperty = AvaloniaProperty.RegisterAttached<UndoManager, AvaloniaObject, UndoManager>( //
            "UndoManagerInstance", inherits: true);

        #endregion Private Properties

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private IAvaloniaObject _scope; // an element to which this instance of scope is attached
        private IParentUndoUnit _openedUnit;
        private IUndoUnit _lastUnit;
        private List<IUndoUnit> _undoStack;   // stack of undo units
        private Stack _redoStack;   // stack of redo units
        private UndoState _state;
        private bool _isEnabled;
        private IParentUndoUnit _lastReopenedUnit;
        private int _topUndoIndex;      // index of the topmost unit in the undo stack
        private int _bottomUndoIndex;   // index of the bottommost unit in the undo stack
        private int _undoLimit;         // maximum size of undo stack, -1 means infinite.
        private int _minUndoStackCount;
        private bool _imeSupportModeEnabled;

        // Default value for UndoLimitProperty. This is the same as the default for Win32 edit controls.
        // See http://msdn.microsoft.com/en-us/library/ms652191.aspx
        private const int _undoLimitDefaultValue = 100;

        #endregion Private Fields
    }
}
