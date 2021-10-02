using System;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Selection;

#nullable enable

namespace Avalonia.Automation.Peers
{
    public class ComboBoxItemAutomationPeer : ListItemAutomationPeer
    {
        public ComboBoxItemAutomationPeer(ComboBoxItem owner)
            : base(owner)
        {
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ComboBoxItem;
        }
    }
}
