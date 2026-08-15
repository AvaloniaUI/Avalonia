using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Avalonia.Win32.Automation.Interop;

[GeneratedComInterface(Options = ComInterfaceOptions.ManagedObjectWrapper)]
[Guid("36dc7aef-33e6-4691-afe1-2be7274b3d33")]
internal partial interface IRangeValueProvider
{
    void SetValue(double value);
    double GetValue();

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetIsReadOnly();

    double GetMaximum();
    double GetMinimum();
    double GetLargeChange();
    double GetSmallChange();
}
