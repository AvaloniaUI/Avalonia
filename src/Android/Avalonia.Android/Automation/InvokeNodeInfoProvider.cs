﻿using Android.OS;
using AndroidX.Core.View.Accessibility;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    public class InvokeNodeInfoProvider : NodeInfoProvider<IInvokeProvider>
    {
        public InvokeNodeInfoProvider(AutomationPeer peer) : base(peer)
        {
        }

        public override bool PerformNodeAction(int action, Bundle arguments)
        {
            if (action == AccessibilityNodeInfoCompat.ActionClick)
            {
                GetProvider().Invoke();
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            base.PopulateNodeInfo(nodeInfo);

            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionClick);
            nodeInfo.Clickable = true;
        }
    }
}
