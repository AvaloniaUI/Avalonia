using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IScrollProvider, UIA.IScrollItemProvider
    {
        bool UIA.IScrollProvider.HorizontallyScrollable => InvokeSync<IScrollProvider, bool>(x => x.HorizontallyScrollable);
        double UIA.IScrollProvider.HorizontalScrollPercent => InvokeSync<IScrollProvider, double>(x => x.HorizontalScrollPercent);
        double UIA.IScrollProvider.HorizontalViewSize => InvokeSync<IScrollProvider, double>(x => x.HorizontalViewSize);
        bool UIA.IScrollProvider.VerticallyScrollable => InvokeSync<IScrollProvider, bool>(x => x.VerticallyScrollable);
        double UIA.IScrollProvider.VerticalScrollPercent => InvokeSync<IScrollProvider, double>(x => x.VerticalScrollPercent);
        double UIA.IScrollProvider.VerticalViewSize => InvokeSync<IScrollProvider, double>(x => x.VerticalViewSize);

        void UIA.IScrollProvider.Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount)
        {
            InvokeSync<IScrollProvider>(x => x.Scroll(horizontalAmount, verticalAmount));
        }

        void UIA.IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent)
        {
            InvokeSync<IScrollProvider>(x => x.SetScrollPercent(horizontalPercent, verticalPercent));
        }

        void UIA.IScrollItemProvider.ScrollIntoView()
        {
            InvokeSync(() => Peer.BringIntoView());
        }
    }


#if NET6_0_OR_GREATER

    internal unsafe partial class AutomationNodeWrapper : UIA.IScrollProvider, UIA.IScrollItemProvider
    {
        public void* IScrollProviderInst { get; init; }
        public void* IScrollItemProviderInst { get; init; }


        double UIA.IScrollProvider.HorizontalScrollPercent => UIA.IScrollProviderNativeWrapper.GetHorizontalScrollPercent(IScrollProviderInst);

        double UIA.IScrollProvider.VerticalScrollPercent => UIA.IScrollProviderNativeWrapper.GetVerticalScrollPercent(IScrollProviderInst);

        double UIA.IScrollProvider.HorizontalViewSize => UIA.IScrollProviderNativeWrapper.GetHorizontalViewSize(IScrollProviderInst);

        double UIA.IScrollProvider.VerticalViewSize => UIA.IScrollProviderNativeWrapper.GetVerticalViewSize(IScrollProviderInst);

        bool UIA.IScrollProvider.HorizontallyScrollable => UIA.IScrollProviderNativeWrapper.GetHorizontallyScrollable(IScrollProviderInst);

        bool UIA.IScrollProvider.VerticallyScrollable => UIA.IScrollProviderNativeWrapper.GetVerticallyScrollable(IScrollProviderInst);

        void UIA.IScrollProvider.Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) => UIA.IScrollProviderNativeWrapper.Scroll(IScrollProviderInst, horizontalAmount, verticalAmount);

        void UIA.IScrollProvider.SetScrollPercent(double horizontalPercent, double verticalPercent) => UIA.IScrollProviderNativeWrapper.SetScrollPercent(IScrollProviderInst, horizontalPercent, verticalPercent);

        void UIA.IScrollItemProvider.ScrollIntoView() => UIA.IScrollItemProviderNativeWrapper.ScrollIntoView(IScrollItemProviderInst);
    }
#endif
}
