using System;

namespace Avalonia.Controls
{
    public class NativeMenuItemBase : AvaloniaObject
    {
        private NativeMenu _parent;

        internal NativeMenuItemBase()
        {

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
