// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//
//              See spec at Undo spec.htm
//

using System.Collections;

using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// IParentUndoUnit interface
    /// </summary>

    internal interface IParentUndoUnit : IUndoUnit
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        /// <summary>
        /// Clear all units from the IParentUndoUnit.  These units aren't "undone", they're simply deleted.
        /// </summary>
        void Clear();

        /// <summary>
        /// Open a new IParentUndoUnit inside the deepest open IParentUndoUnit.
        /// IParentUndoUnits can nest arbitrarily deep.
        /// </summary>
        /// <param name="newUnit">IParentUndoUnit to open</param>
        void Open(IParentUndoUnit newUnit);

        /// <summary>
        /// Closes the open IParentUndoUnit within the IUndoService
        /// </summary>
        void Close(UndoCloseAction closeAction);

        /// <summary>
        /// Closes the given open IParentUndoUnit within the current container or its child
        /// containers.  If closingUnit == null, IParentUndoUnit's currently open unit (if any)
        /// is closed instead.  
        /// </summary>
        void Close(IParentUndoUnit closingUnit, UndoCloseAction closeAction);

        /// <summary>
        /// Adds the given unit to the deepest open IParentUndoUnit.  Since newUnit is not
        /// necessarily an IParentUndoUnit, newUnit is not opened. 
        /// </summary>
        /// <param name="newUnit">IUndoUnit to add</param>
        void Add(IUndoUnit newUnit);

        /// <summary>
        /// Notifies the last parent undo unit in the collection that a new unit has been added
        /// to the collection.  The undo manager or containing parent undo unit calls this
        /// function on its most recently added parent undo unit to notify it that the context
        /// has changed and no further modifications should be made to it.
        /// </summary>
        void OnNextAdd();

        /// <summary>
        /// Notifies the last parent undo unit in the collection that the unit immediately
        /// after it has been discarded.  The undo manager or containing parent undo unit calls this
        /// function on its most recently added parent undo unit to notify it that it should unlock.
        /// its last unit to allow it to be changed again.
        /// </summary>
        void OnNextDiscard();

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// Last unit added to the IParentUndoUnit
        /// </summary>
        IUndoUnit LastUnit
        {
            get;
        }

        /// <summary>
        /// Readonly access to the unit currently open at the top level of this IParentUndoUnit
        /// </summary>
        IParentUndoUnit OpenedUnit
        {
            get;
        }

        /// <summary>
        /// text description of this unit
        /// </summary>
        string Description
        {
            get;
            set;
        }

        /// <summary>
        /// Whether or not the unit can accept new changes
        /// </summary>
        bool Locked
        {
            get;
        }

        /// <summary>
        /// IUndoService or IParentUndoUnit that contains this IParentUndoUnit.  This is a backpointer
        /// only-- setting this value does not change the IParentUndoUnit's actual container.
        /// </summary>
        object Container
        {
            get;
            set;
        }

        #endregion Public Properties
    }
}

