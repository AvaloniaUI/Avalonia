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
                    provider.SetValue(provider.Value + text);
                    return true;

                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionSetText);

            nodeInfo.TextSelectable = true;
            nodeInfo.InputType = (int)InputTypes.ClassText;
            nodeInfo.LiveRegion = ViewCompat.AccessibilityLiveRegionPolite;

            IValueProvider provider = GetProvider();
            nodeInfo.Editable = !provider.IsReadOnly;
        }
    }
}
