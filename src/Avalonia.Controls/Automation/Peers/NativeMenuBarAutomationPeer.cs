using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace Avalonia.Automation.Peers
{
    public class NativeMenuBarAutomationPeer(NativeMenuBar owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.MenuBar;
        }

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var menu = Owner.GetVisualDescendants().OfType<MenuBase>().FirstOrDefault()
                       ?? Owner.GetLogicalDescendants().OfType<MenuBase>().FirstOrDefault();
            if (menu is null)
                return base.GetChildrenCore();

            var result = new List<AutomationPeer>();
            foreach (var logicalChild in menu.LogicalChildren)
            {
                if (logicalChild is MenuItem { IsVisible: true } menuItem)
                    result.Add(GetOrCreate(menuItem));
            }

            return result;
        }
    }
}
