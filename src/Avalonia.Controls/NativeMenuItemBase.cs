using System;

namespace Avalonia.Controls
{
    public class NativeMenuItemBase : AvaloniaObject
    {
        private NativeMenu? _parent;

        internal NativeMenuItemBase()
        {

        }

        public static readonly DirectProperty<NativeMenuItemBase, NativeMenu?> ParentProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItemBase, NativeMenu?>(nameof(Parent), o => o.Parent);

        public NativeMenu? Parent
        {
            get => _parent;
            internal set => SetAndRaise(ParentProperty, ref _parent, value);
        }
    }
}
