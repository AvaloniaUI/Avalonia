// -----------------------------------------------------------------------
// <copyright file="Menu.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Perspex.LogicalTree;

    public class Menu : ItemsControl, IMenu
    {
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal });

        static Menu()
        {
            ItemsPanelProperty.OverrideDefaultValue(typeof(Menu), DefaultPanel);
        }

        void IMenu.ChildPointerEnter(MenuItem item)
        {
            var children = this.GetLogicalChildren().Cast<MenuItem>();

            if (children.Any(x => x.IsSubMenuOpen))
            {
                foreach (MenuItem i in this.GetLogicalChildren())
                {
                    i.IsSubMenuOpen = i == item;
                }
            }
        }
    }
}
