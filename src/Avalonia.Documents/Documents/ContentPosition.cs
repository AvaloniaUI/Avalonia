// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// 
//
// Description: Represents a certain content's position. This position is 
//              content specific. 
//
//

namespace System.Windows.Documents 
{
    /// <summary>
    /// Represents a certain content's position. This position is content 
    /// specific.
    /// </summary>
    public abstract class ContentPosition
    {
        /// <summary>
        /// Static representation of a non-existent ContentPosition.
        /// </summary>
        public static readonly ContentPosition Missing = new MissingContentPosition();

        #region Missing

        /// <summary>
        /// Representation of a non-existent ContentPosition.
        /// </summary>
        private class MissingContentPosition : ContentPosition {}

        #endregion Missing
    }
}
