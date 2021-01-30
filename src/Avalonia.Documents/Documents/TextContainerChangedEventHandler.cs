// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Delegate for a TextChangedEvent fired on a TextContainer.
//

using System;
using System.Windows;

namespace System.Windows.Documents
{
    /// <summary>
    ///  The TextChangedEventHandler delegate is called with TextContainerChangedEventArgs every time
    ///  content is added to or removed from the TextContainer
    /// </summary>
    internal delegate void TextContainerChangedEventHandler(object sender, TextContainerChangedEventArgs e);
}
