#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Automation;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeComWrappers<T>
    {
        private static void** InitIExpandCollapseProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers<T>),
                sizeof(void*) * IExpandCollapseProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&IExpandCollapseProviderManagedWrapper.Expand;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&IExpandCollapseProviderManagedWrapper.Collapse;
            vtbl[idx++] = (delegate* unmanaged<void*, ExpandCollapseState*, int>)&IExpandCollapseProviderManagedWrapper.GetExpandCollapseState;
            Debug.Assert(idx == IExpandCollapseProvider.VtblSize);
            entries[entryIndex].IID = IExpandCollapseProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }
    }
}
#endif
