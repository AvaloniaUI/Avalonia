#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeComWrappers
    {
        private static void** InitIScrollProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IScrollProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, Avalonia.Automation.Provider.ScrollAmount, Avalonia.Automation.Provider.ScrollAmount, int>)&IScrollProviderManagedWrapper.Scroll;
            vtbl[idx++] = (delegate* unmanaged<void*, double, double, int>)&IScrollProviderManagedWrapper.SetScrollPercent;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IScrollProviderManagedWrapper.GetHorizontalScrollPercent;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IScrollProviderManagedWrapper.GetVerticalScrollPercent;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IScrollProviderManagedWrapper.GetHorizontalViewSize;
            vtbl[idx++] = (delegate* unmanaged<void*, double*, int>)&IScrollProviderManagedWrapper.GetVerticalViewSize;
            vtbl[idx++] = (delegate* unmanaged<void*, bool*, int>)&IScrollProviderManagedWrapper.GetHorizontallyScrollable;
            vtbl[idx++] = (delegate* unmanaged<void*, bool*, int>)&IScrollProviderManagedWrapper.GetVerticallyScrollable;
            Debug.Assert(idx == IScrollProvider.VtblSize);
            entries[entryIndex].IID = IScrollProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }

        private static void** InitIScrollItemProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IScrollItemProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&IScrollItemProviderManagedWrapper.ScrollIntoView;
            Debug.Assert(idx == IScrollItemProvider.VtblSize);
            entries[entryIndex].IID = IScrollItemProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }
    }
}
#endif
