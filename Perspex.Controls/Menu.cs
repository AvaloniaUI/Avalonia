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
    using System.Reactive.Disposables;

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

        void IMenu.ChildSubMenuOpened(MenuItem item)
        {
            foreach (MenuItem i in this.GetLogicalChildren())
            {
                i.IsSubMenuOpen = i == item;
            }
        }

        void IMenu.CloseMenu()
        {
            foreach (MenuItem i in this.GetLogicalChildren())
            {
                i.IsSubMenuOpen = false;
            }
        }

        protected override void OnAttachedToVisualTree(IRenderRoot root)
        {
            base.OnAttachedToVisualTree(root);

            var topLevel = root as TopLevel;

            topLevel.Deactivated += this.Deactivated;

            this.subscription = new CompositeDisposable(
                topLevel.AddHandler(
                    InputElement.PointerPressedEvent,
                    this.Deactivated,
                    Interactivity.RoutingStrategies.Tunnel),
                Disposable.Create(() => topLevel.Deactivated -= this.Deactivated));
        }

        protected override void OnDetachedFromVisualTree(IRenderRoot oldRoot)
        {
            base.OnDetachedFromVisualTree(oldRoot);
            this.subscription.Dispose();
        }

        private void Deactivated(object sender, EventArgs e)
        {
            foreach (var i in this.GetLogicalChildren().Cast<MenuItem>())
            {
                i.IsSubMenuOpen = false;               
            }
        }
    }
}
