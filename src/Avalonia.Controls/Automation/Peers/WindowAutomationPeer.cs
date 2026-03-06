using System;
using System.Collections.Generic;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class WindowAutomationPeer : WindowBaseAutomationPeer
    {
        public WindowAutomationPeer(Window owner)
            : base(owner)
        {
            if (owner.IsVisible)
                StartTrackingFocus();
            else
                owner.Opened += OnOpened;
            owner.Closed += OnClosed;
        }

        public new Window Owner => (Window)base.Owner;

        protected override string? GetNameCore() => Owner.Title;

        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var baseChildren = base.GetChildrenCore();
            var overlayPeer = Owner.TopLevelHost.GetOrCreateDecorationsOverlaysPeer();
            
            var rv = new List<AutomationPeer> { overlayPeer };
            if (baseChildren?.Count > 0)
                rv.AddRange(baseChildren);
            return rv;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            Owner.Opened -= OnOpened;
            StartTrackingFocus();
        }
        
        private void OnClosed(object? sender, EventArgs e)
        {
            Owner.Closed -= OnClosed;
            StopTrackingFocus();
        }
    }
}


