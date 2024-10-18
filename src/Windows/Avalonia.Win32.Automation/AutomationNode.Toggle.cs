using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IToggleProvider
    {
        ToggleState UIA.IToggleProvider.GetToggleState() => InvokeSync<IToggleProvider, ToggleState>(x => x.ToggleState);
        void UIA.IToggleProvider.Toggle() => InvokeSync<IToggleProvider>(x => x.Toggle());
    }
}
