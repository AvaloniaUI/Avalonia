using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IValueProvider
    {
        bool UIA.IValueProvider.IsReadOnly => InvokeSync<IValueProvider, bool>(x => x.IsReadOnly);
        string? UIA.IValueProvider.Value => InvokeSync<IValueProvider, string?>(x => x.Value);

        void UIA.IValueProvider.SetValue([MarshalAs(UnmanagedType.LPWStr)] string? value)
        {
            InvokeSync<IValueProvider>(x => x.SetValue(value));
        }
    }
}
