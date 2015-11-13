// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex
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
