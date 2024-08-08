using System;
using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IScrollProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("b38b8077-1fc3-42a5-8cae-d40c2215055a");
        public const int VtblSize = 3 + 8;
#endif
        void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);
        void SetScrollPercent(double horizontalPercent, double verticalPercent);
        double HorizontalScrollPercent { get; }
        double VerticalScrollPercent { get; }
        double HorizontalViewSize { get; }
        double VerticalViewSize { get; }
        bool HorizontallyScrollable { [return: MarshalAs(UnmanagedType.Bool)] get; }
        bool VerticallyScrollable { [return: MarshalAs(UnmanagedType.Bool)] get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IScrollProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int Scroll(void* @this, ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).Scroll(horizontalAmount, verticalAmount);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int SetScrollPercent(void* @this, double horizontalPercent, double verticalPercent)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).SetScrollPercent(horizontalPercent, verticalPercent);
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetHorizontalScrollPercent(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).HorizontalScrollPercent;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetVerticalScrollPercent(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).VerticalScrollPercent;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetHorizontalViewSize(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).HorizontalViewSize;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetVerticalViewSize(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).VerticalViewSize;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetHorizontallyScrollable(void* @this, bool* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).HorizontallyScrollable;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetVerticallyScrollable(void* @this, bool* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IScrollProvider>((ComWrappers.ComInterfaceDispatch*)@this).VerticallyScrollable;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IScrollProviderNativeWrapper : IScrollProvider
    {
        public static void Scroll(void* @this, ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            int hr = ((delegate* unmanaged<void*, ScrollAmount, ScrollAmount, int>)(*(*(void***)@this + 3)))(@this, horizontalAmount, verticalAmount);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static void SetScrollPercent(void* @this, double horizontalPercent, double verticalPercent)
        {
            int hr = ((delegate* unmanaged<void*, double, double, int>)(*(*(void***)@this + 4)))(@this, horizontalPercent, verticalPercent);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        public static double GetHorizontalScrollPercent(void* @this) => AutomationNodeWrapper.InvokeAndGetDouble(@this, 5);

        public static double GetVerticalScrollPercent(void* @this) => AutomationNodeWrapper.InvokeAndGetDouble(@this, 6);

        public static double GetHorizontalViewSize(void* @this) => AutomationNodeWrapper.InvokeAndGetDouble(@this, 7);

        public static double GetVerticalViewSize(void* @this) => AutomationNodeWrapper.InvokeAndGetDouble(@this, 8);

        public static bool GetHorizontallyScrollable(void* @this) => AutomationNodeWrapper.InvokeAndGetBool(@this, 9);

        public static bool GetVerticallyScrollable(void* @this) => AutomationNodeWrapper.InvokeAndGetBool(@this, 10);

        void IScrollProvider.Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) => Scroll(((AutomationNodeWrapper)this).IScrollProviderInst, horizontalAmount, verticalAmount);

        void IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent) => SetScrollPercent(((AutomationNodeWrapper)this).IScrollProviderInst, horizontalPercent, verticalPercent);

        double IScrollProvider.HorizontalScrollPercent => GetHorizontalScrollPercent(((AutomationNodeWrapper)this).IScrollProviderInst);

        double IScrollProvider.VerticalScrollPercent => GetVerticalScrollPercent(((AutomationNodeWrapper)this).IScrollProviderInst);

        double IScrollProvider.HorizontalViewSize => GetHorizontalViewSize(((AutomationNodeWrapper)this).IScrollProviderInst);

        double IScrollProvider.VerticalViewSize => GetVerticalViewSize(((AutomationNodeWrapper)this).IScrollProviderInst);

        bool IScrollProvider.HorizontallyScrollable => GetHorizontallyScrollable(((AutomationNodeWrapper)this).IScrollProviderInst);

        bool IScrollProvider.VerticallyScrollable => GetVerticallyScrollable(((AutomationNodeWrapper)this).IScrollProviderInst);
    }
#endif
}
