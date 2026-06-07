using System;
using Android.OS;
using Android.Views.Accessibility;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ToggleNodeInfoProvider : NodeInfoProvider<IToggleProvider>
    {
        public ToggleNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        public override bool PerformNodeAction(int action, Bundle? arguments)
        {
            IToggleProvider provider = GetProvider();
            switch (action)
            {
                case AccessibilityNodeInfoCompat.ActionClick:
                    provider.Toggle();
                    return true;
                default:
                    return false;
            }
        }

        public override void PopulateNodeInfo(AccessibilityNodeInfoCompat nodeInfo)
        {
            nodeInfo.AddAction(AccessibilityNodeInfoCompat.ActionClick);
            nodeInfo.Clickable = true;

            IToggleProvider provider = GetProvider();
            ToggleState toggleState = provider.ToggleState;

            // Avoid the AndroidX.Core int Checked property here. Its generated
            // setter maps setChecked(int), which can throw while accessibility
            // services query ToggleButton/CheckBox nodes.
            if (OperatingSystem.IsAndroidVersionAtLeast(36))
            {
                AccessibilityNodeInfo? nativeNodeInfo = nodeInfo.Unwrap();
                if (nativeNodeInfo is not null)
                {
                    nativeNodeInfo.CheckedState = toggleState switch
                    {
                        ToggleState.On => CheckedState.True,
                        ToggleState.Indeterminate => CheckedState.Partial,
                        _ => CheckedState.False
                    };
                }
            }
            else
            {
#pragma warning disable CS0618 // Use the older bool overload to avoid the int setter crash.
                nodeInfo.SetChecked(toggleState == ToggleState.On);
#pragma warning restore CS0618

                if (toggleState == ToggleState.Indeterminate)
                {
                    nodeInfo.StateDescription = "partially checked";
                }
            }

            nodeInfo.Checkable = true;
        }
    }
}
