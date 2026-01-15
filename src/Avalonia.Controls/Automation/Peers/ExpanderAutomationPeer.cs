using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Avalonia.Controls.Automation.Peers
{
    public class ExpanderAutomationPeer : ControlAutomationPeer,
        IExpandCollapseProvider
    {
        public ExpanderAutomationPeer(Control owner)
            : base(owner)
        {
            owner.PropertyChanged += OwnerPropertyChanged;
        }

        public new Expander Owner => (Expander)base.Owner;

        public ExpandCollapseState ExpandCollapseState => ToState(Owner.IsExpanded);
        public bool ShowsMenu => false;
        public void Collapse() => Owner.IsExpanded = false;
        public void Expand() => Owner.IsExpanded = true;

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.ComboBox;
        }

        private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == Expander.IsExpandedProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    ToState((bool)e.OldValue!),
                    ToState((bool)e.NewValue!));
            }
        }

        private static ExpandCollapseState ToState(bool value)
        {
            return value ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        }

        protected override bool IsContentElementCore() => true;
        protected override bool IsControlElementCore() => true;
    }
}
