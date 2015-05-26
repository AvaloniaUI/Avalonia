// -----------------------------------------------------------------------
// <copyright file="Menu.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    public class Menu : ItemsControl
    {
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal });

        static Menu()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Menu), DefaultPanel);
        }
    }
}
