using Avalonia.Automation.Peers;

namespace Avalonia.Android.Automation
{
    internal class AutomationPeerState
    {
        public AutomationPeer Instance { get; }

        public bool IsOffscreenCached { get; set; }

        public AutomationPeerState(AutomationPeer instance) 
        {
            Instance = instance;
            IsOffscreenCached = instance.IsOffscreen();
        }
    }
}
