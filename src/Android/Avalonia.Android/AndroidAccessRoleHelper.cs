using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Android
{
    internal static class AndroidAccessRoleHelper
    {
        private static readonly Dictionary<AutomationControlType, string> s_roles = new()
        {
            { AutomationControlType.Button, "android.widget.Button" },
            { AutomationControlType.Calendar, "android.widget.DatePicker" },
            { AutomationControlType.CheckBox, "android.widget.CheckBox" },
            { AutomationControlType.ComboBox, "android.widget.Spinner" },
            { AutomationControlType.Image, "android.widget.ImageView" },
            { AutomationControlType.List, "android.widget.ListView" },
            { AutomationControlType.ProgressBar, "android.widget.ProgressBar" },
            { AutomationControlType.RadioButton, "android.widget.RadioButton" },
            { AutomationControlType.ScrollViewer, "android.widget.ScrollView" },
            { AutomationControlType.Slider, "android.widget.SeekBar" },
            { AutomationControlType.SplitButton, "android.widget.Button" },
            { AutomationControlType.Tab, "android.widget.TabWidget" },
            { AutomationControlType.TabItem, "android.widget.TabWidget" },
            { AutomationControlType.Edit, "android.widget.EditText" },
            { AutomationControlType.Text, "android.widget.TextView" }
        };

        internal static string? GetClassName(AutomationPeer peer)
        {
            if (peer is ControlAutomationPeer controlPeer)
            {
                if (AutomationProperties.GetClassNameOverride(controlPeer.Owner) is string classNameOverride)
                {
                    return classNameOverride;
                }
                ;

                var controlType = controlPeer.Owner.GetType();
                var automationType = peer.GetAutomationControlType();
                s_roles.TryGetValue(automationType, out var role);

                return automationType switch
                {
                    AutomationControlType.Button when controlType.IsAssignableTo(typeof(ToggleSwitch)) => "android.widget.Switch",
                    AutomationControlType.Button or AutomationControlType.SplitButton when peer.GetProvider<IToggleProvider>() is not null => "android.widget.ToggleButton",
                    AutomationControlType.Spinner when controlType.IsAssignableTo(typeof(NumericUpDown)) => "android.widget.NumberPicker",
                    AutomationControlType.Custom when controlType.IsAssignableTo(typeof(DatePicker)) => "android.widget.DatePicker",
                    AutomationControlType.Button when controlType.IsAssignableTo(typeof(CalendarDatePicker)) => "android.widget.DatePicker",
                    AutomationControlType.Custom when controlType.IsAssignableTo(typeof(TimePicker)) => "android.widget.TimePicker",
                    _ => role ?? peer.GetClassName()
                };
            }
            return peer.GetClassName();
        }
    }
}
