using System.Reflection;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Android.Automation
{
    internal class ToggleNodeInfoProvider : NodeInfoProvider<IToggleProvider>
    {
        private PropertyInfo? _checkedProperty;

        public ToggleNodeInfoProvider(ExploreByTouchHelper owner, AutomationPeer peer, int virtualViewId) : 
            base(owner, peer, virtualViewId)
        {
        }

        static ToggleNodeInfoProvider()
        {
            s_checkedProperty = typeof(AccessibilityNodeInfoCompat).GetProperty(nameof(AccessibilityNodeInfoCompat.Checked));
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

            _checkedProperty ??= nodeInfo.GetType().GetProperty(nameof(nodeInfo.Checked));
            if (_checkedProperty?.PropertyType == typeof(int))
            {
                // Needed for Xamarin.AndroidX.Core 1.17+
                _checkedProperty.SetValue(this, 
                    provider.ToggleState switch
                    {
                        ToggleState.On => 1,
                        ToggleState.Indeterminate => 2,
                        _ => 0
                    });
            }
            else if (_checkedProperty?.PropertyType == typeof(bool))
            {
                // Needed for Xamarin.AndroidX.Core < 1.17
                _checkedProperty.SetValue(this, provider.ToggleState == ToggleState.On);
            }

            nodeInfo.Checkable = true;
        }
    }
}
