using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("c7935180-6fb3-4201-b174-7df73adbf64a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IValueProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("c7935180-6fb3-4201-b174-7df73adbf64a");
        public const int VtblSize = 3 + 3;
#endif
        string? Value { get; [param: MarshalAs(UnmanagedType.LPWStr)] set; }
        bool IsReadOnly { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IValueProviderManagedWrapper
    {

        [UnmanagedCallersOnly]
        public static int SetValue(void* @this, void* value)
        {
            try
            {
                var str = value is null ? null : Marshal.PtrToStringUni((IntPtr)value);
                ComWrappers.ComInterfaceDispatch.GetInstance<IValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).Value = str;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetValue(void* @this, void** retVal)
        {
            try
            {
                *retVal = (void*)Marshal.StringToCoTaskMemUni(ComWrappers.ComInterfaceDispatch.GetInstance<IValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).Value);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetIsReadOnly(void* @this, bool* retVal)
        {
            try
            {
                *retVal = ComWrappers.ComInterfaceDispatch.GetInstance<IValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).IsReadOnly;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IValueProviderNativeWrapper : IValueProvider
    {
        public static void SetValue(void* @this, string? value)
        {
            void* str = (void*)Marshal.StringToCoTaskMemUni(value);
            int hr = ((delegate* unmanaged<void*, void*, int>)(*(*(void***)@this + 3)))(@this, str);
            Marshal.FreeCoTaskMem((IntPtr)str);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static string? GetValue(void* @this)
        {
            void* ret;
            int hr = ((delegate* unmanaged<void*, void**, int>)(*(*(void***)@this + 4)))(@this, &ret);
            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            var str = Marshal.PtrToStringUni((IntPtr)ret);
            Marshal.FreeCoTaskMem((IntPtr)ret);
            return str;
        }

        public static bool GetIsReadOnly(void* @this) => AutomationNodeWrapper.InvokeAndGet<bool>(@this, 5);

        string? IValueProvider.Value
        {
            get => GetValue(((AutomationNodeWrapper)this).IValueProviderInst);
            set => SetValue(((AutomationNodeWrapper)this).IValueProviderInst, value);
        }

        bool IValueProvider.IsReadOnly => GetIsReadOnly(((AutomationNodeWrapper)this).IValueProviderInst);
    }
#endif
}
