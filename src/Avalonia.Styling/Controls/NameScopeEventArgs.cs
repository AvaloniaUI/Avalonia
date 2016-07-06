// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Controls
{
    public class NameScopeEventArgs : EventArgs
    {
        public NameScopeEventArgs(string name, object element)
        {
            Name = name;
            Element = element;
        }

        public string Name { get; }
        public object Element { get; }
    }
}
