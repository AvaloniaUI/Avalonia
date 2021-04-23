// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: LogicalDirection defines a logical direction for movement in text
//

namespace System.Windows.Documents
{
    /// <summary>
    ///  LogicalDirection defines a logical direction for movement in text.  It 
    ///  is also used to determine where a TextPointer will move when content
    ///  is inserted at the TextPointer.  
    /// </summary>
    public enum LogicalDirection
    {
        /// <summary>
        ///  Backward - Causes the TextPointer to be positioned 
        ///  before the newly inserted content
        /// </summary>
        Backward            = 0,
        /// <summary>
        ///  Forward - Causes the TextPointer 
        ///  to be positioned after the newly inserted content.
        /// </summary>
        Forward             = 1
    }
}
