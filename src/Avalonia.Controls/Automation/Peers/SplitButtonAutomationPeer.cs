using Avalonia.Automation;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Avalonia.Automation.Peers
{
    public class SplitButtonAutomationPeer : ContentControlAutomationPeer, IInvokeProvider, IExpandCollapseProvider
    {
        private ExpandCollapseState _expandCollapseState;

        public SplitButtonAutomationPeer(SplitButton owner)
            : base(owner)
        {
            _expandCollapseState = ToState(owner.IsFlyoutOpen);
            owner.FlyoutStateChanged += OwnerFlyoutStateChanged;
        }

        public new SplitButton Owner => (SplitButton)base.Owner;

        public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsFlyoutOpen);

        public bool ShowsMenu => true;

        public void Collapse()
        {
            Owner.CloseFlyoutForAutomation();
        }

        public void Expand()
        {
            Owner.OpenFlyoutForAutomation();
        }

        void IInvokeProvider.Invoke()
        {
            EnsureEnabled();
            Owner.InvokePrimary();
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.SplitButton;
        }

        protected override string GetClassNameCore()
        {
            return "SplitButton";
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;

        private void OwnerFlyoutStateChanged(object? sender, System.EventArgs e)
        {
            var oldState = _expandCollapseState;
            var newState = ToState(Owner.IsFlyoutOpen);

            if (oldState != newState)
            {
                _expandCollapseState = newState;
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    oldState,
                    newState);
            }
        }

        private static ExpandCollapseState ToState(bool isOpen)
        {
            return isOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }
    }
}
