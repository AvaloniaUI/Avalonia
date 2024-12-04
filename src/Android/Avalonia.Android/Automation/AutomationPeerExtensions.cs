using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation.Peers;

namespace Avalonia.Android.Automation
{
    public static class AutomationPeerExtensions
    {
        public static IEnumerable<AutomationPeer> GetAllDescendants(this AutomationPeer thisPeer)
        {
            yield return thisPeer;

            foreach (AutomationPeer otherPeer in thisPeer.GetChildren().SelectMany(GetAllDescendants))
            {
                yield return otherPeer;
            }
        }
    }
}
