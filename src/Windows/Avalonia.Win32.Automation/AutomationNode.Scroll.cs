using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Automation.Interop;

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IScrollProvider, UIA.IScrollItemProvider
    {
        bool UIA.IScrollProvider.GetHorizontallyScrollable() => InvokeSync<IScrollProvider, bool>(x => x.HorizontallyScrollable);
        double UIA.IScrollProvider.GetHorizontalScrollPercent() => InvokeSync<IScrollProvider, double>(x => x.HorizontalScrollPercent);
        double UIA.IScrollProvider.GetHorizontalViewSize() => InvokeSync<IScrollProvider, double>(x => x.HorizontalViewSize);
        bool UIA.IScrollProvider.GetVerticallyScrollable() => InvokeSync<IScrollProvider, bool>(x => x.VerticallyScrollable);
        double UIA.IScrollProvider.GetVerticalScrollPercent() => InvokeSync<IScrollProvider, double>(x => x.VerticalScrollPercent);
        double UIA.IScrollProvider.GetVerticalViewSize() => InvokeSync<IScrollProvider, double>(x => x.VerticalViewSize);

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
}
