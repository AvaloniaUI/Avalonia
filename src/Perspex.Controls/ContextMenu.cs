namespace Perspex.Controls
{
    using Primitives;

    public class ContextMenu : SelectingItemsControl
    {
        public static readonly AttachedProperty<ContextMenu> MenuProperty =
       PerspexProperty.RegisterAttached<ContextMenu, TextBlock, ContextMenu>("Menu");

        public static ContextMenu GetMenu(TextBlock element)
        {
            return element.GetValue(MenuProperty);
        }

        public static void SetMenu(TextBlock element, ContextMenu value)
        {
            element.SetValue(MenuProperty, value);
        }
    }
}
