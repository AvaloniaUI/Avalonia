#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeComWrappers<T>
    {
        private static void** InitIRangeValueProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers<T>),
                sizeof(void*) * IRangeValueProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, double, int>)&IRangeValueProviderManagedWrapper.SetValue;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IRangeValueProviderManagedWrapper.GetValue;
            vtbl[idx++] = (delegate* unmanaged<void*, bool*, int>)&IRangeValueProviderManagedWrapper.GetIsReadOnly;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IRangeValueProviderManagedWrapper.GetMaximum;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IRangeValueProviderManagedWrapper.GetMinimum;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IRangeValueProviderManagedWrapper.GetLargeChange;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IRangeValueProviderManagedWrapper.GetSmallChange;
            Debug.Assert(idx == IRangeValueProvider.VtblSize);
            entries[entryIndex].IID = IRangeValueProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }
    }
}
#endif
