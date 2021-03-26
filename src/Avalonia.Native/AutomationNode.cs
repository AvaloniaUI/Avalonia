using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Platform;
using Avalonia.Native.Interop;

#nullable enable

namespace Avalonia.Native
{
    internal class AutomationNode : IRootAutomationNode
    {
        public AutomationNode(AutomationNodeFactory factory, IAvnAutomationNode native)
        {
            Native = native;
            Factory = factory;
        }

        public IAvnAutomationNode Native { get; }
        public IAutomationNodeFactory Factory { get; }

        public void ChildrenChanged() => Native.ChildrenChanged();

        public void PropertyChanged(AutomationProperty property, object? oldValue, object? newValue)
        {
            // TODO
        }

        public void FocusChanged(AutomationPeer? focus)
        {
            // TODO
        }
    }
}
