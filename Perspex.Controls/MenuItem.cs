// -----------------------------------------------------------------------
// <copyright file="MenuItem.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Linq;
    using System.Windows.Input;
    using Perspex.Controls.Primitives;
    using Perspex.Input;
    using Perspex.LogicalTree;
    using Perspex.Collections;
    using Perspex.Rendering;
    using Perspex.Controls.Templates;

    public class MenuItem : HeaderedItemsControl, IMenu
    {
        public static readonly PerspexProperty<ICommand> CommandProperty =
            Button.CommandProperty.AddOwner<MenuItem>();

        public static readonly PerspexProperty<object> CommandParameterProperty =
            Button.CommandParameterProperty.AddOwner<MenuItem>();

        public static readonly PerspexProperty<object> IconProperty =
            PerspexProperty.Register<MenuItem, object>("Icon");

        public static readonly PerspexProperty<bool> IsSubMenuOpenProperty =
            PerspexProperty.Register<MenuItem, bool>("IsSubMenuOpen");

        static MenuItem()
        {
            IsSubMenuOpenProperty.Changed.Subscribe(SubMenuOpenChanged);
        }

        public ICommand Command
        {
            get { return this.GetValue(CommandProperty); }
            set { this.SetValue(CommandProperty, value); }
        }

        public object CommandParameter
        {
            get { return this.GetValue(CommandParameterProperty); }
            set { this.SetValue(CommandParameterProperty, value); }
        }

        public object Icon
        {
            get { return this.GetValue(IconProperty); }
            set { this.SetValue(IconProperty, value); }
        }

        public bool IsSubMenuOpen
        {
            get { return this.GetValue(IsSubMenuOpenProperty); }
            set { this.SetValue(IsSubMenuOpenProperty, value); }
        }

        void IMenu.ChildPointerEnter(MenuItem item)
        {
        }

        void IMenu.ChildSubMenuOpened(MenuItem item)
        {
            foreach (var i in this.Items.Cast<object>().OfType<MenuItem>())
            {
                i.IsSubMenuOpen = i == item;
            }
        }

        protected override void OnPointerEnter(PointerEventArgs e)
        {
            base.OnPointerEnter(e);
            this.GetLogicalParent<IMenu>()?.ChildPointerEnter(this);
        }

        protected override void OnPointerPressed(PointerPressEventArgs e)
        {
            base.OnPointerPressed(e);

            if (!this.Classes.Contains(":empty"))
            {
                this.IsSubMenuOpen = !this.IsSubMenuOpen;
            }
        }

        private void OnSubMenuOpenChanged(bool open)
        {
            if (!open && this.Items != null)
            {
                foreach (var item in this.Items.Cast<object>().OfType<MenuItem>())
                {
                    item.IsSubMenuOpen = false;
                }
            }
            else if (open)
            {
                // TODO: This is broken, meaning that a previous submenu isn't closed when a new 
                // one opens. This is because each menu item is in a separate visual tree to its 
                // parent due to its being contained in a Popup and parenting/templating is 
                // broken across visual trees.
                this.GetLogicalParent<IMenu>()?.ChildSubMenuOpened(this);
            }
        }

        private static void SubMenuOpenChanged(PerspexPropertyChangedEventArgs e)
        {
            var sender = e.Sender as MenuItem;

            if (sender != null)
            {
                sender.OnSubMenuOpenChanged((bool)e.NewValue);
            }
        }
    }
}
