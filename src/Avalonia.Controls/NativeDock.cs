namespace Avalonia.Controls
{
    public class NativeDock
    {
        public static readonly AttachedProperty<NativeMenu?> MenuProperty
            = AvaloniaProperty.RegisterAttached<NativeDock, AvaloniaObject, NativeMenu?>("Menu");

        public static void SetMenu(AvaloniaObject o, NativeMenu? menu) => o.SetValue(MenuProperty, menu);

        public static NativeMenu? GetMenu(AvaloniaObject o) => o.GetValue(MenuProperty);
    }
}
