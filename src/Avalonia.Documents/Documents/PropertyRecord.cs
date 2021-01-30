// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// Description: DependencyProperty/value pair struct used by TextContainer undo.
//

using System;
using Avalonia;

namespace System.Windows.Documents
{
    // This struct records DependencyProperty/value pairs. We use the struct
    // extensively because LocalValueEnumerators may not be cached safely.
    // It is identical to base's LocalValueEntry except that it adds setters.
    internal struct PropertyRecord
    {
        internal AvaloniaProperty Property
        {
            get { return _property; }
            set { _property = value; }
        }

        internal object Value
        {
            get { return _value; }
            set { _value = value; }
        }

        private AvaloniaProperty _property;

        private object _value;
    }
}
