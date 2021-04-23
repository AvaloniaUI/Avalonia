// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: 
//

using System;
using System.Windows;
using System.Collections;

namespace System.Windows.Documents
{
    /// <summary>
    /// Specifies the type of change applied to TextContainer content.
    /// </summary>
    internal enum TextChangeType
    { 
        /// <summary>
        /// New content was inserted.
        /// </summary>
        ContentAdded,

        /// <summary>
        /// Content was deleted.
        /// </summary>
        ContentRemoved, 

        /// <summary>
        /// A local AvaloniaProperty value changed.
        /// </summary>
        PropertyModified,
    }
}
