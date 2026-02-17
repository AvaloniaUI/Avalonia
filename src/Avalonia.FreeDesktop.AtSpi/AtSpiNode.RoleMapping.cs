using Avalonia.Automation.Peers;

namespace Avalonia.FreeDesktop.AtSpi
{
    internal partial class AtSpiNode
    {
        public static AtSpiRole ToAtSpiRole(AutomationControlType controlType)
        {
            return controlType switch
            {
                AutomationControlType.None => AtSpiRole.Panel,
                AutomationControlType.Button => AtSpiRole.PushButton,
                AutomationControlType.Calendar => AtSpiRole.Calendar,
                AutomationControlType.CheckBox => AtSpiRole.CheckBox,
                AutomationControlType.ComboBox => AtSpiRole.ComboBox,
                AutomationControlType.ComboBoxItem => AtSpiRole.ListItem,
                AutomationControlType.Edit => AtSpiRole.Entry,
                AutomationControlType.Hyperlink => AtSpiRole.Link,
                AutomationControlType.Image => AtSpiRole.Image,
                AutomationControlType.ListItem => AtSpiRole.ListItem,
                AutomationControlType.List => AtSpiRole.List,
                AutomationControlType.Menu => AtSpiRole.Menu,
                AutomationControlType.MenuBar => AtSpiRole.MenuBar,
                AutomationControlType.MenuItem => AtSpiRole.MenuItem,
                AutomationControlType.ProgressBar => AtSpiRole.ProgressBar,
                AutomationControlType.RadioButton => AtSpiRole.RadioButton,
                AutomationControlType.ScrollBar => AtSpiRole.ScrollBar,
                AutomationControlType.Slider => AtSpiRole.Slider,
                AutomationControlType.Spinner => AtSpiRole.SpinButton,
                AutomationControlType.StatusBar => AtSpiRole.StatusBar,
                AutomationControlType.Tab => AtSpiRole.PageTabList,
                AutomationControlType.TabItem => AtSpiRole.PageTab,
                AutomationControlType.Text => AtSpiRole.Label,
                AutomationControlType.ToolBar => AtSpiRole.ToolBar,
                AutomationControlType.ToolTip => AtSpiRole.ToolTip,
                AutomationControlType.Tree => AtSpiRole.Tree,
                AutomationControlType.TreeItem => AtSpiRole.TreeItem,
                AutomationControlType.Custom => AtSpiRole.Unknown,
                AutomationControlType.Group => AtSpiRole.Panel,
                AutomationControlType.Thumb => AtSpiRole.PushButton,
                AutomationControlType.DataGrid => AtSpiRole.TreeTable,
                AutomationControlType.DataItem => AtSpiRole.TableCell,
                AutomationControlType.Document => AtSpiRole.Document,
                AutomationControlType.SplitButton => AtSpiRole.PushButton,
                AutomationControlType.Window => AtSpiRole.Frame,
                AutomationControlType.Pane => AtSpiRole.Panel,
                AutomationControlType.Header => AtSpiRole.Header,
                AutomationControlType.HeaderItem => AtSpiRole.ColumnHeader,
                AutomationControlType.Table => AtSpiRole.Table,
                AutomationControlType.TitleBar => AtSpiRole.TitleBar,
                AutomationControlType.Separator => AtSpiRole.Separator,
                AutomationControlType.Expander => AtSpiRole.Panel,
                _ => AtSpiRole.Unknown,
            };
        }

        public static string ToAtSpiRoleName(AtSpiRole role)
        {
            return role switch
            {
                AtSpiRole.Application => "application",
                AtSpiRole.Frame => "frame",
                AtSpiRole.PushButton => "push button",
                AtSpiRole.CheckBox => "check box",
                AtSpiRole.ComboBox => "combo box",
                AtSpiRole.Entry => "entry",
                AtSpiRole.Label => "label",
                AtSpiRole.Image => "image",
                AtSpiRole.List => "list",
                AtSpiRole.ListItem => "list item",
                AtSpiRole.Menu => "menu",
                AtSpiRole.MenuBar => "menu bar",
                AtSpiRole.MenuItem => "menu item",
                AtSpiRole.ProgressBar => "progress bar",
                AtSpiRole.RadioButton => "radio button",
                AtSpiRole.ScrollBar => "scroll bar",
                AtSpiRole.Slider => "slider",
                AtSpiRole.SpinButton => "spin button",
                AtSpiRole.StatusBar => "status bar",
                AtSpiRole.PageTab => "page tab",
                AtSpiRole.PageTabList => "page tab list",
                AtSpiRole.ToolBar => "tool bar",
                AtSpiRole.ToolTip => "tool tip",
                AtSpiRole.Tree => "tree",
                AtSpiRole.TreeItem => "tree item",
                AtSpiRole.Panel => "panel",
                AtSpiRole.Separator => "separator",
                AtSpiRole.Table => "table",
                AtSpiRole.TableCell => "table cell",
                AtSpiRole.TreeTable => "tree table",
                AtSpiRole.ColumnHeader => "column header",
                AtSpiRole.Header => "header",
                AtSpiRole.TitleBar => "title bar",
                AtSpiRole.Document => "document frame",
                AtSpiRole.Link => "link",
                AtSpiRole.Calendar => "calendar",
                AtSpiRole.Window => "window",
                AtSpiRole.Unknown => "unknown",
                _ => "unknown",
            };
        }
    }
}
