using System;

namespace Avalonia.Controls
{
    public class NativeMenuItemBase : AvaloniaObject
    {
        private NativeMenu _menu;
        private NativeMenu _parent;

        static NativeMenuItemBase()
        {
            MenuProperty.Changed.Subscribe(args =>
            {
                var item = (NativeMenuItem)args.Sender;
                var value = (NativeMenu)args.NewValue;
                if (value.Parent != null && value.Parent != item)
                    throw new InvalidOperationException("NativeMenu already has a parent");
                value.Parent = item;
            });
        }

        internal NativeMenuItemBase()
        {

        }

        public static readonly DirectProperty<NativeMenuItem, NativeMenu> MenuProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, NativeMenu>(nameof(Menu), o => o._menu,
                (o, v) =>
                {
                    if (v.Parent != null && v.Parent != o)
                        throw new InvalidOperationException("NativeMenu already has a parent");
                    o._menu = v;
                });

        public NativeMenu Menu
        {
            get => _menu;
            set
            {
                if (value.Parent != null && value.Parent != this)
                    throw new InvalidOperationException("NativeMenu already has a parent");
                SetAndRaise(MenuProperty, ref _menu, value);
            }
        }

        public static readonly DirectProperty<NativeMenuItem, NativeMenu> ParentProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, NativeMenu>("Parent", o => o.Parent, (o, v) => o.Parent = v);

        public NativeMenu Parent
        {
            get => _parent;
            set => SetAndRaise(ParentProperty, ref _parent, value);
        }
    }
}
