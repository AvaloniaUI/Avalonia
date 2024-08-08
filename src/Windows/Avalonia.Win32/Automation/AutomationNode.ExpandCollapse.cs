using Avalonia.Automation;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IExpandCollapseProvider
    {
        ExpandCollapseState UIA.IExpandCollapseProvider.ExpandCollapseState
        {
            get => InvokeSync<IExpandCollapseProvider, ExpandCollapseState>(x => x.ExpandCollapseState);
        }

        void UIA.IExpandCollapseProvider.Expand() => InvokeSync<IExpandCollapseProvider>(x => x.Expand());
        void UIA.IExpandCollapseProvider.Collapse() => InvokeSync<IExpandCollapseProvider>(x => x.Collapse());
    }

#if NET6_0_OR_GREATER

    internal unsafe partial class AutomationNodeWrapper : UIA.IExpandCollapseProvider
    {
        public void* IExpandCollapseProviderInst { get; init; }

        ExpandCollapseState UIA.IExpandCollapseProvider.ExpandCollapseState => UIA.IExpandCollapseProviderNativeWrapper.GetExpandCollapseState(IExpandCollapseProviderInst);

        void UIA.IExpandCollapseProvider.Collapse() => UIA.IExpandCollapseProviderNativeWrapper.Collapse(IExpandCollapseProviderInst);

        void UIA.IExpandCollapseProvider.Expand() => UIA.IExpandCollapseProviderNativeWrapper.Expand(IExpandCollapseProviderInst);
    }
#endif
}
