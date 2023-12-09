using System;
using System.Runtime.InteropServices;
using Avalonia.Win32.Automation;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("2360c714-4bf1-4b26-ba65-9b21316127eb")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IScrollItemProvider
    {
#if NET6_0_OR_GREATER
        public static readonly Guid IID = new("2360c714-4bf1-4b26-ba65-9b21316127eb");
        public const int VtblSize = 3 + 1;
#endif
        void ScrollIntoView();
    }

#if NET6_0_OR_GREATER
    internal static unsafe class IScrollItemProviderManagedWrapper
    {
        [UnmanagedCallersOnly]
        public static int ScrollIntoView(void* @this)
        {
            try
            {
                ComWrappers.ComInterfaceDispatch.GetInstance<IScrollItemProvider>((ComWrappers.ComInterfaceDispatch*)@this).ScrollIntoView();
                return 0;
            }
            catch (Exception ex)
            {
                return ex.HResult;
            }
        }
    }

    [DynamicInterfaceCastableImplementation]
    internal unsafe interface IScrollItemProviderNativeWrapper : IScrollItemProvider
    {
        public static void ScrollIntoView(void* @this) => AutomationNodeWrapper.Invoke(@this, 3);

        void IScrollItemProvider.ScrollIntoView() => ScrollIntoView(((AutomationNodeWrapper)this).IScrollItemProviderInst);
    }
#endif
}
