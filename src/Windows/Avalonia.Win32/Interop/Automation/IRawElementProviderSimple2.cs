using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("a0a839a9-8da1-4a82-806a-8e0d44e79f56")]
    public interface IRawElementProviderSimple2 : IRawElementProviderSimple
    {
#if NET6_0_OR_GREATER
        public new static readonly Guid IID = new("a0a839a9-8da1-4a82-806a-8e0d44e79f56");
        public new const int VtblSize = IRawElementProviderSimple.VtblSize + 1;
#endif
        void ShowContextMenu();
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IRawElementProviderSimple2ManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int GetProviderOptions(void* @this, ProviderOptions* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple2>((ComWrappers.ComInterfaceDispatch*)@this).ProviderOptions;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetPatternProvider(void* @this, int patternId, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple2>((ComWrappers.ComInterfaceDispatch*)@this).GetPatternProvider(patternId);
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetPropertyValue(void* @this, int propertyId, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple2>((ComWrappers.ComInterfaceDispatch*)@this).GetPropertyValue(propertyId);
                Marshal.GetNativeVariantForObject(obj, (IntPtr)ret);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetHostRawElementProvider(void* @this, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple2>((ComWrappers.ComInterfaceDispatch*)@this).HostRawElementProvider;
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int ShowContextMenu(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderSimple2>((ComWrappers.ComInterfaceDispatch*)@this).ShowContextMenu();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IRawElementProviderSimple2NativeWrapper : IRawElementProviderSimple2
    {
        public static void ShowContextMenu(void* @this) => AutomationNodeWrapper.Invoke(@this, 7);

        ProviderOptions IRawElementProviderSimple.ProviderOptions => IRawElementProviderSimpleNativeWrapper.GetProviderOptions(((AutomationNodeWrapper)this).IRawElementProviderSimple2Inst);

        object? IRawElementProviderSimple.GetPatternProvider(int patternId) => IRawElementProviderSimpleNativeWrapper.GetPatternProvider((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderSimple2Inst, patternId);

        object? IRawElementProviderSimple.GetPropertyValue(int propertyId) => IRawElementProviderSimpleNativeWrapper.GetPropertyValue((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderSimple2Inst, propertyId);

        IRawElementProviderSimple? IRawElementProviderSimple.HostRawElementProvider => IRawElementProviderSimpleNativeWrapper.GetHostRawElementProvider((AutomationNodeWrapper)this, ((AutomationNodeWrapper)this).IRawElementProviderSimple2Inst);

        void IRawElementProviderSimple2.ShowContextMenu() => ShowContextMenu(((AutomationNodeWrapper)this).IRawElementProviderSimple2Inst);
    }
#endif
}
