using System;
using System.Runtime.InteropServices;
using Avalonia.Automation.Provider;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IToggleProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("56d00bd0-c4f4-433c-a836-1a52a57e0892");
        public const int VtblSize = 3 + 2;
#endif
        void Toggle( );
        ToggleState ToggleState { get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IToggleProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int Toggle(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IToggleProvider>((ComWrappers.ComInterfaceDispatch*)@this).Toggle();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetToggleState(void* @this, ToggleState* ret)
        {
            try
            {
                *ret = ComWrappers.ComInterfaceDispatch.GetInstance<IToggleProvider>((ComWrappers.ComInterfaceDispatch*)@this).ToggleState;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IToggleProviderNativeWrapper : IToggleProvider
    {
        public static void Toggle(void* @this) => AutomationNodeWrapper.Invoke(@this, 3);

        public static ToggleState GetToggleState(void* @this)
        {
            ToggleState ret;
            int hr = ((delegate* unmanaged<void*, ToggleState*, int>)(*(*(void***)@this + 4)))(@this, &ret);

            if (hr != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return ret;
        }

        void IToggleProvider.Toggle()=> Toggle(((AutomationNodeWrapper)this).IToggleProviderInst);

        ToggleState IToggleProvider.ToggleState => GetToggleState(((AutomationNodeWrapper)this).IToggleProviderInst);
    }
#endif
}
