using System;
using Avalonia.Controls.Primitives;

namespace Avalonia.Automation.Peers
{
    public class PopupRootAutomationPeer : WindowBaseAutomationPeer
    {
        public PopupRootAutomationPeer(PopupRoot owner)
            : base(owner)
        {
            if (owner.IsVisible)
                StartTrackingFocus();
            else
                owner.Opened += OnOpened;
            owner.Closed += OnClosed;
        }

        protected override bool IsContentElementCore() => false;
        protected override bool IsControlElementCore() => false;


        protected override AutomationPeer? GetParentCore()
        {
            var parent = base.GetParentCore();
            return parent;
        }

        private void OnOpened(object? sender, EventArgs e)
        {
            ((PopupRoot)Owner).Opened -= OnOpened;
            StartTrackingFocus();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            ((PopupRoot)Owner).Closed -= OnClosed;
            StopTrackingFocus();
        }
    }
}
