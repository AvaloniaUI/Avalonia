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
            var content = Owner.TopLevelHost?.Decorations?.Content;

            if (content == null)
                return baseChildren;

            var result = new List<AutomationPeer>();

            // Include decoration content peers directly from DrawnWindowDecorationsContent
            if (content.Underlay is Control underlay && underlay.IsVisible)
                result.Add(GetOrCreate(underlay));
            if (content.Overlay is Control overlay && overlay.IsVisible)
                result.Add(GetOrCreate(overlay));
            if (content.FullscreenPopover is Control popover && popover.IsVisible)
                result.Add(GetOrCreate(popover));

            // Add normal Window content children
            if (baseChildren != null)
                result.AddRange(baseChildren);

            return result.Count > 0 ? result : null;
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


