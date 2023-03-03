using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IRangeValueProvider
    {
        double UIA.IRangeValueProvider.Value => InvokeSync<IRangeValueProvider, double>(x => x.Value);
        bool UIA.IRangeValueProvider.IsReadOnly => InvokeSync<IRangeValueProvider, bool>(x => x.IsReadOnly);
        double UIA.IRangeValueProvider.Maximum => InvokeSync<IRangeValueProvider, double>(x => x.Maximum);
        double UIA.IRangeValueProvider.Minimum => InvokeSync<IRangeValueProvider, double>(x => x.Minimum);
        double UIA.IRangeValueProvider.LargeChange => 1;
        double UIA.IRangeValueProvider.SmallChange => 1;

        public void SetValue(double value) => InvokeSync<IRangeValueProvider>(x => x.SetValue(value));
    }
}
