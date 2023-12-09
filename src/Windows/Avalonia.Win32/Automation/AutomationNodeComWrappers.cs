#if NET6_0_OR_GREATER
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal unsafe partial class AutomationNodeComWrappers : ComWrappers
    {
        private const int AutomationNodeVtblLen = 13;
        private static readonly ComInterfaceEntry* s_vtbl;

        public static readonly AutomationNodeComWrappers Instance = new AutomationNodeComWrappers();

        static AutomationNodeComWrappers()
        {
            var idx = 0;
            var entries = (ComInterfaceEntry*)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(ComInterfaceEntry) * AutomationNodeVtblLen);

            InitIRawElementProviderSimpleVtbl(entries, ref idx);
            InitIRawElementProviderSimple2Vtbl(entries, ref idx);
            InitIRawElementProviderFragmentVtbl(entries, ref idx);
            InitIInvokeProviderVtbl(entries, ref idx);
            InitIExpandCollapseProviderVtbl(entries, ref idx);
            InitIRangeValueProviderVtbl(entries, ref idx);
            InitIScrollProviderVtbl(entries, ref idx);
            InitIScrollItemProviderVtbl(entries, ref idx);
            InitISelectionProviderVtbl(entries, ref idx);
            InitISelectionItemProviderVtbl(entries, ref idx);
            InitIToggleProviderVtbl(entries, ref idx);
            InitIValueProviderVtbl(entries, ref idx);
            InitIRawElementProviderFragmentRootVtbl(entries, ref idx);

            s_vtbl = entries;
        }

        protected override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (obj is AutomationNode)
            {
                count = AutomationNodeVtblLen;
                return s_vtbl;
            }

            count = 0;
            return default;
        }

        protected override object? CreateObject(IntPtr externalComObject, CreateObjectFlags flags)
        {
            return AutomationNodeWrapper.Create(externalComObject);
        }

        protected override void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }


        private static void** InitIRawElementProviderSimpleVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IRawElementProviderSimple.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, ProviderOptions*, int>)&IRawElementProviderSimpleManagedWrapper.GetProviderOptions;
            vtbl[idx++] = (delegate* unmanaged<void*, int, void**, int>)&IRawElementProviderSimpleManagedWrapper.GetPatternProvider;
            vtbl[idx++] = (delegate* unmanaged<void*, int, void**, int>)&IRawElementProviderSimpleManagedWrapper.GetPropertyValue;
            vtbl[idx++] = (delegate* unmanaged<void*, void**, int>)&IRawElementProviderSimpleManagedWrapper.GetHostRawElementProvider;
            Debug.Assert(idx == IRawElementProviderSimple.VtblSize);
            entries[entryIndex].IID = IRawElementProviderSimple.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }

        private static void** InitIRawElementProviderSimple2Vtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IRawElementProviderSimple2.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&IRawElementProviderSimple2ManagedWrapper.ShowContextMenu;
            Debug.Assert(idx == IRawElementProviderSimple2.VtblSize);
            entries[entryIndex].IID = IRawElementProviderSimple2.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }

        private static void** InitIRawElementProviderFragmentVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IRawElementProviderFragment.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, NavigateDirection, void**, int>)&IRawElementProviderFragmentManagedWrapper.Navigate;
            vtbl[idx++] = (delegate* unmanaged<void*, SAFEARRAY**, int>)&IRawElementProviderFragmentManagedWrapper.GetRuntimeId;
            vtbl[idx++] = (delegate* unmanaged<void*, Rect*, int>)&IRawElementProviderFragmentManagedWrapper.GetBoundingRectangle;
            vtbl[idx++] = (delegate* unmanaged<void*, SAFEARRAY**, int>)&IRawElementProviderFragmentManagedWrapper.GetEmbeddedFragmentRoots;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&IRawElementProviderFragmentManagedWrapper.SetFocus;
            vtbl[idx++] = (delegate* unmanaged<void*, void**, int>)&IRawElementProviderFragmentManagedWrapper.GetFragmentRoot;
            Debug.Assert(idx == IRawElementProviderFragment.VtblSize);
            entries[entryIndex].IID = IRawElementProviderFragment.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }

        private static void** InitIInvokeProviderVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IInvokeProvider.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, int>)&IInvokeProviderManagedWrapper.Invoke;
            Debug.Assert(idx == IInvokeProvider.VtblSize);
            entries[entryIndex].IID = IInvokeProvider.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }

        private static void** InitIRawElementProviderFragmentRootVtbl(ComInterfaceEntry* entries, ref int entryIndex)
        {
            GetIUnknownImpl(out var fpQueryInterface, out var fpAddRef, out var fpRelease);
            var vtbl = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(
                typeof(AutomationNodeComWrappers),
                sizeof(void*) * IRawElementProviderFragmentRoot.VtblSize);
            var idx = 0;
            vtbl[idx++] = (void*)fpQueryInterface;
            vtbl[idx++] = (void*)fpAddRef;
            vtbl[idx++] = (void*)fpRelease;
            vtbl[idx++] = (delegate* unmanaged<void*, double, double, void**, int>)&IRawElementProviderFragmentRootManagedWrapper.ElementProviderFromPoint;
            vtbl[idx++] = (delegate* unmanaged<void*, void**, int>)&IRawElementProviderFragmentRootManagedWrapper.GetFocus;
            Debug.Assert(idx == IRawElementProviderFragmentRoot.VtblSize);
            entries[entryIndex].IID = IRawElementProviderFragmentRoot.IID;
            entries[entryIndex++].Vtable = (IntPtr)vtbl;
            return vtbl;
        }
    }
}
#endif
