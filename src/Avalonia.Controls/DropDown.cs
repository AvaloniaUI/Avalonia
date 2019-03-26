using System;
using Avalonia.Logging;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    [Obsolete("Use ComboBox")]
    public class DropDown : ComboBox, IStyleable
    {
        public DropDown()
        {
            Logger.Warning(LogArea.Control, this, "DropDown is deprecated: Use ComboBox");
        }

        Type IStyleable.StyleKey => typeof(ComboBox);
    }

    [Obsolete("Use ComboBoxItem")]
    public class DropDownItem : ComboBoxItem, IStyleable
    {
        public DropDownItem()
        {
            Logger.Warning(LogArea.Control, this, "DropDownItem is deprecated: Use ComboBoxItem");
        }

        Type IStyleable.StyleKey => typeof(ComboBoxItem);
    }
}
