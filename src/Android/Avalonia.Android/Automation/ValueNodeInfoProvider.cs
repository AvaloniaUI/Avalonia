using Android.OS;
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

            nodeInfo.ClassName = "android.widget.EditText";
            nodeInfo.LiveRegion = ViewCompat.AccessibilityLiveRegionPolite;

            IValueProvider provider = GetProvider();
            nodeInfo.Editable = !provider.IsReadOnly;
            nodeInfo.SetTextSelection(
                provider.Value?.Length ?? 0, 
                provider.Value?.Length ?? 0
                );
        }
    }
}
