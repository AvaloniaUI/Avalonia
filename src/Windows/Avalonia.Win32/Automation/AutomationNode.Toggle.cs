using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IToggleProvider
    {
        ToggleState UIA.IToggleProvider.ToggleState => InvokeSync<IToggleProvider, ToggleState>(x => x.ToggleState);
        void UIA.IToggleProvider.Toggle() => InvokeSync<IToggleProvider>(x => x.Toggle());
    }
}
