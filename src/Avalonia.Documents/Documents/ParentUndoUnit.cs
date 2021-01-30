// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//
//      See spec at Undo spec.htm 
//

using System;
using System.Windows;
using System.Collections;
//using MS.Utility;

namespace MS.Internal.Documents
{
    /// <summary>
    /// ParentUndoUnit
    /// </summary>
    internal class ParentUndoUnit : IParentUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="description">
        /// Text description of the undo unit
        /// </param>
        public ParentUndoUnit(string description) : base()
        {
            Init(description);
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Opens a new parent undo unit.
        /// </summary>
        /// <param name="newUnit">
        /// IParentUndoUnit to open
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if passed unit is null.
        /// </exception>
        public virtual void Open(IParentUndoUnit newUnit)
        {
            IParentUndoUnit deepestOpen;

            if (newUnit == null)
            {
                throw new ArgumentNullException("newUnit");
            }

            deepestOpen = DeepestOpenUnit;
            if (deepestOpen == null)
            {
                if (IsInParentUnitChain(newUnit))
                {
                    throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitCantBeOpenedTwice)*/);
                }

                _openedUnit = newUnit;
                if (newUnit != null)
                {
                    newUnit.Container = this;
                }
            }
            else
            {
                if (newUnit != null)
                {
                    newUnit.Container = deepestOpen;
                }

                deepestOpen.Open(newUnit);
            }
        }

        /// <summary>
        /// Closes the current open unit, adding it to the containing unit's undo stack if committed.
        /// </summary>
        public virtual void Close(UndoCloseAction closeAction)
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
        /// Thrown if no undo unit is currently open
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown if unit is null
        /// </exception>
        public virtual void Close(IParentUndoUnit unit, UndoCloseAction closeAction)
        {
            UndoManager undoManager;

            if (unit == null)
            {
                throw new ArgumentNullException("unit");
            }

            if (OpenedUnit == null)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoNoOpenUnit)*/);
            }

            // find the parent of the given unit
            if (OpenedUnit != unit)
            {
                IParentUndoUnit closeParent;

                closeParent = this;
                while (closeParent.OpenedUnit != null && closeParent.OpenedUnit != unit)
                {
                    closeParent = closeParent.OpenedUnit;
                }

                if (closeParent.OpenedUnit == null)
                {
                    throw new ArgumentException(/*SR.Get(SRID.UndoUnitNotFound), "unit"*/);
                }

                if (closeParent != this)
                {
                    closeParent.Close(closeAction);
                    return;
                }
            }

            //
            // Close our open unit
            //

            // Get the undo manager
            undoManager = TopContainer as UndoManager;

            if (closeAction != UndoCloseAction.Commit)
            {
                // discard unit
                if (undoManager != null)
                {
                    undoManager.IsEnabled = false;
                }

                if (OpenedUnit.OpenedUnit != null)
                {
                    OpenedUnit.Close(closeAction);
                }

                if (closeAction == UndoCloseAction.Rollback)
                {
                    ((IParentUndoUnit)OpenedUnit).Do();
                }

                _openedUnit = null;

                // unlock previous unit(s)
                if (TopContainer is UndoManager)
                {
                    ((UndoManager)TopContainer).OnNextDiscard();
                }
                else
                {
                    ((IParentUndoUnit)TopContainer).OnNextDiscard();
                }

                if (undoManager != null)
                {
                    undoManager.IsEnabled = true;
                }
            }
            else
            {
                // commit unit
                if (OpenedUnit.OpenedUnit != null)
                {
                    OpenedUnit.Close(UndoCloseAction.Commit);
                }

                IParentUndoUnit openedUnit = OpenedUnit;
                _openedUnit = null;
                Add(openedUnit);
                SetLastUnit(openedUnit);
            }
        }

        /// <summary>
        /// Adds an undo unit to the deepest open parent unit's collection.
        /// </summary>
        /// <param name="unit">
        /// IUndoUnit to add
        /// </param>
        /// <returns>
        /// TRUE if unit successfully added, FALSE otherwise
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if unit is null
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if:
        ///     unit being added is already open
        ///     unit being added to is locked
        /// </exception>
        public virtual void Add(IUndoUnit unit)
        {
            IParentUndoUnit parentUndoUnit;

            if (unit == null)
            {
                throw new ArgumentNullException("unit");
            }

            parentUndoUnit = DeepestOpenUnit;

            // If we have an open unit, call Add on it
            if (parentUndoUnit != null)
            {
                parentUndoUnit.Add(unit);
                return;
            }

            if (IsInParentUnitChain(unit))
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitCantBeAddedTwice)*/);
            }

            if (Locked)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitLocked)*/);
            }

            if (!Merge(unit))
            {
                _units.Push(unit);
                if (LastUnit is IParentUndoUnit)
                {
                    ((IParentUndoUnit)LastUnit).OnNextAdd();
                }

                SetLastUnit(unit);
            }
        }

        /// <summary>
        /// Clear all undo units.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if unit is locked
        /// </exception>
        public virtual void Clear()
        {
            if (Locked)
            {
                throw new InvalidOperationException(/*SR.Get(SRID.UndoUnitLocked)*/);
            }

            _units.Clear();
            SetOpenedUnit(null);
            SetLastUnit(null);
        }

        /// <summary>
        /// Notifies the last parent undo unit in the collection that a new unit has been added
        /// to the collection.  The undo manager or containing parent undo unit calls this
        /// function on its most recently added parent undo unit to notify it that the context
        /// has changed and no further modifications should be made to it.
        /// </summary>
        public virtual void OnNextAdd()
        {
            _locked = true;
            foreach (IUndoUnit unit in _units)
            {
                if (unit is IParentUndoUnit)
                {
                    ((IParentUndoUnit)unit).OnNextAdd();
                }
            }
        }

        /// <summary>
        /// Inverse of OnNextAdd().  Called when a unit previously added after this one gets discarded.
        /// </summary>
        public virtual void OnNextDiscard()
        {
            _locked = false;
            IParentUndoUnit lastParent = this;
            foreach (IUndoUnit unit in _units)
            {
                if (unit is IParentUndoUnit)
                {
                    lastParent = unit as IParentUndoUnit;
                }
            }

            if (lastParent != this)
            {
                lastParent.OnNextDiscard();
            }
        }

        /// <summary>
        /// Implements IUndoUnit::Do().  For IParentUndoUnit, this means iterating through
        /// all contained units and calling their Do().
        /// </summary>
        public virtual void Do()
        {
            IParentUndoUnit redo;
            UndoManager topContainer;

            // Create the parent redo unit
            redo = CreateParentUndoUnitForSelf();
            topContainer = TopContainer as UndoManager;

            if (topContainer != null)
            {
                if (topContainer.IsEnabled)
                {
                    topContainer.Open(redo);
                }
            }

            while (_units.Count > 0)
            {
                IUndoUnit unit;

                unit = _units.Pop() as IUndoUnit;
                unit.Do();
            }

            if (topContainer != null)
            {
                if (topContainer.IsEnabled)
                {
                    topContainer.Close(redo, UndoCloseAction.Commit);
                }
            }
        }

        /// <summary>
        /// Iterates through all child units, attempting to merge the given unit into that unit.
        /// Only simple undo units are merged-- parent undo units are not.
        /// </summary>
        /// <param name="unit">
        /// IUndoUnit to merge
        /// </param>
        /// <returns>
        /// true if unit was merged, false otherwise
        /// </returns>
        public virtual bool Merge(IUndoUnit unit)
        {
            Invariant.Assert(unit != null);
            return false;
        }

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// text description of this unit
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (value == null)
                {
                    value = String.Empty;
                }

                _description = value;
            }
        }

        /// <summary>
        /// Returns the most recent child parent unit
        /// </summary>
        public IParentUndoUnit OpenedUnit
        {
            get
            {
                return _openedUnit;
            }
        }

        /// <summary>
        /// Readonly access to the last unit added to the IParentUndoUnit
        /// </summary>
        public IUndoUnit LastUnit
        {
            get
            {
                return _lastUnit;
            }
        }

        /// <summary>
        /// Whether or not the unit can accept new changes
        /// </summary>
        public virtual bool Locked
        {
            get
            {
                return _locked;
            }

            protected set
            {
                _locked = value;
            }
        }

        /// <summary>
        /// The IParentUndoUnit or UndoManager this parent unit is contained by.
        ///</summary>
        public object Container
        {
            get
            {
                return _container;
            }
            set
            {
                if (!(value is IParentUndoUnit || value is UndoManager))
                {
                    throw new Exception(/*SR.Get(SRID.UndoContainerTypeMismatch)*/);
                }
                _container = value;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Public Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Protected Methods
        //
        //------------------------------------------------------

        #region Protected Methods

        /// <summary>
        /// Initialization common to all constructors
        /// </summary>
        /// <param name="description">
        /// String describing the undo unit
        /// </param>
        protected void Init(string description)
        {
            if (description == null)
            {
                description = String.Empty;
            }

            _description = description;
            _locked = false;
            _openedUnit = null;
            _units = new Stack(2);
            _container = null;
        }

        /// <summary>
        /// current opened unit
        /// </summary>
        /// <param name="value">
        /// IParentUndoUnit to which OpenedUnit is to be set
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
        /// Callback from Do method allowing derived subclass to
        /// provide its own ParentUndoUnit. By default general
        /// ParentUndoUnit is created.
        /// </summary>
        /// <returns></returns>
        protected virtual IParentUndoUnit CreateParentUndoUnitForSelf()
        {
            return new ParentUndoUnit(Description);
        }


        #endregion Protected Methods

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        #region Protected Properties

        /// <summary>
        /// Returns the deepest open parent undo unit contained within this one.
        /// </summary>
        protected IParentUndoUnit DeepestOpenUnit
        {
            get
            {
                IParentUndoUnit openedUnit;

                openedUnit = _openedUnit;
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

        /// <summary>
        /// Returns the outermost container of this unit.
        /// </summary>
        protected object TopContainer
        {
            get
            {
                object container;

                container = this;
                while (container is IParentUndoUnit && ((IParentUndoUnit)container).Container != null)
                {
                    container = ((IParentUndoUnit)container).Container;
                }
                return container;
            }
        }

        protected Stack Units
        {
            get
            {
                return _units;
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
        /// Walk up the parent undo unit chain and make sure none of the parent units
        /// in that chain are the same as the given unit.
        /// </summary>
        /// <param name="unit">
        /// Unit to search for in the parent chain
        /// </param>
        /// <returns>
        /// true if the unit is already in the parent chain, false otherwise
        /// </returns>
        bool IsInParentUnitChain(IUndoUnit unit)
        {
            if (unit is IParentUndoUnit)
            {
                IParentUndoUnit parent;

                parent = this;
                do
                {
                    if (parent == unit)
                    {
                        return true;
                    }

                    parent = parent.Container as IParentUndoUnit;
                } while (parent != null);
            }
            return false;
        }

        #endregion Private methods

        //------------------------------------------------------
        //
        //  Private Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private string _description;
        private bool _locked;
        private IParentUndoUnit _openedUnit;
        private IUndoUnit _lastUnit;
        private Stack _units;
        private object _container;

        #endregion Private Fields
    }
}
