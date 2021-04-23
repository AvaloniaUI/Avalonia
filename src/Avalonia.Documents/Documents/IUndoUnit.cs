// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//
//              See spec at Undo spec.htm 
//

using System;

namespace MS.Internal.Documents
{
    /// <summary>
    /// IUndoUnit interface
    /// </summary>
    /// 
    internal interface IUndoUnit
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
        /// Perform the appropriate action for this unit.  If this is a parent undo unit, the
        /// parent must create an appropriate parent undo unit to contain the redo units.
        /// </summary>
        void Do();

        /// <summary>
        /// Attempt to merge the given undo unit with this unit.
        /// Merge is typically called by a ParentUndoUnit's Add() method.  If the merge
        /// succeeds, the parent should not add the merged unit.
        /// </summary>
        /// <param name="unit">Unit to merge into this one</param>
        /// <returns>
        /// true if unit was merged.
        /// false otherwise
        /// </returns>
        bool Merge(IUndoUnit unit);

        #endregion Public Methods        

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

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

        //------------------------------------------------------
        //
        //  Protected Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Methods
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Properties
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Internal Events
        //
        //------------------------------------------------------

        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

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
    }
}

