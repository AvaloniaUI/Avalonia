using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IToggleProvider
    {
        ToggleState UIA.IToggleProvider.ToggleState => InvokeSync<IToggleProvider, ToggleState>(x => x.ToggleState);
        void UIA.IToggleProvider.Toggle() => InvokeSync<IToggleProvider>(x => x.Toggle());
    }

#if NET6_0_OR_GREATER

    internal unsafe partial class AutomationNodeWrapper : UIA.IToggleProvider
    {
        public void* IToggleProviderInst { get; init; }

        ToggleState UIA.IToggleProvider.ToggleState => UIA.IToggleProviderNativeWrapper.GetToggleState(IToggleProviderInst);

        void UIA.IToggleProvider.Toggle() => UIA.IToggleProviderNativeWrapper.Toggle(IToggleProviderInst);
    }
#endif
}
