using Avalonia.Input;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a tab in a <see cref="TabStrip"/>.
    /// </summary>
    public class TabStripItem : ListBoxItem
    {
        protected override void OnGotFocus(GotFocusEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateSelectionFromEvent(e);
        }
    }
}
