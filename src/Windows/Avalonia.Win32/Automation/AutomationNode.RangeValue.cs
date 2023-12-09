using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IRangeValueProvider
    {
        double UIA.IRangeValueProvider.Value
        {
            get => InvokeSync<IRangeValueProvider, double>(x => x.Value);
            set => InvokeSync<IRangeValueProvider>(x => x.SetValue(value));
        }
        bool UIA.IRangeValueProvider.IsReadOnly => InvokeSync<IRangeValueProvider, bool>(x => x.IsReadOnly);
        double UIA.IRangeValueProvider.Maximum => InvokeSync<IRangeValueProvider, double>(x => x.Maximum);
        double UIA.IRangeValueProvider.Minimum => InvokeSync<IRangeValueProvider, double>(x => x.Minimum);
        double UIA.IRangeValueProvider.LargeChange => 1;
        double UIA.IRangeValueProvider.SmallChange => 1;
    }

#if NET6_0_OR_GREATER

    internal unsafe partial class AutomationNodeWrapper : UIA.IRangeValueProvider
    {
        public void* IRangeValueProviderInst { get; init; }

        double UIA.IRangeValueProvider.Value
        {
            get => UIA.IRangeValueProviderNativeWrapper.GetValue(IRangeValueProviderInst);
            set => UIA.IRangeValueProviderNativeWrapper.SetValue(IRangeValueProviderInst, value);
        }

        bool UIA.IRangeValueProvider.IsReadOnly => UIA.IRangeValueProviderNativeWrapper.GetIsReadOnly(IRangeValueProviderInst);

        double UIA.IRangeValueProvider.Maximum => UIA.IRangeValueProviderNativeWrapper.GetMaximum(IRangeValueProviderInst);

        double UIA.IRangeValueProvider.Minimum => UIA.IRangeValueProviderNativeWrapper.GetMinimum(IRangeValueProviderInst);

        double UIA.IRangeValueProvider.LargeChange => UIA.IRangeValueProviderNativeWrapper.GetLargeChange(IRangeValueProviderInst);

        double UIA.IRangeValueProvider.SmallChange => UIA.IRangeValueProviderNativeWrapper.GetSmallChange(IRangeValueProviderInst);
    }
#endif
}
