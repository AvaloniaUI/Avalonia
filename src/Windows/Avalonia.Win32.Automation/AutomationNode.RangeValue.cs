using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IRangeValueProvider
    {
        double UIA.IRangeValueProvider.GetValue() => InvokeSync<IRangeValueProvider, double>(x => x.Value);
        bool UIA.IRangeValueProvider.GetIsReadOnly() => InvokeSync<IRangeValueProvider, bool>(x => x.IsReadOnly);
        double UIA.IRangeValueProvider.GetMaximum() => InvokeSync<IRangeValueProvider, double>(x => x.Maximum);
        double UIA.IRangeValueProvider.GetMinimum() => InvokeSync<IRangeValueProvider, double>(x => x.Minimum);
        double UIA.IRangeValueProvider.GetLargeChange() => 1;
        double UIA.IRangeValueProvider.GetSmallChange() => 1;

        public void SetValue(double value) => InvokeSync<IRangeValueProvider>(x => x.SetValue(value));
    }
}
