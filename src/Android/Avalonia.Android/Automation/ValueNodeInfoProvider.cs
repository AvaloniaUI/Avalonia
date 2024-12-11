using Android.OS;
using Android.Text;
using AndroidX.Core.View;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ValueNodeInfoProvider : NodeInfoProvider<IValueProvider>
    {
        public ValueNodeInfoProvider(AutomationPeer peer, int virtualViewId) : base(peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IValueProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionSetText:
                    string? text = arguments?.GetCharSequence(
                        AccessibilityNodeInfoCompat.ActionArgumentSetTextCharsequence
                        );
                    provider.SetValue(text);
                    return true;

                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            IValueProvider provider = GetProvider();
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionSetText);

            nodeInfo.Editable = !provider.IsReadOnly;
            nodeInfo.TextSelectable = true;
            nodeInfo.InputType = (int)InputTypes.ClassText;
            nodeInfo.LiveRegion = ViewCompat.AccessibilityLiveRegionPolite;
        }
    }
}
