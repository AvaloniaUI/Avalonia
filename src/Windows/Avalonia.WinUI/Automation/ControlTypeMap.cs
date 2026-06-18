using AvControlType = global::Avalonia.Automation.Peers.AutomationControlType;
using XamlControlType = Microsoft.UI.Xaml.Automation.Peers.AutomationControlType;

namespace Avalonia.WinUI.Automation;

internal static class ControlTypeMap
{
    public static XamlControlType ToXaml(AvControlType type) => type switch
    {
        AvControlType.Button => XamlControlType.Button,
        AvControlType.Calendar => XamlControlType.Calendar,
        AvControlType.CheckBox => XamlControlType.CheckBox,
        AvControlType.ComboBox => XamlControlType.ComboBox,
        AvControlType.ComboBoxItem => XamlControlType.ListItem,
        AvControlType.Edit => XamlControlType.Edit,
        AvControlType.Hyperlink => XamlControlType.Hyperlink,
        AvControlType.Image => XamlControlType.Image,
        AvControlType.ListItem => XamlControlType.ListItem,
        AvControlType.List => XamlControlType.List,
        AvControlType.Menu => XamlControlType.Menu,
        AvControlType.MenuBar => XamlControlType.MenuBar,
        AvControlType.MenuItem => XamlControlType.MenuItem,
        AvControlType.ProgressBar => XamlControlType.ProgressBar,
        AvControlType.RadioButton => XamlControlType.RadioButton,
        AvControlType.ScrollBar => XamlControlType.ScrollBar,
        AvControlType.Slider => XamlControlType.Slider,
        AvControlType.Spinner => XamlControlType.Spinner,
        AvControlType.StatusBar => XamlControlType.StatusBar,
        AvControlType.Tab => XamlControlType.Tab,
        AvControlType.TabItem => XamlControlType.TabItem,
        AvControlType.Text => XamlControlType.Text,
        AvControlType.ToolBar => XamlControlType.ToolBar,
        AvControlType.ToolTip => XamlControlType.ToolTip,
        AvControlType.Tree => XamlControlType.Tree,
        AvControlType.TreeItem => XamlControlType.TreeItem,
        AvControlType.Group => XamlControlType.Group,
        AvControlType.Thumb => XamlControlType.Thumb,
        AvControlType.DataGrid => XamlControlType.DataGrid,
        AvControlType.DataItem => XamlControlType.DataItem,
        AvControlType.Document => XamlControlType.Document,
        AvControlType.SplitButton => XamlControlType.SplitButton,
        AvControlType.Window => XamlControlType.Window,
        AvControlType.Pane => XamlControlType.Pane,
        AvControlType.Header => XamlControlType.Header,
        AvControlType.HeaderItem => XamlControlType.HeaderItem,
        AvControlType.Table => XamlControlType.Table,
        AvControlType.TitleBar => XamlControlType.TitleBar,
        AvControlType.Separator => XamlControlType.Separator,
        AvControlType.Expander => XamlControlType.Group,
        _ => XamlControlType.Custom,
    };
}
