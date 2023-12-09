﻿#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeComWrappers
    {
        private static void** InitIValueProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IValueProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, void*, int>)&IValueProviderManagedWrapper.SetValue;
            vtbl[idx++] = (delegate* unmanaged<void*, void**, int>)&IValueProviderManagedWrapper.GetValue;
            vtbl[idx++] = (delegate* unmanaged<void*, bool*, int>)&IValueProviderManagedWrapper.GetIsReadOnly;
            Debug.Assert(idx == IValueProvider.VtblSize);
            entries[entryIndex].IID = IValueProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }
    }
}
#endif