using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IValueProvider
    {
        bool UIA.IValueProvider.IsReadOnly => InvokeSync<IValueProvider, bool>(x => x.IsReadOnly);
        string? UIA.IValueProvider.Value
        {
            get => InvokeSync<IValueProvider, string?>(x => x.Value);
            [param: MarshalAs(UnmanagedType.LPWStr)]
            set => InvokeSync<IValueProvider>(x => x.SetValue(value));
        }
    }

#if NET6_0_OR_GREATER
    internal unsafe partial class AutomationNodeWrapper : UIA.IValueProvider
    {
        public void* IValueProviderInst { get; init; }

        string? UIA.IValueProvider.Value
        {
            get => UIA.IValueProviderNativeWrapper.GetValue(IValueProviderInst);
            set => UIA.IValueProviderNativeWrapper.SetValue(IValueProviderInst, value);
        }

        bool UIA.IValueProvider.IsReadOnly => UIA.IValueProviderNativeWrapper.GetIsReadOnly(IValueProviderInst);
    }
#endif
}
