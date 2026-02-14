namespace Avalonia.Controls
{
    public static class NativeDock
    {
        public static readonly AttachedProperty<NativeMenu?> MenuProperty =
            AvaloniaProperty.RegisterAttached<AvaloniaObject, NativeMenu?>("Menu", typeof(NativeDock));

        public static void SetMenu(AvaloniaObject o, NativeMenu? menu) => o.SetValue(MenuProperty, menu);

        public static NativeMenu? GetMenu(AvaloniaObject o) => o.GetValue(MenuProperty);
    }
}
