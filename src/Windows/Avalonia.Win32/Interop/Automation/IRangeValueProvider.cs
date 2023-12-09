using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("36dc7aef-33e6-4691-afe1-2be7274b3d33")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IRangeValueProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("36dc7aef-33e6-4691-afe1-2be7274b3d33");
        public const int VtblSize = 3 + 7;
#endif
        double Value { get; set; }
        bool IsReadOnly { [return: MarshalAs(UnmanagedType.Bool)] get; }
        double Maximum { get; }
        double Minimum { get; }
        double LargeChange { get; }
        double SmallChange { get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IRangeValueProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int GetValue(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRangeValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).Value;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int SetValue(void* @this, double value)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IRangeValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).Value = value;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetIsReadOnly(void* @this, bool* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRangeValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).IsReadOnly;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetMaximum(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRangeValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).Maximum;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetMinimum(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRangeValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).Minimum;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetLargeChange(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRangeValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).LargeChange;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetSmallChange(void* @this, double* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IRangeValueProvider>((ComWrappers.ComInterfaceDispatch*)@this).SmallChange;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IRangeValueProviderNativeWrapper : IRangeValueProvider
    {
        public static void SetValue(void* @this, double value) => AutomationNodeWrapper.Invoke(@this, 3, value);

        public static double GetValue(void* @this) => AutomationNodeWrapper.InvokeAndGet<double>(@this, 4);

        public static bool GetIsReadOnly(void* @this) => AutomationNodeWrapper.InvokeAndGet<bool>(@this, 5);

        public static double GetMaximum(void* @this) => AutomationNodeWrapper.InvokeAndGet<double>(@this, 6);

        public static double GetMinimum(void* @this) => AutomationNodeWrapper.InvokeAndGet<double>(@this, 7);

        public static double GetLargeChange(void* @this) => AutomationNodeWrapper.InvokeAndGet<double>(@this, 8);

        public static double GetSmallChange(void* @this) => AutomationNodeWrapper.InvokeAndGet<double>(@this, 9);

        double IRangeValueProvider.Value
        {
            get => GetValue(((AutomationNodeWrapper)this).IRangeValueProviderInst);
            set => SetValue(((AutomationNodeWrapper)this).IRangeValueProviderInst, value);
        }

        bool IRangeValueProvider.IsReadOnly => GetIsReadOnly(((AutomationNodeWrapper)this).IRangeValueProviderInst);

        double IRangeValueProvider.Maximum => GetMaximum(((AutomationNodeWrapper)this).IRangeValueProviderInst);

        double IRangeValueProvider.Minimum => GetMinimum(((AutomationNodeWrapper)this).IRangeValueProviderInst);

        double IRangeValueProvider.LargeChange => GetLargeChange(((AutomationNodeWrapper)this).IRangeValueProviderInst);

        double IRangeValueProvider.SmallChange => GetSmallChange(((AutomationNodeWrapper)this).IRangeValueProviderInst);
    }
#endif
}
