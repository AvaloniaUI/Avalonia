using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IRawElementProviderFragmentRoot
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("620ce2a5-ab8f-40a9-86cb-de3c75599b58");
        public const int VtblSize = 3 + 2;
#endif
        IRawElementProviderFragment? ElementProviderFromPoint(double x, double y);
        IRawElementProviderFragment? GetFocus();
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IRawElementProviderFragmentRootManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int ElementProviderFromPoint(void* @this, double x, double y, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).ElementProviderFromPoint(x, y);
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetFocus(void* @this, void** ret)
        {
            try
            {
                var obj = ComWrappers.ComInterfaceDispatch.GetInstance<IRawElementProviderFragmentRoot>((ComWrappers.ComInterfaceDispatch*)@this).GetFocus();
                *ret = obj is null ? null : (void*)AutomationNodeComWrappers.Instance.GetOrCreateComInterfaceForObject(obj, CreateComInterfaceFlags.None);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IRawElementProviderFragmentRootNativeWrapper : IRawElementProviderFragmentRoot
    {
        public static IRawElementProviderFragment? ElementProviderFromPoint(void* @this, double x, double y)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 9)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IRawElementProviderFragment?)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        public static IRawElementProviderFragment? GetFocus(void* @this)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 10)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return (IRawElementProviderFragment?)AutomationNodeComWrappers.Instance.GetOrCreateObjectForComInstance((IntPtr)ret, CreateObjectFlags.None);
        }

        IRawElementProviderFragment? IRawElementProviderFragmentRoot.ElementProviderFromPoint(double x, double y) => ElementProviderFromPoint(((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst, x, y);

        IRawElementProviderFragment? IRawElementProviderFragmentRoot.GetFocus() => GetFocus(((AutomationNodeWrapper)this).IRawElementProviderFragmentRootInst);
    }
#endif
}
