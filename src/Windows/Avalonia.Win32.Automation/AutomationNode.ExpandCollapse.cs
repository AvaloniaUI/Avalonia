using Avalonia.Automation;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IExpandCollapseProvider
    {
        ExpandCollapseState UIA.IExpandCollapseProvider.GetExpandCollapseState()
        {
            return InvokeSync<IExpandCollapseProvider, ExpandCollapseState>(x => x.ExpandCollapseState);
        }

        void UIA.IExpandCollapseProvider.Expand() => InvokeSync<IExpandCollapseProvider>(x => x.Expand());
        void UIA.IExpandCollapseProvider.Collapse() => InvokeSync<IExpandCollapseProvider>(x => x.Collapse());
    }
}
