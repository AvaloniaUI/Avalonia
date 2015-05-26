// -----------------------------------------------------------------------
// <copyright file="Menu.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using Perspex.Input;
    using Perspex.LogicalTree;
    using Perspex.Rendering;

    public class Menu : ItemsControl, IMenu
    {
        private static readonly ItemsPanelTemplate DefaultPanel =
            new ItemsPanelTemplate(() => new StackPanel { Orientation = Orientation.Horizontal });

        private IDisposable subscription;

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

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            var r = root as IInputElement;
            this.subscription = r.AddHandler(
                InputElement.PointerPressedEvent, 
                this.RootPointerPressed, 
                Interactivity.RoutingStrategies.Tunnel);
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);
            this.subscription.Dispose();
        }

        private void RootPointerPressed(object sender, PointerPressEventArgs e)
        {
            foreach (var i in this.GetLogicalChildren().Cast<MenuItem>())
            {
                i.IsSubMenuOpen = false;
            }
        }
    }
}
