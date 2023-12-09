using System;
using System.Runtime.InteropServices;
using Avalonia.Automation;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IExpandCollapseProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("d847d3a5-cab0-4a98-8c32-ecb45c59ad24");
        public const int VtblSize = 3 + 3;
#endif
        void Expand();
        void Collapse();
        ExpandCollapseState ExpandCollapseState { get; }
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IExpandCollapseProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int Expand(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IExpandCollapseProvider>((ComWrappers.ComInterfaceDispatch*)@this).Expand();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int Collapse(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IExpandCollapseProvider>((ComWrappers.ComInterfaceDispatch*)@this).Collapse();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }

        [UnmanagedCallersOnly]
        public static int GetExpandCollapseState(void* @this, ExpandCollapseState* retVal)
        {
            try
            {
                *retVal = ComWrappers.ComInterfaceDispatch.GetInstance<IExpandCollapseProvider>((ComWrappers.ComInterfaceDispatch*)@this).ExpandCollapseState;
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IExpandCollapseProviderNativeWrapper : IExpandCollapseProvider
    {
        public static void Expand(void* @this) => AutomationNodeWrapper.Invoke(@this, 3);

        public static void Collapse(void* @this) => AutomationNodeWrapper.Invoke(@this, 4);

        public static ExpandCollapseState GetExpandCollapseState(void* @this) => AutomationNodeWrapper.InvokeAndGet<ExpandCollapseState>(@this, 5);

        void IExpandCollapseProvider.Expand() => Expand(((AutomationNodeWrapper)this).IExpandCollapseProviderInst);

        void IExpandCollapseProvider.Collapse() => Collapse(((AutomationNodeWrapper)this).IExpandCollapseProviderInst);

        ExpandCollapseState IExpandCollapseProvider.ExpandCollapseState => GetExpandCollapseState(((AutomationNodeWrapper)this).IExpandCollapseProviderInst);
    }
#endif
}
