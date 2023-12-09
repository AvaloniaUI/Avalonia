#if NET6_0_OR_GREATER
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeComWrappers<T>
    {
        private static void** InitISelectionProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers<T>),
                sizeof(void*) * ISelectionProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = typeof(T) == typeof(AutomationNode)
                ? (delegate* unmanaged<void*, SAFEARRAY**, int>)&ISelectionProviderManagedWrapper.GetSelection_AutomationNode
                : (delegate* unmanaged<void*, SAFEARRAY**, int>)&ISelectionProviderManagedWrapper.GetSelection_RootAutomationNode;
            vtbl[idx++] = (delegate* unmanaged<void*, bool*, int>)&ISelectionProviderManagedWrapper.GetCanSelectMultiple;
            vtbl[idx++] = (delegate* unmanaged<void*, bool*, int>)&ISelectionProviderManagedWrapper.GetIsSelectionRequired;
            Debug.Assert(idx == ISelectionProvider.VtblSize);
            entries[entryIndex].IID = ISelectionProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }

        private static void** InitISelectionItemProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers<T>),
                sizeof(void*) * ISelectionItemProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&ISelectionItemProviderManagedWrapper.Select;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&ISelectionItemProviderManagedWrapper.AddToSelection;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&ISelectionItemProviderManagedWrapper.RemoveFromSelection;
            vtbl[idx++] = (delegate* unmanaged<void*, bool*, int>)&ISelectionItemProviderManagedWrapper.GetIsSelected;
            vtbl[idx++] = typeof(T) == typeof(AutomationNode)
                ? (delegate* unmanaged<void*, void**, int>)&ISelectionItemProviderManagedWrapper.GetSelectionContainer_AutomationNode
                : (delegate* unmanaged<void*, void**, int>)&ISelectionItemProviderManagedWrapper.GetSelectionContainer_RootAutomationNode;
            Debug.Assert(idx == ISelectionItemProvider.VtblSize);
            entries[entryIndex].IID = ISelectionItemProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }
    }
}
#endif
