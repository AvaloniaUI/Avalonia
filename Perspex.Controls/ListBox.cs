// -----------------------------------------------------------------------
// <copyright file="ListBox.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using Perspex.Controls.Generators;
    using Perspex.Controls.Primitives;

    public class ListBox : SelectingItemsControl
    {
        protected override ItemContainerGenerator CreateItemContainerGenerator()
        {
            return new TypedItemContainerGenerator<ListBoxItem>(this);
        }
    }
}
