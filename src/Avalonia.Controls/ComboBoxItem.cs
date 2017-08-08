using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// A selectable item in a <see cref="ComboBox"/>.
    /// </summary>
    public class ComboBoxItem : ListBoxItem
    {
        public override string ToString()
        {
            if (Content == null)
            {
                return String.Empty;
            }

            return Content.ToString();
        }
    }
}
