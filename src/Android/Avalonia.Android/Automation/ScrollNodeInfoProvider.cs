using Android.OS;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ScrollNodeInfoProvider : NodeInfoProvider<IScrollProvider>
    {
        public ScrollNodeInfoProvider(AutomationPeer peer, int virtualViewId) : base(peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IScrollProvider scrollProvider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionScrollForward:
                    if (scrollProvider.VerticallyScrollable)
                    {
                        scrollProvider.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallIncrement);
                    }
                    else if(scrollProvider.HorizontallyScrollable)
                    {
                        scrollProvider.Scroll(ScrollAmount.SmallIncrement, ScrollAmount.NoAmount);
                    }
                    return true;
                case AccessibilityNodeInfoCompat.ActionScrollBackward:
                    if (scrollProvider.VerticallyScrollable)
                    {
                        scrollProvider.Scroll(ScrollAmount.NoAmount, ScrollAmount.SmallDecrement);
                    }
                    else if (scrollProvider.HorizontallyScrollable)
                    {
                        scrollProvider.Scroll(ScrollAmount.SmallDecrement, ScrollAmount.NoAmount);
                    }
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionScrollForward);
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionScrollBackward);
            nodeInfo.Scrollable = true;
        }
    }
}
