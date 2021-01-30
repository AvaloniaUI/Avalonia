// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//

using System;
using System.Windows;
using System.Collections;

namespace System.Windows.Controls
{
    /// <summary>
    /// Specifies the changes applied to TextContainer content.
    /// </summary>
    public class TextChange
    {
        //------------------------------------------------------
        //
        //  Constructors
        //
        //------------------------------------------------------

        #region Constructors

        internal TextChange()
        {
        }

        #endregion Constructors

        //------------------------------------------------------
        //
        //  Public Members
        //
        //------------------------------------------------------

        #region Public Members

        /// <summary>
        /// 0-based character offset for this change
        /// </summary>
        public int Offset
        {
            get
            {
                return _offset;
            }
            internal set
            {
                _offset = value;
            }
        }

        /// <summary>
        /// Number of characters added
        /// </summary>
        public int AddedLength
        {
            get
            {
                return _addedLength;
            }
            internal set
            {
                _addedLength = value;
            }
        }

        /// <summary>
        /// Number of characters removed
        /// </summary>
        public int RemovedLength
        {
            get
            {
                return _removedLength;
            }
            internal set
            {
                _removedLength = value;
            }
        }

        #endregion Public Members

        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private int _offset;
        private int _addedLength;
        private int _removedLength;

        #endregion Private Fields
    }
}
