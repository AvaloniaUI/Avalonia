using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.OS;
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
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionSetText:
                    string? text = arguments?.GetCharSequence(
                        AccessibilityNodeInfoCompat.ActionArgumentSetTextCharsequence
                        );
                    GetProvider().SetValue(text);
                    return true;

                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionSetText);
            nodeInfo.Editable = !GetProvider().IsReadOnly;
        }
    }
}
