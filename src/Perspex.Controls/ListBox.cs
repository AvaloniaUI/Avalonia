// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls.Generators;
using Perspex.Controls.Primitives;

namespace Perspex.Controls
{
    public class ListBox : SelectingItemsControl
    {
        protected override IItemContainerGenerator CreateItemContainerGenerator()
        {
            return new ItemContainerGenerator<ListBoxItem>(this);
        }
    }
}
