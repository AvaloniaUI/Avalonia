// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Avalonia.Layout
{
    [System.Diagnostics.DebuggerDisplay("Count = {Count}, Height = {Height}")]
    internal class StaggeredColumnLayout : List<StaggeredItem>
    {
        public double Height { get; private set; }

        public new void Add(StaggeredItem item)
        {
            Height = item.Top + item.Height;
            base.Add(item);
        }

        public new void Clear()
        {
            Height = 0;
            base.Clear();
        }
    }
}
