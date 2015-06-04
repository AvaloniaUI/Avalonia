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
    using Perspex.VisualTree;
    using Perspex.Interactivity;

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

        public static readonly RoutedEvent<RoutedEventArgs> ClickEvent =
            RoutedEvent.Register<MenuItem, RoutedEventArgs>("Click", RoutingStrategies.Bubble);

        static MenuItem()
        {
            ClickEvent.AddClassHandler<MenuItem>(x => x.OnClick);
            IsSubMenuOpenProperty.Changed.Subscribe(SubMenuOpenChanged);
        }

        public event EventHandler<RoutedEventArgs> Click
        {
            add { this.AddHandler(ClickEvent, value); }
            remove { this.RemoveHandler(ClickEvent, value); }
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

        void IMenu.CloseMenu()
        {
            this.IsSubMenuOpen = false;
            this.GetParentMenu().CloseMenu();
        }

        protected virtual void OnClick(RoutedEventArgs e)
        {
            if (this.Command != null)
            {
                this.Command.Execute(this.CommandParameter);
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

            if (this.Classes.Contains(":empty"))
            {
                RoutedEventArgs click = new RoutedEventArgs
                {
                    RoutedEvent = ClickEvent,
                };

                this.RaiseEvent(click);
                this.GetParentMenu().CloseMenu();
            }
            else
            {
                this.IsSubMenuOpen = !this.IsSubMenuOpen;
            }
        }

        private IMenu GetParentMenu()
        {
            var parent = this.GetLogicalParent<IMenu>();

            if (parent != null)
            {
                return parent;
            }
            else
            {
                var popupRoot = this.GetVisualAncestors().OfType<PopupRoot>().FirstOrDefault();
                var parentItem = ((ILogical)popupRoot).GetLogicalParent<Popup>().TemplatedParent;
                return (IMenu)parentItem;
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
                this.GetParentMenu().ChildSubMenuOpened(this);
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
