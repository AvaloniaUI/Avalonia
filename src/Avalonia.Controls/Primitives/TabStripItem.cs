using Avalonia.Input;

namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Represents a tab in a <see cref="TabStrip"/>.
    /// </summary>
    public class TabStripItem : ListBoxItem
    {
        protected override void OnGotFocus(FocusChangedEventArgs e)
        {
            base.OnGotFocus(e);
            UpdateSelectionFromEvent(e);
        }
    }
}
