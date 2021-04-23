// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: Delegate for a Change event fired on a TextContainer.
//

using System;
using System.Windows;

namespace System.Windows.Documents
{
    // Delegate for a ChangeAdded event fired on a TextContainer.
    internal delegate void TextContainerChangeEventHandler(object sender, TextContainerChangeEventArgs e);
}
