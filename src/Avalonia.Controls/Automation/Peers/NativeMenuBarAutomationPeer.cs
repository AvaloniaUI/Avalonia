using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
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
            var menu = Owner.VisualChildren.OfType<MenuBase>().FirstOrDefault() ??
                Owner.GetVisualDescendants().OfType<MenuBase>().FirstOrDefault();

            if (menu is null)
            {
                return base.GetChildrenCore();
            }

            var children = menu.GetLogicalChildren()
                .OfType<Control>()
                .Where(x => x.IsVisible)
                .Select(GetOrCreate)
                .ToList();

            return children.Count > 0 ? children : base.GetChildrenCore();
        }
    }
}
