using Avalonia.Automation.Peers;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides the base class for defining a new control that encapsulates related existing controls and provides its own logic.
    /// </summary>
    public class UserControl : ContentControl
    {
        protected override AutomationPeer OnCreateAutomationPeer() => new UserControlAutomationPeer(this);
    }
}
