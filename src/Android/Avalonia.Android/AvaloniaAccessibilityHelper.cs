using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Android
{
    internal class AvaloniaAccessibilityHelper : ExploreByTouchHelper
    {
        private readonly AvaloniaView avaloniaView;

        private readonly Dictionary<int, AutomationPeer> peers;

        public AvaloniaAccessibilityHelper(AvaloniaView avaloniaView) 
        {
            this.avaloniaView = avaloniaView;
            this.peers = new();
        }

        protected override void GetVisibleVirtualViews(List<int> virtualViewIds) 
        {
            Control? control = avaloniaView.Content as Control;
            AutomationPeer peer = control?.GetOrCreateAutomationPeer();
            foreach (AutomationPeer child in peer?.GetChildren() ?? [])
            {
                virtualViewIds.Add(child.GetAutomationId().GetHashCode());
            }
        }

        protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat nodeInfo) 
        {
            AutomationPeer peer = peers[virtualViewId];

            Rect bounds = peer.GetBoundingRectangle();
            nodeInfo.SetBoundsInWindow(bounds);

            nodeInfo.HintText = peer.GetHelpText();
        }
    }
}
