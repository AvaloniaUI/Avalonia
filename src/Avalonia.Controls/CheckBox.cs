using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace Avalonia.Controls
{
    /// <summary>
    /// A check box control.
    /// </summary>
    public class CheckBox : ToggleButton
    {
        static CheckBox()
        {
            AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<CheckBox>(AutomationControlType.CheckBox);
        }
    }
}
